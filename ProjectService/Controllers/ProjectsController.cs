using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ProjectService.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize("RequireUserId")] // checking whether the user id is present in the request header
public class ProjectsController(
    ILogger<ProjectsController> logger,
    IProjectService projectService, 
    ProjectDbContext context) : ControllerBase
{

    private readonly ILogger<ProjectsController> _logger = logger;
    private readonly IProjectService _projectService = projectService;
    private readonly ProjectDbContext _context = context;
    
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] ProjectCreateModel model)
    {
        string image = await _projectService.CreateImage(model.Width, model.Height);
        
        string userId = Request.Headers["X-UserId"].ToString();

        var project = new Project
        {
            Name = model.Name,
            Width = model.Width,
            Height = model.Height,
            UserId = userId,
            CreatedDate = DateTime.Now,
            LastModifiedDate = DateTime.Now,
            Image = image,
            PreviewImage = await _projectService.CompressImage(image)
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return Ok(new { id = project.Id });
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromBody] ProjectUploadModel model)
    {
        string userId = Request.Headers["X-UserId"].ToString();
        
        var project = new Project
        {
            Name = model.Name,
            Width = model.Width,
            Height = model.Height,
            UserId = userId,
            CreatedDate = DateTime.Now,
            LastModifiedDate = DateTime.Now,
            Image = model.Image,
            PreviewImage = await _projectService.CompressImage(model.Image)
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return Ok(new { id = project.Id });
    }

   [HttpGet]
    public async Task<IActionResult> GetAllProjects([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var projects = await _context.Projects
                                         .Where(p => p.UserId == Request.Headers["X-UserId"].ToString())
                                         .OrderByDescending(p => p.LastModifiedDate)
                                         .Skip((pageNumber - 1) * pageSize)
                                         .Take(pageSize)
                                         .Select(p => new ProjectsGetModel
                                         {
                                             Id = p.Id,
                                             Name = p.Name,
                                             CollaboratorsCount = p.Collaborators.Count,
                                             ImagePreview = p.PreviewImage,
                                             CreatedAt = p.CreatedDate,
                                             LastUpdatedAt = p.LastModifiedDate
                                         })
                                         .AsNoTracking()
                                         .ToListAsync();

            if (projects == null || projects.Count == 0)
            {
                return NotFound("No projects found.");
            }

            return Ok(projects);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProject(string id)
    {
        var project = await _context.Projects.AsNoTracking()
                                    .FirstOrDefaultAsync(p => p.Id == id && 
                                                              p.UserId == Request.Headers["X-UserId"].ToString());

        if (project == null) return NotFound("Project not found.");

        return Ok(new { name = project.Name, image = project.Image });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProject(string id)
    {
        var project = await _context.Projects
                                    .FirstOrDefaultAsync(p => p.Id == id && 
                                                              p.UserId == Request.Headers["X-UserId"].ToString());
        
        if (project == null) return NotFound("Project not found.");

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return Ok("Project deleted successfully.");
    }

    [HttpPut("{imageId}")]
    public async Task<IActionResult> SaveProject(string imageId, [FromBody] SaveProjectModel model) {
        var project = await _context.Projects
                                    .FirstOrDefaultAsync(p => p.Id == imageId && 
                                                              p.UserId == Request.Headers["X-UserId"].ToString());

        if (project == null) return NotFound("Project not found.");

        project.Image = model.Image;
        project.Name = model.Name;
        project.PreviewImage = await _projectService.CompressImage(model.Image);
        project.LastModifiedDate = DateTime.Now;

        _context.Projects.Update(project);
        await _context.SaveChangesAsync();

        return Ok("Project saved successfully. ");
    }
}

public class ProjectCreateModel
{
    public required string Name { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ProjectUploadModel
{
    public required string Name { get; set; }
    public required int Width { get; set; }
    public required int Height { get; set; }
    public required string Image { get; set; }
}

public class ProjectsGetModel
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int CollaboratorsCount { get; set; }
    public string? ImagePreview { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdatedAt { get; set; }
}

public class SaveProjectModel
{
    public required string Image { get; set; }
    public required string Name { get; set; }
}
