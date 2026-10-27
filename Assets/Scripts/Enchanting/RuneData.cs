using UnityEngine;

/// <summary>
/// A rune blank made of a metal or a mineral.
/// </summary>
[CreateAssetMenu(fileName = "Rune", menuName = "Enchanting/Rune")]
public class RuneData : ScriptableObject
{
    public string id;
    public string displayName;

    [Tooltip("Metal or mineral the rune is made of.")]
    public string material;

    [Tooltip("Symbol that works better on this rune (e.g. lightning on an electrolyte rune). Optional.")]
    public EnchantSymbolData synergySymbol;
}
