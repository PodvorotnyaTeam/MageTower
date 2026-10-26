using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enchanting rules: applying a symbol to an item and working out which
/// effects the item has.
/// </summary>
public static class EnchantingService
{
    // Placeholder numbers: the design doc gives no values for enchanting.
    public const float SameSymbolStep = 1f;
    public const float MaterialSynergyBonus = 0.5f;

    public static bool TryApply(
        EnchantableItem item,
        RuneData rune,
        EnchantSymbolData symbol,
        float quality)
    {
        if (item == null || rune == null || symbol == null)
            return false;

        if (item.FreeSlots <= 0)
        {
            Debug.Log($"EnchantingService: {item.name} has no free rune slots.");
            return false;
        }

        item.enchants.Add(new AppliedEnchant
        {
            symbol = symbol,
            rune = rune,
            quality = Mathf.Clamp01(quality)
        });

        Debug.Log($"Enchant applied: {item.ItemID} <- {symbol.id} on {rune.id}");

        item.RefreshVisual();

        GameEvents.OnItemEnchanted?.Invoke(item.ItemID, symbol.id);

        return true;
    }

    /// <summary>
    /// True if the item has the effect. An empty effect id means any enchantment.
    /// </summary>
    public static bool ItemHasEffect(EnchantableItem item, string effectID)
    {
        if (item == null || item.enchants.Count == 0)
            return false;

        if (string.IsNullOrEmpty(effectID))
            return true;

        var station = Object.FindFirstObjectByType<EnchantingStation>();
        var effects = Evaluate(item, station != null ? station.Synergies : null);

        return effects.TryGetValue(effectID, out float power) && power > 0f;
    }

    /// <summary>
    /// Effect id -> strength. The same symbol repeated on the item adds up,
    /// a rune made of a matching material adds a bonus, and a pair of
    /// different symbols from the synergy list adds its own effect.
    /// </summary>
    public static Dictionary<string, float> Evaluate(
        EnchantableItem item,
        IEnumerable<EnchantSynergyData> synergies)
    {
        var symbolPower = new Dictionary<EnchantSymbolData, float>();

        foreach (var enchant in item.enchants)
        {
            if (enchant.symbol == null)
                continue;

            float power = SameSymbolStep * enchant.quality;

            if (enchant.rune != null && enchant.rune.synergySymbol == enchant.symbol)
                power += MaterialSynergyBonus;

            symbolPower.TryGetValue(enchant.symbol, out float current);
            symbolPower[enchant.symbol] = current + power;
        }

        var effects = new Dictionary<string, float>();

        foreach (var pair in symbolPower)
            AddEffect(effects, pair.Key.effectID, pair.Value);

        if (synergies != null)
        {
            foreach (var synergy in synergies)
            {
                if (synergy == null ||
                    synergy.symbolA == null ||
                    synergy.symbolB == null ||
                    !symbolPower.TryGetValue(synergy.symbolA, out float powerA) ||
                    !symbolPower.TryGetValue(synergy.symbolB, out float powerB))
                    continue;

                AddEffect(effects, synergy.effectID, Mathf.Min(powerA, powerB));
            }
        }

        return effects;
    }

    private static void AddEffect(Dictionary<string, float> effects, string effectID, float power)
    {
        if (string.IsNullOrWhiteSpace(effectID))
            return;

        effects.TryGetValue(effectID, out float current);
        effects[effectID] = current + power;
    }
}
