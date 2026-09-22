using System;
public static class GameEvents
{
    public static Action<string> OnNPCInteracted;
    public static Action<string> OnQuestTurnedIn;
    public static Action<string> OnQuestFailed;
    public static Action<string, int> OnItemCollected;
    public static Action<string, int> OnPotionCrafted;
}
