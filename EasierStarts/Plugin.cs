using BepInEx;
using BepInEx.Logging;

namespace EasierStarts;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public class Plugin : BaseUnityPlugin
{
    public const string PluginGuid = "com.pogwas.easierstarts";
    public const string PluginName = "Easier Starts";
    public const string PluginVersion = "0.1.0";

    internal static Plugin Instance;
    internal static ManualLogSource Log;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        Log.LogInfo($"{PluginName} v{PluginVersion} is loading...");
        Log.LogInfo($"{PluginName} loaded successfully.");
    }
}
