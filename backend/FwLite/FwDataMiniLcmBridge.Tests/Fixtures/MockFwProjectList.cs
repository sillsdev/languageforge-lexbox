﻿using Microsoft.Extensions.Options;
using MiniLcm.Models;

namespace FwDataMiniLcmBridge.Tests.Fixtures;

public class MockFwProjectList(IOptions<FwDataBridgeConfig> config, MockFwProjectLoader loader) : FieldWorksProjectList(config)
{
    public override IEnumerable<IProjectIdentifier> EnumerateProjects()
    {
        return loader.Projects.Keys.Select(k => new FwDataProject(k, _config.Value.ProjectsFolder));
    }

    public override FwDataProject? GetProject(string name)
    {
        return EnumerateProjects().OfType<FwDataProject>().FirstOrDefault(p => p.Name == name);
    }
}
