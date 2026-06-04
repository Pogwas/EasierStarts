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

    internal static ConfigEntry<bool> FreeItemEnabled;
    internal static ConfigEntry<string> FreeItem;
    internal static ConfigEntry<bool> FreeItemPerPlayer;
    internal static ConfigEntry<bool> FreeItemFirstLevelOnly;
    internal static ConfigEntry<bool> FreeItemDeferToOtherMods;
    internal static ConfigEntry<float> FreeItemDeferCheckDelay;

    // Curated weapons + magic staffs roster for the [Free Item] dropdown (asset names from resources.assets).
    private static readonly string[] WeaponItems = new[]
    {
        "Item Gun Tranq", "Item Gun Stun", "Item Gun Handgun", "Item Gun Shotgun",
        "Item Gun Laser", "Item Gun Shockwave",
        "Item Melee Sword", "Item Melee Sledge Hammer", "Item Melee Frying Pan",
        "Item Melee Baseball Bat", "Item Melee Inflatable Hammer", "Item Melee Stun Baton",
        "Item Staff Torque", "Item Staff Void", "Item Staff Zero Gravity",
    };

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

        FreeItemEnabled = Config.Bind(
            "Free Item", "Enabled", true,
            "Master toggle for the free starter weapon granted at the truck.");

        FreeItem = Config.Bind(
            "Free Item", "Item", "Item Gun Tranq",
            new ConfigDescription(
                "Which weapon or magic staff to grant for free at the truck at run start. Defaults to the Tranq Gun.",
                new AcceptableValueList<string>(WeaponItems)));

        FreeItemPerPlayer = Config.Bind(
            "Free Item", "PerPlayer", true,
            "When true (default), one is granted per player (scales with lobby size). When false, exactly one is granted regardless of lobby size.");

        FreeItemFirstLevelOnly = Config.Bind(
            "Free Item", "FirstLevelOnly", true,
            "When true (default), the weapon is granted only on the first level of a run (a run-start leg-up). When false, it is re-granted at the truck every level (items don't carry across levels).");

        FreeItemDeferToOtherMods = Config.Bind(
            "Free Item", "DeferToOtherMods", true,
            "When true (default), Easier Starts watches the start of the level for another mod's starter weapon (one you have equipped OR one lying at the truck) and skips its own free-weapon grant if it finds one — so it won't stack a second weapon on top of mods like StartWithGun or Let me Solo Them. Set false to always grant regardless.");

        FreeItemDeferCheckDelay = Config.Bind(
            "Free Item", "DeferCheckDelay", 4.0f,
            new ConfigDescription(
                "Maximum seconds to watch for another mod's weapon before granting yours. Easier Starts checks several times across this window and defers the moment it spots a weapon; if none appears by the end, it grants. Raise this if a slow mod spawns its weapon later. Only used when DeferToOtherMods is on. Default 4.",
                new AcceptableValueRange<float>(0f, 10f)));

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
