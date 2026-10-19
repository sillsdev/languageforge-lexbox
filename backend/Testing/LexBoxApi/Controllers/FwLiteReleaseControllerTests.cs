using FluentAssertions;
using LexBoxApi.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Testing.LexBoxApi.Controllers;

public class FwLiteReleaseControllerTests
{
    [Fact]
    public void AppInstallerActionAcceptsHeadOnTheSamePathAsGet()
    {
        //App Installer HEADs the .appinstaller URL and requires Content-Length. [HttpGet] does not
        //match HEAD, so this attribute is load-bearing; File() then omits the body.
        var method = typeof(FwLiteReleaseController).GetMethod(nameof(FwLiteReleaseController.AppInstaller))!;
        var get = method.GetCustomAttributes(typeof(HttpGetAttribute), inherit: true)
            .Should().ContainSingle().Subject.As<HttpGetAttribute>();
        var head = method.GetCustomAttributes(typeof(HttpHeadAttribute), inherit: true)
            .Should().ContainSingle().Subject.As<HttpHeadAttribute>();
        head.Template.Should().Be(get.Template).And.EndWith(".appinstaller");
    }
}
