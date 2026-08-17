using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance;

    [SerializeField]
    private List<FactionReputation> reputations = new();

    private void Awake()
    {
        Instance = this;
    }

    public int GetReputation(FactionData faction)
    {
        var rep = reputations.Find(r => r.faction == faction);
        if (rep == null)
        {
            return 0;
        }
        return rep.reputation;
    }

    public void AddReputation(FactionData faction, int amount)
    {
        var rep = reputations.Find(r => r.faction == faction);

        if (rep == null)
        {
            rep = new FactionReputation
            {
                faction = faction,
                reputation = 0
            };
            reputations.Add(rep);
        }

        rep.reputation += amount;
        Debug.Log($"{faction.factionName} reputation: {rep.reputation}");
    }

    public ReputationRank GetRank(int reputation)
    {
        if (reputation < -50)
            return ReputationRank.Hostile;
        if (reputation < 0)
            return ReputationRank.Unfriendly;
        if (reputation < 50)
            return ReputationRank.Neutral;
        if (reputation < 150)
            return ReputationRank.Friendly;
        if (reputation < 300)
            return ReputationRank.Honored;

        return ReputationRank.Revered;
    }
    public ReputationRank GetRank(FactionData faction)
    {
        int reputation = GetReputation(faction);

        return GetRank(reputation);
    }
}
