using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ProjectService;

public class Project
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; init; } 
    
    public required string Name { get; set; }
    
    [ForeignKey("UserId")]
    public required string UserId { get; set; }

    public required int Width { get; set; }

    public required int Height { get; set; }

    public List<string> Collaborators { get; set; } = [];

    public required DateTime CreatedDate { get; set; }

    public required DateTime LastModifiedDate { get; set; }

    public string? Image { get; set; }

    public string? PreviewImage { get; set; }
}