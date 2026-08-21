using UnityEngine;

public class NPCController : MonoBehaviour
{

    [SerializeField] private NPCMovement movement;

    private Transform counterPoint;
    private Transform exitPoint;

    public NPCState CurrentState { get; private set; } = NPCState.None;

    public void Initialize(Transform counterPoint, Transform exitPoint)
    {
        this.counterPoint = counterPoint;
        this.exitPoint = exitPoint;
    }

    public void StartVisit()
    {

        if (counterPoint == null)
        {
            Debug.LogError("NPCController: CounterPoint is NULL.");
            return;
        }

        if (movement == null)
        {
            Debug.LogError("NPCController: Movement is NULL.");
            return;
        }

        CurrentState = NPCState.GoingToCounter;

        movement.MoveTo(counterPoint);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case NPCState.GoingToCounter:

                if (movement.HasReachedDestination())
                {
                    CurrentState = NPCState.WaitingAtCounter;
                }

                break;

            case NPCState.Leaving:

                if (movement.HasReachedDestination())
                {
                    Destroy(gameObject);
                }

                break;
        }
    }

    public void Leave()
    {
        if (exitPoint == null)
        {
            Debug.LogError("NPCController: ExitPoint is NULL.");
            return;
        }

        CurrentState = NPCState.Leaving;

        movement.MoveTo(exitPoint);
    }

    public bool CanInteract()
    {
        return CurrentState == NPCState.WaitingAtCounter;
    }
}