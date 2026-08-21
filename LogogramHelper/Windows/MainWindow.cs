using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Linq;
using System.Diagnostics;
using System.Numerics;

namespace LogogramHelper.Windows;

public sealed class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private string filter = string.Empty;

    public MainWindow(Plugin plugin) : base("Logogram Helper FR+", ImGuiWindowFlags.NoResize)
    {
        this.plugin = plugin;
        ShowCloseButton = true;
        Size = new Vector2(620f, 560f);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public void Dispose() { }

    public override void OnClose()
    {
        plugin.NotifyMainWindowClosed();
    }

    public override void Draw()
    {
        float scale = ImGui.GetFontSize() / 17f;
        ImGui.SetNextItemWidth(400f * scale);
        ImGui.InputTextWithHint("##filter", "Filtrer les actions Logos...", ref filter, 80, ImGuiInputTextFlags.AutoSelectAll);

        ImGui.SameLine();
        if (ImGui.Button("Actualiser"))
            plugin.TryCaptureLogogramStock();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Actualiser le stock depuis le mélangeur Logos");

        ImGui.SameLine();
        if (ImGui.Button("Ko-fi"))
            Process.Start(new ProcessStartInfo { FileName = "https://ko-fi.com/apetih", UseShellExecute = true });
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Soutenir l'auteur original sur Ko-fi");

        ImGui.TextDisabled("Le stock affiché est le dernier stock enregistré dans Eureka.");
        ImGui.Spacing();

        int visibleIndex = 0;
        var style = ImGui.GetStyle();
        float iconSize = 40f * scale;
        float buttonWidth = iconSize + (style.FramePadding.X * 2f);
        float buttonStep = buttonWidth + style.ItemSpacing.X;
        float usableWidth = Math.Max(buttonWidth,
            ImGui.GetContentRegionAvail().X - style.ScrollbarSize - style.ItemSpacing.X - 2f);
        int iconsPerRow = Math.Max(1, (int)((usableWidth + style.ItemSpacing.X) / buttonStep));
        foreach (var action in plugin.LogosActions)
        {
            string name = plugin.GetFrenchActionName(action.Id);
            bool match = string.IsNullOrWhiteSpace(filter) || name.Contains(filter, StringComparison.OrdinalIgnoreCase);
            var tint = new Vector4(1f, 1f, 1f, match ? 1f : 0.22f);
            var tex = Plugin.TextureProvider.GetFromGameIcon(action.IconID).GetWrapOrEmpty();
            if (ImGui.ImageButton(tex.Handle, new Vector2(40, 40) * scale, Vector2.Zero, Vector2.One, -1, new Vector4(0,0,0,1), tint))
                plugin.DrawLogosDetailUI(action);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(name);
            visibleIndex++;
            if (visibleIndex % iconsPerRow != 0) ImGui.SameLine();
        }

        ImGui.Spacing();
        if (ImGui.CollapsingHeader("Obtention des logogrammes"))
        {
            ImGui.TextDisabled("Chaque logogramme est associé à l'éclat qui peut le contenir.");
            if (ImGui.BeginTable("sources", 3, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Logogramme", ImGuiTableColumnFlags.WidthStretch, 2.2f);
                ImGui.TableSetupColumn("Stock", ImGuiTableColumnFlags.WidthFixed, 60f);
                ImGui.TableSetupColumn("Éclat", ImGuiTableColumnFlags.WidthStretch, 2f);
                ImGui.TableHeadersRow();

                foreach (var logogram in plugin.Logograms.Values.OrderBy(x => plugin.GetFrenchItemName((uint)x.Id)))
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(plugin.GetFrenchItemName((uint)logogram.Id));
                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(plugin.LogogramStock.TryGetValue(logogram.Id, out int n) ? n.ToString() : "0");
                    ImGui.TableNextColumn();
                    var shards = plugin.GetSourceShards(logogram.Id).Select(x => plugin.GetFrenchItemName((uint)x));
                    ImGui.TextWrapped(string.Join(", ", shards));
                }
                ImGui.EndTable();
            }
        }
    }
}
