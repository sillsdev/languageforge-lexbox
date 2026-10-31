using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace MiniLcmWeb;

/// <summary>
/// OpenAPI summary and description of a MiniLcm route, shared by every host that maps or forwards it.
/// </summary>
public record MiniLcmRouteDoc(string Summary, string Description);

public static class MiniLcmRouteDocs
{
    private const string Search =
        "`search` matches headwords, lexeme forms, citation forms and glosses; sort by `SearchRelevance` to get the best matches first.";
    private const string Exemplar =
        "`exemplarValue` with `exemplarWritingSystem` keeps entries whose headword starts with that letter, as in a dictionary's letter headings.";

    public static readonly MiniLcmRouteDoc GetWritingSystems = new("Get the writing systems",
        "The project's vernacular and analysis writing systems, in display order. Their ids key every multilingual field, e.g. `lexemeForm.en`.");

    public static readonly MiniLcmRouteDoc GetEntries = new("List or search entries",
        $"Entries with their senses and example sentences, sorted by `sortField` (headword by default) in `sortWritingSystem`, paged with `count` and `offset`. {Search} {Exemplar} `gridifyFilter` narrows the list further. The response is a JSON array streamed as it is read.");

    public static readonly MiniLcmRouteDoc CountEntries = new("Count entries",
        "How many entries `GET entries` would return across all pages for the same `search` and `gridifyFilter`, e.g. to show \"20 of 135\" next to a page of search results.");

    public static readonly MiniLcmRouteDoc GetEntry = new("Get an entry",
        "One entry with its senses and example sentences. 404 when there is no entry with this id.");

    public static readonly MiniLcmRouteDoc GetEntryIndex = new("Find an entry's position",
        "The zero-based position of the entry in the list `GET entries` returns for the same `search`, sort, exemplar and `gridifyFilter`, so a client can open the page that holds it. -1 when the entry is not in that list.");

    public static readonly MiniLcmRouteDoc GetSense = new("Get a sense",
        "One sense with its example sentences; `entryId` names the entry it belongs to. 404 when there is no sense with this id.");

    public static readonly MiniLcmRouteDoc GetPartsOfSpeech = new("List parts of speech",
        "Every part of speech in the project; senses refer to one by `partOfSpeechId`.");

    public static readonly MiniLcmRouteDoc GetSemanticDomains = new("List semantic domains",
        "Every semantic domain in the project, with its code and multilingual name. This is the whole list (often well over a thousand), so fetch it once and cache it.");

    public static readonly MiniLcmRouteDoc GetPublications = new("List publications",
        "Every publication in the project; an entry's `publishIn` lists the ones it appears in.");

    public static readonly MiniLcmRouteDoc GetComplexFormTypes = new("List complex form types",
        "Every complex form type in the project, e.g. compound or idiom; an entry's `complexFormTypes` refers to them.");

    public static readonly MiniLcmRouteDoc GetMorphTypes = new("List morph types",
        "Every morph type in the project, e.g. root, stem or prefix, with the prefix and postfix tokens written around forms of that type.");

    public static readonly MiniLcmRouteDoc CreateEntry = new("Create an entry",
        "Creates the entry as given, including its senses, example sentences, complex forms and components, and adds it to the main publication. Ids may be supplied by the caller. Returns the created entry with a `Location` header.");

    public static readonly MiniLcmRouteDoc UpdateEntry = new("Update an entry",
        "Applies the difference between `before` (the entry as the caller last read it) and `after` (the entry as it should be now), so concurrent edits to other fields are kept. Both ids must match the route id. Returns the entry as stored.");

    public static readonly MiniLcmRouteDoc DeleteEntry = new("Delete an entry",
        "Deletes the entry with its senses and example sentences.");

    public static TBuilder WithDoc<TBuilder>(this TBuilder builder, MiniLcmRouteDoc doc, string? note = null)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.WithSummary(doc.Summary)
            .WithDescription(note is null ? doc.Description : $"{doc.Description}\n\n{note}");
    }
}
