using MiniLcm;

namespace FwDataMiniLcmBridge;

public class FwDataProject(string name, string fileName) : IProjectIdentifier
{
    public string Name { get; } = name;
    public string FileName { get; } = fileName;
    public string Origin { get; } = "FieldWorks";
}
