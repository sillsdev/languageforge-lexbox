using SIL.LCModel;

namespace FwDataMiniLcmBridge.LcmUtils;

public record LcmDirectories(string ProjectsDirectory, string TemplateDirectory) : ILcmDirectories;
