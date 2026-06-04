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
    private static LevelGenerator _deferLevel;   // level whose defer-watch window is running
    private static float _deferStartTime;        // Time.time the watch window started
    private static float _lastPollTime;          // Time.time of the last scene weapon scan
    private const float PollInterval = 0.3f;     // throttle the scene scan to a few times/sec

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

        // Defer-to-other-mods gate: watch the start of the level for another mod's starter weapon
        // (equipped OR lying at the truck) and skip our grant if one appears, so we don't stack a
        // second weapon on top of mods like StartWithGun or Let me Solo Them. We poll across a
        // window rather than checking once, because other mods can spawn a frame or two after us.
        if (Plugin.FreeItemDeferToOtherMods.Value)
        {
            // Start (or restart, on a new level) the watch window.
            if (!ReferenceEquals(_deferLevel, lg))
            {
                _deferLevel = lg;
                _deferStartTime = Time.time;
                _lastPollTime = float.NegativeInfinity; // scan immediately on the first tick
            }

            // Throttled scan: defer the moment another mod's weapon is present.
            if (Time.time - _lastPollTime >= PollInterval)
            {
                _lastPollTime = Time.time;
                if (ForeignWeaponPresent())
                {
                    Plugin.Log.LogInfo("[FreeItem] Another mod's weapon detected — deferring (no grant this level).");
                    _lastGrantedLevel = lg; // mark handled so we stop watching this level
                    return;
                }
            }

            // No weapon yet — keep watching until the window expires, then fall through to grant.
            if (Time.time - _deferStartTime < Plugin.FreeItemDeferCheckDelay.Value) return;
        }

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

    // True if another mod's starter weapon is present in the scene — whether equipped in an
    // inventory slot OR lying loose at the truck. A scene-wide scan covers both, because both
    // equipped and dropped items carry an ItemAttributes in the scene. This runs only during the
    // short watch window at level start, and only before we've granted our own weapon, so it
    // never sees our own item.
    private static bool ForeignWeaponPresent()
    {
        var all = Object.FindObjectsOfType<ItemAttributes>(true);
        foreach (var attrs in all)
        {
            if (attrs == null) continue;
            if (IsWeaponItem(attrs.item, attrs.gameObject)) return true;
        }
        return false;
    }

    private static bool IsWeaponItem(Item item, GameObject go)
    {
        // Primary: the item's category enum — covers every gun and melee (and a staff too, if the
        // game tags it 'launcher').
        if (item != null)
        {
            var t = item.itemType;
            if (t == SemiFunc.itemType.gun || t == SemiFunc.itemType.melee || t == SemiFunc.itemType.launcher)
                return true;
        }

        // Fallback for magic staffs: Torque / Void / Zero Gravity share no base class and their
        // itemType is not guaranteed to be a weapon category, so detect them by component name.
        if (go != null)
        {
            foreach (var comp in go.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp == null) continue;
                if (comp.GetType().Name.StartsWith("ItemStaff", System.StringComparison.Ordinal))
                    return true;
            }
        }
        return false;
    }
}
