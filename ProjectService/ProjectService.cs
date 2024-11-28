using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ProjectService;
public interface IProjectService
{
    Task<string> CreateImage(int width, int height);
    Task<string> CompressImage(string base64Image);
}

public class ProjectService : IProjectService
{
    public async Task<string> CreateImage(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Color.White);
        using var outputStream = new MemoryStream();
        await image.SaveAsPngAsync(outputStream);
        var base64Data = Convert.ToBase64String(outputStream.ToArray());
        return $"data:image/png;base64,{base64Data}";
    }

    public async Task<string> CompressImage(string base64Image)
    {
        var base64Data = base64Image.Substring(base64Image.IndexOf(",") + 1);
        byte[] imageBytes = Convert.FromBase64String(base64Data);

        using var inputStream = new MemoryStream(imageBytes);
        using var image = await Image.LoadAsync(inputStream);
        image.Mutate(x => x.Resize(426, 240));

        using var outputStream = new MemoryStream();
        var encoder = new JpegEncoder { Quality = 75 };
                    
        await image.SaveAsync(outputStream, encoder);

        var newBase64Data = Convert.ToBase64String(outputStream.ToArray());
        return $"data:image/jpeg;base64,{newBase64Data}";
    }
}