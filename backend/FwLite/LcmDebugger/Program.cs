// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using LcmDebugger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Exceptions;
using Refit;
using SIL.Harmony;
using SIL.Harmony.Core;

var builder = Host.CreateApplicationBuilder();
//slows down import to log all sql.
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
builder.Services.AddFwDataBridge();
builder.Services.AddLcmCrdtClient();
builder.Services.AddFwLiteProjectSync();

using var app = builder.Build();

await using var scope = app.Services.CreateAsyncScope();

await scope.ServiceProvider.PrintAllEntries("sena-3");
