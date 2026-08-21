using System;
using System.Collections.Generic;
using LogogramHelper.Classes;

namespace LogogramHelper;

[Serializable]
public sealed class LogosAction
{
    public uint Id { get; set; }
    public uint IconID { get; set; }
    public string? Duration { get; set; }
    public string? Cast { get; set; }
    public string? Recast { get; set; }
    public List<List<Recipe>> Recipes { get; set; } = new();
    public List<uint> Roles { get; set; } = new();
}
