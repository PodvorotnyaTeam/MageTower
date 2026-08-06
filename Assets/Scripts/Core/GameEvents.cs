using System;
public static class GameEvents
{
    public static Action<string> OnNPCInteracted;
    public static Action<string> OnQuestTurnedIn;
    public static Action<string, int> OnItemCollected;
}