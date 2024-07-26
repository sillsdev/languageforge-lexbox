﻿namespace MiniLcm;

public class SemanticDomain : IObjectWithId
{
    public required Guid Id { get; set; }
    public required MultiString Name { get; set; }
    public required string Code { get; set; }
}
