using System.Collections.Generic;

namespace LogogramHelper.Classes;

internal sealed class LogogramItem
{
    public ulong Id { get; set; }
    public List<int> Contents { get; set; } = new();
}
