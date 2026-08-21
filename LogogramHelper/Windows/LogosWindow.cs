using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace LogogramHelper.Windows;

public sealed class LogosWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private LogosAction action;

    public LogosWindow(Plugin plugin) : base("Détails Logos", ImGuiWindowFlags.None)
    {
        this.plugin = plugin;
        action = plugin.LogosActions.First();
        ShowCloseButton = true;
        Size = new Vector2(620f, 500f);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }
    public void SetDetails(LogosAction value) => action = value;

    public override void Draw()
    {
        string actionName = plugin.GetFrenchActionName(action.Id);
        float scale = ImGui.GetFontSize() / 17f;
        ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + Math.Max(250f * scale, ImGui.GetContentRegionAvail().X));

        var texture = Plugin.TextureProvider.GetFromGameIcon(action.IconID).GetWrapOrEmpty();
        ImGui.Image(texture.Handle, new Vector2(40, 40) * scale);
        ImGui.SameLine();
        ImGui.BeginGroup();
        ImGui.TextUnformatted(actionName);
        foreach (uint role in action.Roles)
        {
            ImGui.SameLine();
            var roleTex = Plugin.TextureProvider.GetFromGameIcon(role).GetWrapOrEmpty();
            ImGui.Image(roleTex.Handle, new Vector2(18, 18) * scale);
        }

        var details = new List<string>();
        if (!string.IsNullOrWhiteSpace(action.Duration)) details.Add($"DURÉE : {action.Duration}");
        if (!string.IsNullOrWhiteSpace(action.Cast)) details.Add($"INCANT. : {action.Cast}");
        if (!string.IsNullOrWhiteSpace(action.Recast)) details.Add($"RECHARGE : {action.Recast}");
        if (details.Count > 0) ImGui.TextDisabled(string.Join(" · ", details));
        ImGui.EndGroup();

        ImGui.Spacing();
        string desc = plugin.GetFrenchActionDescription(action.Id);
        if (!string.IsNullOrWhiteSpace(desc)) ImGui.TextWrapped(desc);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextUnformatted("Combinaisons");

        if (ImGui.BeginTable($"recipes{action.Id}", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Possible", ImGuiTableColumnFlags.WidthFixed, 70f);
            ImGui.TableSetupColumn("Recette", ImGuiTableColumnFlags.WidthStretch);
            foreach (var recipe in action.Recipes)
            {
                int possible = int.MaxValue;
                var names = new List<string>();
                foreach (var ingredient in recipe)
                {
                    int stock = plugin.LogogramStock.TryGetValue(ingredient.LogogramID, out int n) ? n : 0;
                    possible = Math.Min(possible, stock / ingredient.Quantity);
                    string name = plugin.GetFrenchItemName((uint)ingredient.LogogramID);
                    names.Add(ingredient.Quantity > 1 ? $"{name} ×{ingredient.Quantity}" : name);
                }
                if (possible == int.MaxValue) possible = 0;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                if (possible > 0) ImGui.TextUnformatted(possible.ToString());
                else ImGui.TextDisabled("0");
                ImGui.TableNextColumn();
                ImGui.TextWrapped(string.Join(" + ", names));
            }
            ImGui.EndTable();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.TextUnformatted("Obtention");
        var used = action.Recipes.SelectMany(x => x).Select(x => x.LogogramID).Distinct();
        foreach (int id in used)
        {
            string name = plugin.GetFrenchItemName((uint)id);
            string shards = string.Join(", ", plugin.GetSourceShards(id).Select(x => plugin.GetFrenchItemName((uint)x)));
            ImGui.BulletText($"{name} — {shards}");
        }

        ImGui.PopTextWrapPos();
    }
}
