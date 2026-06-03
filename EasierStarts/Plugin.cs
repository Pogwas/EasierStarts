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
    public const string PluginVersion = "0.1.0";

    internal static Plugin Instance;
    internal static ManualLogSource Log;

    internal static ConfigEntry<int> DefibrosBase;
    internal static ConfigEntry<int> DefibrosPerPlayer;
    internal static ConfigEntry<int> StorePrice;

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
