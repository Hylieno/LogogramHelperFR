using Dalamud.Configuration;
using System;
using System.Collections.Generic;

namespace LogogramHelper;

[Serializable]
public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public Dictionary<int, int> SavedLogogramStock { get; set; } = new();
}
