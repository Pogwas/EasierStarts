using HarmonyLib;

namespace EasierStarts;

// Overrides the Defibro's shop price. ItemAttributes computes a per-instance price in
// GetValue() (host side) and clients receive it via GetValueRPC. Both are postfixed so the
// override is consistent across host and clients. Only Defibros in the shop are affected
// (ShopManager.instance is non-null only while in the shop).
internal static class DefibroPrice
{
    private const string DefibroSoName = "Item ReviveItem";

    private static readonly AccessTools.FieldRef<ItemAttributes, int> ValueRef =
        AccessTools.FieldRefAccess<ItemAttributes, int>("value");
    private static readonly AccessTools.FieldRef<ItemAttributes, Item> ItemRef =
        AccessTools.FieldRefAccess<ItemAttributes, Item>("item");

    // Applies the configured price override to a Defibro's ItemAttributes.value, if applicable.
    internal static void Apply(ItemAttributes attr)
    {
        int kDollars = Plugin.StorePrice.Value;     // 0-50, in thousands of dollars (shown x1000 = price)
        if (kDollars <= 0) return;                   // 0 = leave the vanilla price untouched
        if (attr == null) return;
        if (ShopManager.instance == null) return;    // only re-price Defibros in the shop
        var item = ItemRef(attr);
        if (item == null || item.name != DefibroSoName) return;
        // ItemAttributes.value is already in THOUSANDS (the shop UI shows "$" + value + "K"), and
        // StorePrice is in thousands too, so they map 1:1 (5 -> value 5 -> "$5K" = $5,000).
        ValueRef(attr) = kDollars;
    }
}

// Host side: GetValue computes the value (and RPCs the raw value to clients).
[HarmonyPatch(typeof(ItemAttributes), "GetValue")]
internal static class ItemAttributesGetValuePatch
{
    [HarmonyPostfix]
    public static void Postfix(ItemAttributes __instance)
    {
        DefibroPrice.Apply(__instance);
    }
}

// Client side: GetValueRPC receives the value sent by the host.
[HarmonyPatch(typeof(ItemAttributes), "GetValueRPC")]
internal static class ItemAttributesGetValueRpcPatch
{
    [HarmonyPostfix]
    public static void Postfix(ItemAttributes __instance)
    {
        DefibroPrice.Apply(__instance);
    }
}
