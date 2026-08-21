using System;

namespace LogogramHelper.Classes;

[Serializable]
public sealed class Logogram
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
