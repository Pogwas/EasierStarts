using UnityEngine;

namespace EasierStarts;

// Spawns DefibrosPerLevel free Defibros at the truck at the start of every gameplay level.
// Driven each frame from EasierStartsBehaviour.Update. Host / singleplayer only.
// Vanilla destroys carried items across level transitions, so a fresh grant every level
// is the intended behaviour.
internal static class DefibroGranter
{
    // "revive" substring-matches the Defibro SO name "Item ReviveItem".
    private const string DefibroKey = "revive";

    private static Item _cachedDefibro;
    private static bool _permanentGiveup;
    private static LevelGenerator _lastGrantedLevel;

    public static void Tick()
    {
        int flatBase = Plugin.DefibrosBase.Value;
        int perPlayer = Plugin.DefibrosPerPlayer.Value;
        if (flatBase <= 0 && perPlayer <= 0) return;
        if (_permanentGiveup) return;
        if (!SemiFunc.RunIsLevel()) return;
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

        var lg = LevelGenerator.Instance;
        if (lg == null || !lg.Generated) return;

        // One grant per gameplay level — a fresh LevelGenerator.Instance means a new level.
        if (ReferenceEquals(lg, _lastGrantedLevel)) return;

        var truck = TruckSafetySpawnPoint.instance;
        if (truck == null) return; // truck not in the scene yet — retry next tick

        if (_cachedDefibro == null)
        {
            _cachedDefibro = GrantHelper.FindItemByKey(DefibroKey);
            if (_cachedDefibro == null)
            {
                // Only give up permanently once the dictionary is actually populated.
                if (StatsManager.instance != null && StatsManager.instance.itemDictionary != null
                    && StatsManager.instance.itemDictionary.Count > 0)
                {
                    Plugin.Log.LogWarning("[Defibro] No Defibro (Item ReviveItem) found in itemDictionary; disabling the free grant");
                    _permanentGiveup = true;
                }
                return;
            }
            Plugin.Log.LogInfo($"[Defibro] Matched item — name='{_cachedDefibro.name}', itemName='{_cachedDefibro.itemName}'");
        }

        // Mark BEFORE spawning so a spawn exception cannot cause an infinite re-grant loop.
        _lastGrantedLevel = lg;

        // Scale the grant by lobby size: DefibrosPerPlayer for each player in the run.
        int players = SemiFunc.PlayerGetList()?.Count ?? 1;
        if (players < 1) players = 1;
        int count = flatBase + perPlayer * players;
        if (count <= 0) return;

        Vector3 basePos = truck.transform.position + Vector3.up * 0.5f;
        Quaternion rot = truck.transform.rotation;
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            try
            {
                // Small lateral spread so multiple Defibros don't spawn inside one another.
                Vector3 pos = basePos + truck.transform.right * (i * 0.4f);
                var go = GrantHelper.SpawnItem(_cachedDefibro, pos, rot, "Defibro");
                if (go != null) spawned++;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[Defibro] Spawn {i + 1}/{count} threw: {ex.GetType().Name}: {ex.Message}");
            }
        }
        Plugin.Log.LogInfo($"[Defibro] Granted {spawned}/{count} free Defibro(s) at the truck (base {flatBase} + {perPlayer}/player x {players} player(s)).");
    }
}
