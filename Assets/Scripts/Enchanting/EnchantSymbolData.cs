using UnityEngine;

/// <summary>
/// A symbol that is cut into a rune. One stencil carries exactly one symbol.
/// </summary>
[CreateAssetMenu(fileName = "EnchantSymbol", menuName = "Enchanting/Symbol")]
public class EnchantSymbolData : ScriptableObject
{
    public string id;
    public string displayName;

    [Tooltip("Effect the symbol gives to the item on its own (e.g. fire).")]
    public string effectID;
}
