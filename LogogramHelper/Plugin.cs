using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Memory;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using LogogramHelper.Classes;
using LogogramHelper.Windows;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LogogramHelper;

public sealed class Plugin : IDalamudPlugin
{
    public string Name => "Logogram Helper FR+";
    private const string CommandName = "/logo";

    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;

    public WindowSystem WindowSystem { get; } = new("LogogramHelperFR");
    public MainWindow MainWindow { get; }
    public LogosWindow LogosWindow { get; }

    internal List<LogosAction> LogosActions = new();
    internal IDictionary<int, Logogram> Logograms = new Dictionary<int, Logogram>();
    internal IDictionary<ulong, LogogramItem> LogogramItems = new Dictionary<ulong, LogogramItem>();
    internal IDictionary<int, int> LogogramStock = new Dictionary<int, int>();
    internal Configuration Config { get; }

    private bool manualOpen;
    private bool manipulatorWasOpen;

    public Plugin()
    {
        LoadData();
        Config = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        LogogramStock = new Dictionary<int, int>(Config.SavedLogogramStock);

        MainWindow = new MainWindow(this);
        LogosWindow = new LogosWindow(this);
        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(LogosWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnLogoCommand)
        {
            HelpMessage = "Ouvre/ferme Logogram Helper, y compris hors d'Eureka."
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenMainUi += OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        AddonLifecycle.RegisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", ItemDetailOnUpdate);
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenMainUi -= OpenMainUi;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
        AddonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "ItemDetail", ItemDetailOnUpdate);
        CommandManager.RemoveHandler(CommandName);
        WindowSystem.RemoveAllWindows();
    }

    private void OpenMainUi()
    {
        manualOpen = true;
        MainWindow.IsOpen = true;
    }

    private void OpenConfigUi()
    {
        OpenMainUi();
    }

    private void OnLogoCommand(string command, string args)
    {
        manualOpen = !MainWindow.IsOpen || !manualOpen;
        MainWindow.IsOpen = manualOpen;
        if (!MainWindow.IsOpen)
            LogosWindow.IsOpen = false;
    }

    private void DrawUI()
    {
        bool manipulatorOpen = GameGui.GetAddonByName("EurekaMagiciteItemSynthesis", 1) != IntPtr.Zero;
        bool shardListOpen = GameGui.GetAddonByName("EurekaMagiciteItemShardList", 1) != IntPtr.Zero;

        if (manipulatorOpen || shardListOpen)
            TryCaptureLogogramStock();

        if (manipulatorOpen && !manipulatorWasOpen)
            MainWindow.IsOpen = true;

        if (!manipulatorOpen && manipulatorWasOpen && !manualOpen)
        {
            MainWindow.IsOpen = false;
            LogosWindow.IsOpen = false;
        }

        manipulatorWasOpen = manipulatorOpen;
        WindowSystem.Draw();
    }

    internal unsafe void TryCaptureLogogramStock()
    {
        try
        {
            var holder = Framework.Instance()->GetUIModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
            var arr = holder.GetNumberArrayData(137);
            if (arr == null || arr->IntArray == null) return;

            int count = arr->IntArray[0];
            if (count <= 0 || count > 100) return;

            bool changed = false;
            for (int i = 1; i <= count; i++)
            {
                int stock = arr->IntArray[4 * i];
                int id = arr->IntArray[(4 * i) + 1];
                if (id <= 0) continue;

                if (!LogogramStock.TryGetValue(id, out int old) || old != stock)
                {
                    LogogramStock[id] = stock;
                    changed = true;
                }
            }

            if (changed)
            {
                Config.SavedLogogramStock = new Dictionary<int, int>(LogogramStock);
                PluginInterface.SavePluginConfig(Config);
            }
        }
        catch (Exception ex)
        {
            Log.Debug(ex, "Impossible de lire le stock de logogrammes pour le moment.");
        }
    }

    private void LoadData()
    {
        string baseDir = PluginInterface.AssemblyLocation.Directory?.FullName!;
        Logograms = JsonConvert.DeserializeObject<List<Logogram>>(File.ReadAllText(Path.Combine(baseDir, "logograms.json")))!
            .ToDictionary(x => x.Id, x => x);
        LogogramItems = JsonConvert.DeserializeObject<List<LogogramItem>>(File.ReadAllText(Path.Combine(baseDir, "itemContents.json")))!
            .ToDictionary(x => x.Id, x => x);
        LogosActions = JsonConvert.DeserializeObject<List<LogosAction>>(File.ReadAllText(Path.Combine(baseDir, "logosActions.json")))!;
    }

    internal string GetFrenchItemName(uint id)
    {
        try
        {
            var sheet = DataManager.GetExcelSheet<Lumina.Excel.Sheets.Item>(Dalamud.Game.ClientLanguage.French);
            var row = sheet.GetRow(id);
            var text = row.Name.ExtractText();
            return string.IsNullOrWhiteSpace(text) ? $"Objet #{id}" : text;
        }
        catch { return $"Objet #{id}"; }
    }

    internal string GetFrenchActionName(uint id)
    {
        try
        {
            var sheet = DataManager.GetExcelSheet<Lumina.Excel.Sheets.Action>(Dalamud.Game.ClientLanguage.French);
            string name = sheet.GetRow(id).Name.ExtractText();
            int colon = name.IndexOf(':');
            if (colon >= 0 && name[..colon].Contains("logo", StringComparison.OrdinalIgnoreCase))
                name = name[(colon + 1)..].Trim();
            return name;
        }
        catch { return $"Action #{id}"; }
    }

    internal string GetFrenchActionDescription(uint id)
    {
        try
        {
            var sheet = DataManager.GetExcelSheet<Lumina.Excel.Sheets.ActionTransient>(Dalamud.Game.ClientLanguage.French);
            return sheet.GetRow(id).Description.ExtractText();
        }
        catch { return string.Empty; }
    }

    internal IEnumerable<ulong> GetSourceShards(int logogramId) =>
        LogogramItems.Where(kv => kv.Value.Contents.Contains(logogramId)).Select(kv => kv.Key);

    internal void DrawLogosDetailUI(LogosAction action)
    {
        LogosWindow.SetDetails(action);
        LogosWindow.IsOpen = true;
    }

    internal void NotifyMainWindowClosed()
    {
        manualOpen = false;
        LogosWindow.IsOpen = false;
    }

    private unsafe void ItemDetailOnUpdate(AddonEvent type, AddonArgs args)
    {
        ulong id = GameGui.HoveredItem;
        if (!LogogramItems.TryGetValue(id, out var item)) return;

        var names = item.Contents.Select(x => GetFrenchItemName((uint)x)).ToArray();
        var holder = Framework.Instance()->GetUIModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder;
        var stringArrayData = holder.GetStringArrayData(27);
        var seStr = GetTooltipString(stringArrayData, 13);
        if (seStr == null) return;

        string insert = $"\n\nLogogrammes possibles : {string.Join(", ", names)}";
        if (!seStr.TextValue.Contains(insert, StringComparison.Ordinal))
            seStr.Payloads.Insert(1, new TextPayload(insert));
        stringArrayData->SetValue(13, seStr.Encode(), false, true, true);
    }

    private static unsafe SeString? GetTooltipString(StringArrayData* stringArrayData, int field)
    {
        var value = stringArrayData->StringArray[field].Value;
        return value != null ? MemoryHelper.ReadSeStringNullTerminated((nint)value) : null;
    }
}
