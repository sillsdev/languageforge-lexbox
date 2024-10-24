using MiniLcm.Models;

namespace FwDataMiniLcmBridge;

public class FwDataProject(string name, string fileName, string? path = null) : IProjectIdentifier
{
    public string Name { get; } = name;
    public string FileName { get; } = fileName;
    public string? Path { get; } = path;
    public string Origin { get; } = "FieldWorks";
}
