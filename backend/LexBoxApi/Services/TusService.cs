using System.Net;
using System.Text;
using LexBoxApi.Config;
using LexCore.Utils;
using LexSyncReverseProxy;
using Microsoft.Extensions.Options;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using tusdotnet.Stores;
using Path = System.IO.Path;

namespace LexBoxApi.Services;

public class TusService
{
    private readonly TusConfig _config;
    private readonly ProjectService _projectService;

    public TusService(IOptions<TusConfig> config, ProjectService projectService)
    {
        _projectService = projectService;
        _config = config.Value;
        Directory.CreateDirectory(Path.GetFullPath(_config.TestUploadPath));
    }

    public Task<DefaultTusConfiguration> GetTestConfig(HttpContext context)
    {
        return Task.FromResult(new DefaultTusConfiguration
        {
            Store = new TusDiskStore(Path.GetFullPath(_config.TestUploadPath)),
            Events = new Events
            {
                OnBeforeCreateAsync = createContext =>
                {

                    var filetype = GetFiletype(createContext.Metadata);
                    if (string.IsNullOrEmpty(filetype))
                    {
                        createContext.FailRequest(HttpStatusCode.BadRequest, "unknown file type");
                        return Task.CompletedTask;
                    }
                    if (filetype != "application/png")
                    {
                        createContext.FailRequest(HttpStatusCode.BadRequest, $"file type {filetype} is not allowed");
                        return Task.CompletedTask;
                    }
                    //validate the upload before it begins
                    return Task.CompletedTask;
                },
                OnFileCompleteAsync = completeContext =>
                {
                    //do something with the uploaded file
                    return Task.CompletedTask;
                }
            }
        });
    }

    private string? GetFiletype(Dictionary<string, Metadata> metadata)
    {
        if (!metadata.TryGetValue("filetype", out var filetypeMetadata)) return null;
        return filetypeMetadata.GetString(Encoding.UTF8);
    }

    public Task<DefaultTusConfiguration> GetResetZipUploadConfig()
    {
        return Task.FromResult(new DefaultTusConfiguration
        {
            Store = new TusDiskStore(Path.GetFullPath(_config.ResetUploadPath)),
            Events = new Events
            {
                OnBeforeCreateAsync = BeforeStartZipUpload, OnFileCompleteAsync = FinishZipUpload
            }
        });
    }

    private async Task BeforeStartZipUpload(BeforeCreateContext createContext)
    {
        var projectCode = createContext.HttpContext.Request.GetProjectCode();
        if (string.IsNullOrEmpty(projectCode))
        {
            createContext.FailRequest(HttpStatusCode.BadRequest, "Missing project code");
            return;
        }

        if (!await _projectService.ProjectExists(projectCode))
        {
            createContext.FailRequest(HttpStatusCode.NotFound, $"Project {projectCode} not found");
            return;
        }

        var filetype = GetFiletype(createContext.Metadata);
        if (string.IsNullOrEmpty(filetype))
        {
            createContext.FailRequest(HttpStatusCode.BadRequest, "unknown file type");
            return;
        }

        if (filetype != "application/zip")
        {
            createContext.FailRequest(HttpStatusCode.BadRequest, $"file type {filetype} is not allowed, only application/zip is allowed");
            return;
        }
    }

    private async Task FinishZipUpload(FileCompleteContext completeContext)
    {
        try
        {
            var projectCode = completeContext.HttpContext.Request.GetProjectCode();
            ArgumentException.ThrowIfNullOrEmpty(projectCode);
            var tusFile = await completeContext.GetFileAsync();
            await using var fileStream = await tusFile.GetContentAsync(completeContext.CancellationToken);
            await _projectService.FinishReset(projectCode, fileStream);
        }
        finally
        {
            if (completeContext.Store is ITusTerminationStore terminationStore)
            {
                await terminationStore.DeleteFileAsync(completeContext.FileId, completeContext.CancellationToken);
            }
        }
    }
}
