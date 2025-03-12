// See https://aka.ms/new-console-template for more information

using FwDataMiniLcmBridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddFwDataBridge();

var app = builder.Build();

var fieldWorksProjectList = app.Services.GetRequiredService<FieldWorksProjectList>();
var fwDataProject = fieldWorksProjectList.GetProject("sbe-flex") ?? throw new InvalidOperationException("Could not find project");
var fwDataFactory = app.Services.GetRequiredService<FwDataFactory>();
var miniLcmApi = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, false);
var entries = await miniLcmApi.GetEntries().ToArrayAsync();


fwDataFactory.Dispose();
