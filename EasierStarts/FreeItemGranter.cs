using UnityEngine;

namespace EasierStarts;

// Spawns one free weapon at the truck at run start (the [Free Item] feature). Driven each frame
// from EasierStartsBehaviour.Update. Host / singleplayer only. Sibling of DefibroGranter.
internal static class FreeItemGranter
{
    private static Item _cached;
    private static string _cachedFrom;          // the Item config string _cached reflects
    private static bool _warned;                // gave up on the current config (bad name)
    private static LevelGenerator _lastGrantedLevel;

    public static void Tick()
    {
        if (!Plugin.FreeItemEnabled.Value) return;
        if (!SemiFunc.RunIsLevel()) return;
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

        var lg = LevelGenerator.Instance;
        if (lg == null || !lg.Generated) return;

        // One grant per gameplay level.
        if (ReferenceEquals(lg, _lastGrantedLevel)) return;

        // Level-1-only gate.
        if (Plugin.FreeItemFirstLevelOnly.Value)
        {
            if (RunManager.instance == null) return;
            if (RunManager.instance.levelsCompleted != 0) return;
        }

        var truck = TruckSafetySpawnPoint.instance;
        if (truck == null) return; // truck not in scene yet — retry next tick

        // Resolve the configured weapon (re-resolve if the config changed).
        string want = Plugin.FreeItem.Value ?? "";
        if (_cachedFrom != want) { _cached = null; _warned = false; _cachedFrom = want; }

        if (_cached == null && !_warned)
        {
            // Need the item dictionary populated to resolve. If not ready, return WITHOUT
            // marking the level so we retry on a later tick.
            if (StatsManager.instance == null || StatsManager.instance.itemDictionary == null
                || StatsManager.instance.itemDictionary.Count == 0)
            {
                return;
            }
            _cached = GrantHelper.FindItemByKey(want);
            if (_cached == null)
            {
                Plugin.Log.LogWarning($"[FreeItem] No item matches '{want}'; granting nothing");
                _warned = true;
            }
            else
            {
                Plugin.Log.LogInfo($"[FreeItem] Matched '{want}' -> name='{_cached.name}', itemName='{_cached.itemName}'");
            }
        }

        // Mark BEFORE spawning so a spawn exception cannot cause an infinite re-grant loop.
        _lastGrantedLevel = lg;

        if (_cached == null) return; // unresolved (already warned)

        int players = SemiFunc.PlayerGetList()?.Count ?? 1;
        if (players < 1) players = 1;
        int count = Plugin.FreeItemPerPlayer.Value ? players : 1;

        Vector3 basePos = truck.transform.position + Vector3.up * 0.5f;
        Quaternion rot = truck.transform.rotation;
        int spawned = 0;
        for (int i = 0; i < count; i++)
        {
            try
            {
                // Small lateral spread so multiple copies don't spawn inside one another.
                Vector3 pos = basePos + truck.transform.right * (i * 0.4f);
                var go = GrantHelper.SpawnItem(_cached, pos, rot, "FreeItem");
                if (go != null) spawned++;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[FreeItem] Spawn {i + 1}/{count} threw: {ex.GetType().Name}: {ex.Message}");
            }
        }
        Plugin.Log.LogInfo($"[FreeItem] Granted {spawned}/{count} '{_cached.itemName}' at the truck ({players} player(s)).");
    }
}
