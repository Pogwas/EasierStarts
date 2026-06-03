using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasierStarts;

// Spawns a configurable list of free items at the truck. Driven each frame from
// EasierStartsBehaviour.Update. Host / singleplayer only. Generic sibling of DefibroGranter
// — the Defibro feature stays separate; this handles everything else via the [Free Items] list.
internal static class FreeItemGranter
{
    // One parsed list entry.
    private sealed class Entry
    {
        public string Key;
        public int Count;
        public bool PerPlayer;
        public Item Resolved;   // lazily resolved + cached
        public bool Warned;     // warned-once when not found after the dictionary is populated
    }

    private static LevelGenerator _lastGrantedLevel;
    private static string _parsedFrom;                  // the Items string the cache was built from
    private static List<Entry> _entries = new List<Entry>();

    public static void Tick()
    {
        if (!Plugin.FreeItemsEnabled.Value) return;
        if (!SemiFunc.RunIsLevel()) return;
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;

        var lg = LevelGenerator.Instance;
        if (lg == null || !lg.Generated) return;

        // One grant per gameplay level — a fresh LevelGenerator.Instance means a new level.
        if (ReferenceEquals(lg, _lastGrantedLevel)) return;

        // Level-1-only gate.
        if (Plugin.FreeItemsFirstLevelOnly.Value)
        {
            if (RunManager.instance == null) return;
            if (RunManager.instance.levelsCompleted != 0) return;
        }

        var truck = TruckSafetySpawnPoint.instance;
        if (truck == null) return; // truck not in scene yet — retry next tick

        EnsureParsed();
        if (_entries.Count == 0) return;

        // We can only resolve item names once the item dictionary is populated. If it isn't
        // ready yet, return WITHOUT marking the level granted so we retry on a later tick.
        bool dictReady = StatsManager.instance != null
            && StatsManager.instance.itemDictionary != null
            && StatsManager.instance.itemDictionary.Count > 0;
        if (!dictReady) return;

        // Resolve any unresolved entries now that the dictionary is ready.
        foreach (var e in _entries)
        {
            if (e.Resolved != null) continue;
            e.Resolved = GrantHelper.FindItemByKey(e.Key);
            if (e.Resolved == null)
            {
                if (!e.Warned)
                {
                    Plugin.Log.LogWarning($"[FreeItems] No item matches '{e.Key}'; skipping that entry");
                    e.Warned = true;
                }
            }
            else
            {
                Plugin.Log.LogInfo($"[FreeItems] Matched '{e.Key}' -> name='{e.Resolved.name}', itemName='{e.Resolved.itemName}'");
            }
        }

        // Mark BEFORE spawning so a spawn exception cannot cause an infinite re-grant loop.
        _lastGrantedLevel = lg;

        int players = SemiFunc.PlayerGetList()?.Count ?? 1;
        if (players < 1) players = 1;

        Vector3 basePos = truck.transform.position + Vector3.up * 0.5f;
        Quaternion rot = truck.transform.rotation;
        int spreadIndex = 0;
        int totalSpawned = 0;
        int totalRequested = 0;

        foreach (var e in _entries)
        {
            if (e.Resolved == null) continue; // unresolved/bad name — already warned

            int count = e.PerPlayer ? e.Count * players : e.Count;
            if (count <= 0) continue;
            totalRequested += count;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    // Small lateral spread so items don't spawn inside one another.
                    Vector3 pos = basePos + truck.transform.right * (spreadIndex * 0.4f);
                    spreadIndex++;
                    var go = GrantHelper.SpawnItem(e.Resolved, pos, rot, "FreeItems");
                    if (go != null) totalSpawned++;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[FreeItems] Spawn of '{e.Key}' ({i + 1}/{count}) threw: {ex.GetType().Name}: {ex.Message}");
                }
            }
        }

        Plugin.Log.LogInfo($"[FreeItems] Granted {totalSpawned}/{totalRequested} item(s) at the truck across {_entries.Count} entr(ies) ({players} player(s)).");
    }

    // Rebuild the parsed entry list when the Items config string changes.
    private static void EnsureParsed()
    {
        string raw = Plugin.FreeItems.Value ?? "";
        if (raw == _parsedFrom) return;
        _parsedFrom = raw;
        _entries = Parse(raw);
    }

    // Parses "name:count, name:count/player, ..." into entries. Invalid tokens are skipped
    // with a warning; valid ones still parse.
    private static List<Entry> Parse(string raw)
    {
        var list = new List<Entry>();
        if (string.IsNullOrWhiteSpace(raw)) return list;

        foreach (var rawToken in raw.Split(','))
        {
            string token = rawToken.Trim();
            if (token.Length == 0) continue;

            int colon = token.LastIndexOf(':');
            if (colon <= 0 || colon == token.Length - 1)
            {
                Plugin.Log.LogWarning($"[FreeItems] Bad entry '{token}' (expected name:count or name:count/player); skipping");
                continue;
            }

            string key = token.Substring(0, colon).Trim();
            string countPart = token.Substring(colon + 1).Trim();
            bool perPlayer = false;

            // Optional "/player" suffix. Split on the slash (rather than EndsWith) so whitespace
            // around it is tolerated — "1/player", "1 / player", "1/ player", "1 /player" all work,
            // consistent with the whitespace tolerance everywhere else. An unrecognized suffix
            // (e.g. "1/player/player") leaves countPart with the slash so TryParse rejects it.
            int slash = countPart.IndexOf('/');
            if (slash >= 0)
            {
                string suffix = countPart.Substring(slash + 1).Trim();
                if (suffix.Equals("player", StringComparison.OrdinalIgnoreCase))
                {
                    perPlayer = true;
                    countPart = countPart.Substring(0, slash).Trim();
                }
            }

            if (key.Length == 0 || !int.TryParse(countPart, out int count) || count < 0)
            {
                Plugin.Log.LogWarning($"[FreeItems] Bad entry '{token}' (count must be a non-negative integer); skipping");
                continue;
            }
            if (count == 0) continue;

            list.Add(new Entry { Key = key, Count = count, PerPlayer = perPlayer });
        }
        return list;
    }
}
