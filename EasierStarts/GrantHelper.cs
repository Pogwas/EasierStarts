using Photon.Pun;
using UnityEngine;

namespace EasierStarts;

// Item lookup + spawn helpers, adapted from "Let me Solo Them"'s SoloGrantHelper.
internal static class GrantHelper
{
    // Searches StatsManager.itemDictionary for an item whose SO name / display name matches
    // `key` (normalised, then exact-or-substring). Returns null on no match or if the
    // dictionary is not yet populated.
    internal static Item FindItemByKey(string key)
    {
        if (StatsManager.instance == null || StatsManager.instance.itemDictionary == null) return null;
        if (StatsManager.instance.itemDictionary.Count == 0) return null;
        string lower = key.ToLower();
        foreach (var item in StatsManager.instance.itemDictionary.Values)
        {
            if (item == null) continue;
            string soName = item.name ?? "";
            string displayName = item.itemName ?? "";
            string normalized = soName.Replace("Item ", "").ToLower();
            if (normalized == lower
                || displayName.ToLower() == lower
                || soName.ToLower().Contains(lower)
                || displayName.ToLower().Contains(lower))
            {
                return item;
            }
        }
        return null;
    }

    // Spawns one instance of `item` at pos/rot. Networked (room object) in multiplayer,
    // plain Instantiate in singleplayer. MUST be called on the host / in singleplayer only.
    // Returns the spawned GameObject, or null on failure.
    internal static GameObject SpawnItem(Item item, Vector3 pos, Quaternion rot, string tag)
    {
        GameObject spawned;
        if (GameManager.instance.gameMode == 0)
        {
            spawned = Object.Instantiate(item.prefab.Prefab, pos, rot);
        }
        else
        {
            spawned = PhotonNetwork.InstantiateRoomObject(item.prefab.ResourcePath, pos, rot, 0);
        }
        if (spawned == null)
        {
            Plugin.Log.LogWarning($"[{tag}] Spawn returned null");
            return null;
        }
        if (!spawned.activeSelf) spawned.SetActive(true);
        return spawned;
    }
}
