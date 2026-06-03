using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EasierStarts;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.pogwas.easierstarts";
    public const string PluginName = "Easier Starts";
    public const string PluginVersion = "0.2.0";

    internal static Plugin Instance;
    internal static ManualLogSource Log;

    internal static ConfigEntry<int> DefibrosBase;
    internal static ConfigEntry<int> DefibrosPerPlayer;
    internal static ConfigEntry<int> StorePrice;

    internal static ConfigEntry<bool> FreeItemsEnabled;
    internal static ConfigEntry<string> FreeItems;
    internal static ConfigEntry<bool> FreeItemsFirstLevelOnly;

    private Harmony _harmony;
    private static GameObject _behaviourGO;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo($"{PluginName} v{PluginVersion} is loading...");

        DefibrosBase = Config.Bind(
            "Defibro", "DefibrosBase", 1,
            new ConfigDescription(
                "Flat number of free Defibros spawned at the truck each level, regardless of lobby size. Total granted = DefibrosBase + DefibrosPerPlayer x player count. Default 1.",
                new AcceptableValueRange<int>(0, 10)));

        DefibrosPerPlayer = Config.Bind(
            "Defibro", "DefibrosPerPlayer", 1,
            new ConfigDescription(
                "Extra free Defibros per player, added on top of DefibrosBase (total = DefibrosBase + this x player count). With both at the default 1: solo = 2, a 4-player lobby = 5. 0 = no per-player scaling. Default 1.",
                new AcceptableValueRange<int>(0, 10)));

        StorePrice = Config.Bind(
            "Defibro", "StorePrice (x1000)", 5,
            new ConfigDescription(
                "Defibro shop price in thousands of dollars — the shown number x1000 is the price (e.g. 5 = $5,000, 50 = $50,000). 0 leaves the vanilla price (~$44,000) untouched. Range 0-50, +$1,000 per step. Default 5 = $5,000.",
                new AcceptableValueRange<int>(0, 50)));

        FreeItemsEnabled = Config.Bind(
            "Free Items", "Enabled", true,
            "Master toggle for the free-item grant. When true, the items in the Items list are spawned at the truck (level 1 only by default — see FirstLevelOnly).");

        FreeItems = Config.Bind(
            "Free Items", "Items", "Item Gun Tranq:1/player",
            "Comma-separated list of items to free-grant at the truck. Each entry is 'name:count' (a flat count) or 'name:count/player' (count multiplied by the number of players). 'name' matches an item's asset name or display name (e.g. 'Item Gun Tranq' / 'Tranq Gun'; a partial name like 'tranq' also works). Default grants one Tranq Gun per player. Leave empty to grant nothing. Example: \"Item Gun Tranq:1/player, Item Health Pack Small:2\".");

        FreeItemsFirstLevelOnly = Config.Bind(
            "Free Items", "FirstLevelOnly", true,
            "When true (default), the items are granted only on the first level of a run (a run-start leg-up). When false, they are re-granted at the truck every level (items don't carry across levels, so this keeps a starter weapon always available).");

        _harmony = new Harmony(PluginGuid);
        _harmony.PatchAll();

        SceneManager.sceneLoaded += OnSceneLoaded;

        Log.LogInfo($"{PluginName} loaded successfully.");
    }

    // REPO destroys DontDestroyOnLoad objects at boot, so the behaviour is (re)created
    // on every scene load if it has gone missing.
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_behaviourGO == null)
        {
            _behaviourGO = new GameObject("EasierStarts.Behaviour", typeof(EasierStartsBehaviour));
            DontDestroyOnLoad(_behaviourGO);
            Log.LogDebug($"[EasierStarts] behaviour (re)created after scene '{scene.name}'");
        }
    }
}
