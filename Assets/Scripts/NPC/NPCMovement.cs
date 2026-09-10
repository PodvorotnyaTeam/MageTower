using UnityEngine;
using UnityEngine.AI;

public class NPCMovement : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;

    public bool HasReachedDestination()
    {
        if (agent == null)
            return false;

        if (agent.pathPending)
            return false;

        if (!agent.hasPath)
            return false;

        return agent.remainingDistance <= agent.stoppingDistance + 0.05f;
    }

    public void MoveTo(Transform target)
    {
        if (target == null)
        {
            Debug.LogError("NPCMovement: target is NULL.");
            return;
        }

        if (agent == null)
        {
            Debug.LogError("NPCMovement: NavMeshAgent is NULL.");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("NPCMovement: NPC is NOT on NavMesh.");
            return;
        }

        //Debug.Log($"NPC moving to {target.name}");

        agent.SetDestination(target.position);
    }
}
