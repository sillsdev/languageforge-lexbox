﻿using Gridify;
using MiniLcm.Filtering;

namespace LcmCrdt;

public class LcmCrdtConfig
{
    public string ProjectPath { get; set; } = Path.GetFullPath(".");
    public GridifyMapper<Entry> Mapper { get; set; } = EntryFilter.NewMapper(new EntryFilterMapProvider());
}
