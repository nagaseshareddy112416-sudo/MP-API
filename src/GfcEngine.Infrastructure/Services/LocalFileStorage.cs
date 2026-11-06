using Microsoft.Extensions.Configuration;
using GfcEngine.Application.Common.Interfaces;

namespace GfcEngine.Infrastructure.Services;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(IConfiguration configuration)
    {
        var configured = configuration["FileStorage:LocalPath"];
        _basePath = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Directory.GetCurrentDirectory(), "uploads")
            : Path.GetFullPath(configured);

        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
        }
    }

    public async Task<string> SaveFileAsync(Stream stream, string fileName, string subFolder, CancellationToken cancellationToken = default)
    {
        var targetDir = Path.Combine(_basePath, subFolder);
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";
        var relativePath = Path.Combine(subFolder, uniqueFileName);
        var fullPath = Path.Combine(_basePath, relativePath);

        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);

        return relativePath.Replace('\\', '/');
    }

    public Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found at {relativePath}");
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetFullPath(relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }
        return Task.CompletedTask;
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_basePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}

