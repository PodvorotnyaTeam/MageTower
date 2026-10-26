using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The enchanting table. Put the item, a stencil and a rune into its trigger
/// and interact to apply the symbol. The stencil and the rune are used up.
/// </summary>
public class EnchantingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private List<EnchantSynergyData> synergies = new();

    [Range(0f, 1f)]
    [Tooltip("Stand-in for the shadow mini-game until it is made.")]
    [SerializeField] private float debugShadowQuality = 1f;

    private readonly List<EnchantableItem> items = new();
    private readonly List<Stencil> stencils = new();
    private readonly List<RuneItem> runes = new();

    public IReadOnlyList<EnchantSynergyData> Synergies => synergies;

    private void OnTriggerEnter(Collider other)
    {
        Track(other.GetComponentInParent<EnchantableItem>(), items);
        Track(other.GetComponentInParent<Stencil>(), stencils);
        Track(other.GetComponentInParent<RuneItem>(), runes);
    }

    private void OnTriggerExit(Collider other)
    {
        items.Remove(other.GetComponentInParent<EnchantableItem>());
        stencils.Remove(other.GetComponentInParent<Stencil>());
        runes.Remove(other.GetComponentInParent<RuneItem>());
    }

    public void Interact(PlayerInteractor interactor)
    {
        items.RemoveAll(i => i == null);
        stencils.RemoveAll(s => s == null);
        runes.RemoveAll(r => r == null);

        if (items.Count == 0 || stencils.Count == 0 || runes.Count == 0)
        {
            Debug.Log("EnchantingStation: need an item, a stencil and a rune on the table.");
            return;
        }

        EnchantableItem item = items[0];
        Stencil stencil = stencils[0];
        RuneItem runeItem = runes[0];

        if (!EnchantingService.TryApply(item, runeItem.rune, stencil.symbol, GetShadowQuality()))
            return;

        stencils.Remove(stencil);
        runes.Remove(runeItem);
        Destroy(stencil.gameObject);
        Destroy(runeItem.gameObject);

        LogEffects(item);
    }

    private void LogEffects(EnchantableItem item)
    {
        var parts = new List<string>();

        foreach (var effect in EnchantingService.Evaluate(item, synergies))
            parts.Add($"{effect.Key} {effect.Value:0.#}");

        Debug.Log($"EnchantingStation: {item.name} now has [{string.Join(", ", parts)}], free slots: {item.FreeSlots}");
    }

    // Replace with the result of the shadow mini-game when it exists.
    protected virtual float GetShadowQuality()
    {
        return debugShadowQuality;
    }

    private static void Track<T>(T component, List<T> list) where T : Component
    {
        if (component != null && !list.Contains(component))
            list.Add(component);
    }
}
