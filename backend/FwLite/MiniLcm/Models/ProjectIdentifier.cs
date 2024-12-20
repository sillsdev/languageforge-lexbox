namespace MiniLcm.Models;

public interface IProjectIdentifier
{
    string Name { get; }
    ProjectDataFormat DataFormat { get; }
}

public enum ProjectDataFormat
{
    Harmony,
    FwData
}
