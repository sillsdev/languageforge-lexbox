// Copyright (c) $year$ SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace MiniLcm.Models;

public class Publication : IPossibility
{
    public required Guid Id { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid[] GetReferences()
    {
        return [];
    }

    public void RemoveReference(Guid id, DateTimeOffset time)
    {
        return;
    }

    public IObjectWithId Copy()
    {
        return new Publication()
        {
            Id = Id,
            DeletedAt = DeletedAt,
            Name = Name.Copy()
        };
    }
    public virtual MultiString Name { get; set; } = new();
}
