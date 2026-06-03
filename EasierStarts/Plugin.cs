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

    internal static ConfigEntry<int> DefibrosPerPlayer;
    internal static ConfigEntry<int> StorePrice;

    private Harmony _harmony;
    private static GameObject _behaviourGO;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo($"{PluginName} v{PluginVersion} is loading...");

        DefibrosPerPlayer = Config.Bind(
            "Defibro", "DefibrosPerPlayer", 1,
            new ConfigDescription(
                "How many free Defibros to spawn at the truck PER PLAYER at the start of every level — the total scales with lobby size (so a 4-player lobby gets 4x this). 0 disables the free grant entirely. Default 1 = one Defibro per player.",
                new AcceptableValueRange<int>(0, 10)));

        StorePrice = Config.Bind(
            "Defibro", "StorePrice", 1,
            new ConfigDescription(
                "Defibro shop price in units of $5,000 (slider 0-10, +$5,000 per step): 0 = leave the vanilla price (~$44,000) untouched, 1 = $5,000, 2 = $10,000, ... 10 = $50,000. Default 1 = $5,000.",
                new AcceptableValueRange<int>(0, 10)));

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
