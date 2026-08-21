using System;

namespace LogogramHelper.Classes;

[Serializable]
public sealed class Recipe
{
    public int LogogramID { get; set; }
    public int Quantity { get; set; }
}
