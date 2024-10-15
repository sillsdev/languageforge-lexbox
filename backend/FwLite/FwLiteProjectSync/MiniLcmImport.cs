using LcmCrdt;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Models;

namespace FwLiteProjectSync;

public class MiniLcmImport(ILogger<MiniLcmImport> logger)
{
    public async Task ImportProject(IMiniLcmApi importTo, IMiniLcmApi importFrom, int entryCount)
    {
        var writingSystems = await importFrom.GetWritingSystems();
        foreach (var ws in writingSystems.Analysis)
        {
            await importTo.CreateWritingSystem(WritingSystemType.Analysis, ws);
            logger.LogInformation("Imported ws {WsId}", ws.Id);
        }

        foreach (var ws in writingSystems.Vernacular)
        {
            await importTo.CreateWritingSystem(WritingSystemType.Vernacular, ws);
            logger.LogInformation("Imported ws {WsId}", ws.Id);
        }

        await foreach (var partOfSpeech in importFrom.GetPartsOfSpeech())
        {
            await importTo.CreatePartOfSpeech(partOfSpeech);
            logger.LogInformation("Imported part of speech {Id}", partOfSpeech.Id);
        }

        await foreach (var complexFormType in importFrom.GetComplexFormTypes())
        {
            await importTo.CreateComplexFormType(complexFormType);
            logger.LogInformation("Imported complex form type {Id}", complexFormType.Id);
        }


        var semanticDomains = importFrom.GetSemanticDomains();
        var entries = importFrom.GetEntries(new QueryOptions(Count: 100_000, Offset: 0));
        if (importTo is CrdtMiniLcmApi crdtLexboxApi)
        {
            logger.LogInformation("Importing semantic domains");
            await crdtLexboxApi.BulkImportSemanticDomains(semanticDomains.ToBlockingEnumerable());
            logger.LogInformation("Importing {Count} entries", entryCount);
            await crdtLexboxApi.BulkCreateEntries(entries);
        }
        else
        {
            await foreach (var semanticDomain in semanticDomains)
            {
                await importTo.CreateSemanticDomain(semanticDomain);
                logger.LogTrace("Imported semantic domain {Id}", semanticDomain.Id);
            }

            var index = 0;
            await foreach (var entry in entries)
            {
                await importTo.CreateEntry(entry);
                logger.LogTrace("Imported entry, {Index} of {Count} {Id}", index++, entryCount, entry.Id);
            }
        }

        logger.LogInformation("Imported {Count} entries", entryCount);
    }
}
