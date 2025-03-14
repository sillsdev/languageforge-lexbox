﻿using LcmCrdt;
using SIL.Harmony;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using SIL.Harmony.Db;
using SIL.Harmony.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using FwLiteWeb.Hubs;
using Microsoft.OpenApi.Models;
using MiniLcm.Models;

namespace FwLiteWeb.Routes;

public static class HistoryRoutes
{
    public static IEndpointConventionBuilder MapHistoryRoutes(this WebApplication app)
    {
        var group = app.MapGroup("/api/history/{project}").WithOpenApi(operation =>
        {
            operation.Parameters.Add(new OpenApiParameter()
            {
                Name = CrdtMiniLcmApiHub.ProjectRouteKey, In = ParameterLocation.Path, Required = true
            });
            return operation;
        });
        group.MapGet("/snapshot/{snapshotId:guid}",
            async (Guid snapshotId, HistoryService historyService) => await historyService.GetSnapshot(snapshotId));
        group.MapGet("/snapshot/commit/{commitId}",
            async (Guid commitId, Guid entityId, HistoryService historyService) =>
                await historyService.GetObject(commitId, entityId));
        group.MapGet("/{entityId}",
            (Guid entityId, HistoryService historyService) => historyService.GetHistory(entityId));
        return group;
    }
}
