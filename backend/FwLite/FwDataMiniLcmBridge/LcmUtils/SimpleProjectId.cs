using SIL.LCModel;

namespace FwDataMiniLcmBridge.LcmUtils;

public class SimpleProjectId : IProjectIdentifier
{
    private readonly BackendProviderType _type;

    public SimpleProjectId(BackendProviderType type, string path)
    {
        this._type = type;
        this.Path = path;
    }

    public string UiName => this.Name;

    public string Path { get; set; }

    public string Handle => this.Path;

    public string PipeHandle => throw new NotImplementedException();

    public string Name => System.IO.Path.GetFileNameWithoutExtension(this.Path);

    public string? ProjectFolder => System.IO.Path.GetDirectoryName(this.Path);

    public BackendProviderType Type => this._type;
}
