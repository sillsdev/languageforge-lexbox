﻿using LexCore.Entities;

namespace LexCore.ServiceInterfaces;

public interface IHgService
{
    Task InitRepo(string code);
    Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode, ProjectMigrationStatus migrationStatus);
    Task<Changeset[]> GetChangesets(string projectCode, ProjectMigrationStatus migrationStatus);
    Task<ProjectType> DetermineProjectType(string projectCode, ProjectMigrationStatus migrationStatus);
    Task DeleteRepo(string code);
    Task SoftDeleteRepo(string code, string deletedRepoSuffix);
    Task<string?> BackupRepo(string code);
    Task ResetRepo(string code);
    Task<bool> MigrateRepo(Project project, CancellationToken cancellationToken);
    Task FinishReset(string code, Stream zipFile);
    Task<string> VerifyRepo(string code, CancellationToken token);
    Task<int?> GetLexEntryCount(string code);
    Task<string?> GetRepositoryIdentifier(Project project);
    Task<string> ExecuteHgRecover(string code, CancellationToken token);
}
