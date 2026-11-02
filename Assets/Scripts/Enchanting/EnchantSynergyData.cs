using UnityEngine;

/// <summary>
/// Two different symbols on the same item give an extra effect
/// (fire + ice = steam).
/// </summary>
[CreateAssetMenu(fileName = "EnchantSynergy", menuName = "Enchanting/Synergy")]
public class EnchantSynergyData : ScriptableObject
{
    public EnchantSymbolData symbolA;
    public EnchantSymbolData symbolB;

    [Tooltip("Effect that appears when both symbols are on the item.")]
    public string effectID;
}
