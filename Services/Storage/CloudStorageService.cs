namespace Healthcare_Industry.Services;

public interface ICloudStorageService
{
    Task<string?> SaveProductImageAsync(IFormFile? file);
}

public sealed class LocalCloudStorageService : ICloudStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private readonly IWebHostEnvironment _environment;

    public LocalCloudStorageService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public async Task<string?> SaveProductImageAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only JPG, PNG, and WEBP images can be uploaded.");
        }

        var folder = Path.Combine(_environment.WebRootPath, "uploads", "product-images");
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var filePath = Path.Combine(folder, fileName);

        await using var stream = File.Create(filePath);
        await file.CopyToAsync(stream);

        return $"/uploads/product-images/{fileName}";
    }
}
