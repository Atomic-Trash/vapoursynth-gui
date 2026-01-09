using System.IO;

namespace VapourSynthPortable.Tests.Fixtures;

/// <summary>
/// Provides a temporary directory for tests that need file I/O.
/// Automatically cleans up after each test.
/// </summary>
public class TempDirectoryFixture : IDisposable
{
    public string TempPath { get; }

    public TempDirectoryFixture()
    {
        TempPath = Path.Combine(Path.GetTempPath(), "VapourSynthTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempPath);
    }

    /// <summary>
    /// Creates a file with the given content in the temp directory.
    /// </summary>
    public string CreateFile(string fileName, string content)
    {
        var filePath = Path.Combine(TempPath, fileName);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(filePath, content);
        return filePath;
    }

    /// <summary>
    /// Creates an empty file in the temp directory.
    /// </summary>
    public string CreateEmptyFile(string fileName)
    {
        return CreateFile(fileName, string.Empty);
    }

    /// <summary>
    /// Creates a subdirectory in the temp directory.
    /// </summary>
    public string CreateSubdirectory(string name)
    {
        var path = Path.Combine(TempPath, name);
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Gets the full path for a file in the temp directory.
    /// </summary>
    public string GetPath(string fileName) => Path.Combine(TempPath, fileName);

    /// <summary>
    /// Checks if a file exists in the temp directory.
    /// </summary>
    public bool FileExists(string fileName) => File.Exists(GetPath(fileName));

    /// <summary>
    /// Reads the content of a file in the temp directory.
    /// </summary>
    public string ReadFile(string fileName) => File.ReadAllText(GetPath(fileName));

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(TempPath))
            {
                Directory.Delete(TempPath, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}

/// <summary>
/// Collection fixture for sharing temp directory across tests in a class.
/// </summary>
public class TempDirectoryCollection : ICollectionFixture<TempDirectoryFixture>
{
}
