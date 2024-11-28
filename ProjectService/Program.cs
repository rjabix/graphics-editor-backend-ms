using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ProjectService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
    o.MaximumReceiveMessageSize = 102400000;
});

builder.Services.AddScoped<IProjectService, ProjectService.ProjectService>();

builder.Services.AddDbContext<ProjectDbContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireUserId", policy =>
    {
        policy.Requirements.Add(new UserIdRequirement()); // checking whether
                                                          // the user id is present in the request header
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhost",
        builder =>
        {
            builder.WithOrigins("http://localhost", "http://localhost:3000", "http://localhost:8080")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowLocalhost");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<SignalingHub>("/signalr");

app.Run();


// block for internal classes

// authorization policy for checking whether the user id is present in the request header
internal class UserIdRequirement : IAuthorizationRequirement { }

internal class UserIdHandler(IHttpContextAccessor httpContextAccessor) : AuthorizationHandler<UserIdRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserIdRequirement requirement)
    {
        var userId = httpContextAccessor.HttpContext.Request.Headers["UserId"].ToString();
        if (!string.IsNullOrEmpty(userId))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}