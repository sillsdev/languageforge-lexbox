﻿namespace MiniLcm;

public class InMemoryApi : ILexboxApi
{
    private readonly List<Entry> _entries =
    [
        new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "apple" },
                }
            },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = new MultiString
                    {
                        Values =
                        {
                            {"en", "fruit"}
                        }
                    },
                    Definition = new MultiString
                    {
                        Values =
                        {
                            { "en", "A red or green fruit that grows on a tree" },
                        }
                    },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Id = Guid.NewGuid(),
                            Sentence = new MultiString
                            {
                                Values =
                                {
                                    { "en", "The apple fell from the tree." },
                                }
                            }
                        },
                    ],
                },
            ],
        },
        new Entry
        {
            Id = Guid.NewGuid(),
            LexemeForm = new MultiString
            {
                Values =
                {
                    { "en", "banana" },
                }
            },
            Senses =
            [
                new Sense
                {
                    Id = Guid.NewGuid(),
                    Gloss = new MultiString
                    {
                        Values =
                        {
                            { "en", "fruit" }
                        }
                    },
                    Definition = new MultiString
                    {
                        Values =
                        {
                            { "en", "A yellow fruit that grows on a tree" },
                        }
                    },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Id = Guid.NewGuid(),
                            Sentence = new MultiString
                            {
                                Values =
                                {
                                    { "en", "The banana fell from the tree." },
                                }
                            }
                        },
                    ],
                },
            ],
        },
    ];

    private readonly WritingSystems _writingSystems = new WritingSystems{
        Analysis =
        [
            new WritingSystem { Id = "en", Name = "English", Abbreviation = "en", Font = "Arial" },
        ],
        Vernacular =
        [
            new WritingSystem { Id = "en", Name = "English", Abbreviation = "en", Font = "Arial" },
        ]
    };

    private readonly string[] _exemplars = Enumerable.Range('a', 'z').Select(c => ((char)c).ToString()).ToArray();

    public Task<Entry> CreateEntry(Entry entry)
    {
        if (entry.Id == default) entry.Id = Guid.NewGuid();
        _entries.Add(entry);
        return Task.FromResult(entry);
    }

    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence)
    {
        if (exampleSentence.Id == default) exampleSentence.Id = Guid.NewGuid();
        var entry = _entries.Single(e => e.Id == entryId);
        var sense = entry.Senses.Single(s => s.Id == senseId);
        sense.ExampleSentences.Add(exampleSentence);
        return Task.FromResult(exampleSentence);
    }

    public Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        if (sense.Id == default) sense.Id = Guid.NewGuid();
        var entry = _entries.Single(e => e.Id == entryId);
        entry.Senses.Add(sense);
        return Task.FromResult(sense);
    }


    public Task DeleteEntry(Guid id)
    {
        _entries.RemoveAll(e => e.Id == id);
        return Task.CompletedTask;
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        var entry = _entries.Single(e => e.Id == entryId);
        var sense = entry.Senses.Single(s => s.Id == senseId);
        sense.ExampleSentences.RemoveAll(es => es.Id == exampleSentenceId);
        return Task.CompletedTask;
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        var entry = _entries.Single(e => e.Id == entryId);
        entry.Senses.RemoveAll(s => s.Id == senseId);
        return Task.CompletedTask;
    }

    public Task<Entry[]> GetEntries(string exemplar, QueryOptions? options = null)
    {
        var entries = _entries.Where(e => e.LexemeForm.Values["en"].StartsWith(exemplar)).OfType<Entry>().ToArray();
        return Task.FromResult(entries);
    }

    public Task<Entry[]> GetEntries(QueryOptions? options = null)
    {
        return Task.FromResult(_entries.OfType<Entry>().ToArray());
    }

    public Task<Entry> GetEntry(Guid id)
    {
        var entry = _entries.Single(e => e.Id == id);
        return Task.FromResult(entry as Entry);
    }

    public Task<string[]> GetExemplars()
    {
        return Task.FromResult(_exemplars);
    }

    public Task<WritingSystems> GetWritingSystems()
    {
        return Task.FromResult(_writingSystems);
    }

    public Task<Entry[]> SearchEntries(string query, QueryOptions? options = null)
    {
        var entries = _entries.Where(e => e.LexemeForm.Values["en"].Contains(query)).OfType<Entry>().ToArray();
        return Task.FromResult(entries);
    }

    public UpdateBuilder<T> CreateUpdateBuilder<T>() where T : class
    {
        return new UpdateBuilder<T>();
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        var entry = _entries.Single(e => e.Id == id);
        update.Apply(entry);
        return Task.FromResult(entry as Entry);
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        var entry = _entries.Single(e => e.Id == entryId);
        var sense = entry.Senses.Single(s => s.Id == senseId);
        var es = sense.ExampleSentences.Single(es => es.Id == exampleSentenceId);
        update.Apply(es);
        return Task.FromResult(es);
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        var entry = _entries.Single(e => e.Id == entryId);
        var s = entry.Senses.Single(s => s.Id == senseId);
        update.Apply(s);
        return Task.FromResult(s);
    }
}

internal static class Helpers
{
    public static void RemoveAll<T>(this IList<T> list, Func<T, bool> predicate)
    {
        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (predicate(list[i]))
            {
                list.RemoveAt(i);
            }
        }
    }
}