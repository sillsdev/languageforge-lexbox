﻿using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Tests.MiniLcmTests;

[Collection(ProjectLoaderFixture.Name)]
public class UpdateEntryTests(ProjectLoaderFixture fixture) : UpdateEntryTestsBase
{
    protected override Task<IMiniLcmApi> NewApi()
    {
        return Task.FromResult<IMiniLcmApi>(fixture.NewProjectApi("update-entry-test", "en", "en"));
    }

    protected override bool ApiUsesImplicitOrdering => true;

    [Fact]
    public async Task UpdateEntry_WithNullLexemeFormOA_CreatesNewLexemeForm()
    {
        // Arrange
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = new MultiString { { "en", "test" } }
        });

        var fwApi = (FwDataMiniLcmApi)Api;
        var lexEntry = fwApi.EntriesRepository.GetObject(entry.Id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Set LexemeFormOA to null",
            "Restore LexemeFormOA",
            fwApi.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                lexEntry.LexemeFormOA = null;
            });

        // Act
        var updatedEntry = await Api.UpdateEntry(entry, entry with
        {
            LexemeForm = new MultiString { { "en", "updated test" } }
        });

        // Assert
        updatedEntry.LexemeForm.Should().NotBeNull();
        updatedEntry.LexemeForm["en"].Should().Be("updated test");
    }
}
