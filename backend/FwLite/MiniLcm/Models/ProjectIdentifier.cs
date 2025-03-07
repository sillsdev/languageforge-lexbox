namespace MiniLcm.Models;

public interface IProjectIdentifier
{
    /// <summary>
    /// A human-readable unique identifier for the project
    /// </summary>
    string Name { get; }
    ProjectDataFormat DataFormat { get; }
}

public enum ProjectDataFormat
{
    Harmony,
    FwData
}
