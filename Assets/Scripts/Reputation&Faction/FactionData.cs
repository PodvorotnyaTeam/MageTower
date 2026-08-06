using UnityEngine;

[CreateAssetMenu(menuName = "Factions/Faction")]
public class FactionData : ScriptableObject
{
    public string id;
    public string factionName;

    [TextArea]
    public string description;
}
