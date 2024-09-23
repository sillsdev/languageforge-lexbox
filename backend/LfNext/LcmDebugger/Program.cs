// See https://aka.ms/new-console-template for more information

using FwDataMiniLcmBridge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder();
builder.Services.AddFwDataBridge();

var app = builder.Build();

var fwDataFactory = app.Services.GetRequiredService<FwDataFactory>();
var miniLcmApi = fwDataFactory.GetFwDataMiniLcmApi("fruit", false);
await miniLcmApi.GetEntries().ToArrayAsync();
var complexEntryTypesOa = miniLcmApi.Cache.LangProject.LexDbOA.ComplexEntryTypesOA;
