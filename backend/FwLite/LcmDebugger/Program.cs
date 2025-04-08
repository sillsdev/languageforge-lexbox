// See https://aka.ms/new-console-template for more information

using FwDataMiniLcmBridge;
using FwLiteProjectSync;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddFwDataBridge();
builder.Services.AddLcmCrdtClient();
builder.Services.AddFwLiteProjectSync();

using var app = builder.Build();

var fieldWorksProjectList = app.Services.GetRequiredService<FieldWorksProjectList>();
var fwDataProject = fieldWorksProjectList.GetProject("sena-3") ?? throw new InvalidOperationException("Could not find project");

var importService = app.Services.GetRequiredService<MiniLcmImport>();
// await importService.Import(fwDataProject);


var fwDataFactory = app.Services.GetRequiredService<FwDataFactory>();
var miniLcmApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, false);
var entries = await miniLcmApi.GetEntries().ToArrayAsync();


fwDataFactory.Dispose();
