using LexBoxApi.Services;
using LexCore.Config;
using LexCore.Entities;
using LexCore.Exceptions;
using LexSyncReverseProxy;
using Shouldly;
using Testing.Fixtures;

namespace Testing.LexCore.Services;

public class HgServiceTests
{
    private const string LexboxHgWeb = "https://hg.lexbox";
    private const string LexboxResumable = "https://resumable.lexbox";
    private const string RedmineResumable = "https://resumable.redmine";
    private const string RedminePublic = "https://public.redmine";
    private const string RedminePrivate = "https://private.redmine";
    private HgConfig _hgConfig = new HgConfig
    {
        RepoPath = "nothing",
        HgWebUrl = LexboxHgWeb,
        HgResumableUrl = LexboxResumable,
        PublicRedmineHgWebUrl = RedminePublic,
        PrivateRedmineHgWebUrl = RedminePrivate,
        RedmineHgResumableUrl = RedmineResumable
    };

    [Theory]
    //lexbox
    [InlineData(HgType.hgWeb, ProjectMigrationStatus.Migrated, LexboxHgWeb)]
    [InlineData(HgType.resumable, ProjectMigrationStatus.Migrated, LexboxResumable)]
    //redmine
    [InlineData(HgType.hgWeb, ProjectMigrationStatus.PublicRedmine, RedminePublic)]
    [InlineData(HgType.hgWeb, ProjectMigrationStatus.PrivateRedmine, RedminePrivate)]
    [InlineData(HgType.resumable, ProjectMigrationStatus.PublicRedmine, RedmineResumable)]
    [InlineData(HgType.resumable, ProjectMigrationStatus.PrivateRedmine, RedmineResumable)]
    public void DetermineProjectPrefixWorks(HgType type, ProjectMigrationStatus status, string expectedUrl)
    {
        HgService.DetermineProjectUrlPrefix(type, "test", status, _hgConfig).ShouldBe(expectedUrl);
    }

    [Theory]
    [InlineData(HgType.hgWeb)]
    [InlineData(HgType.resumable)]
    public void ThrowsIfMigrating(HgType type)
    {
        var act = () => HgService.DetermineProjectUrlPrefix(type, "test", ProjectMigrationStatus.Migrating, _hgConfig);
        act.ShouldThrow<ProjectMigratingException>();
    }
}
