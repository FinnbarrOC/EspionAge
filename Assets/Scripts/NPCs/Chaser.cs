﻿using UnityEngine;
using UnityEngine.AI;

public abstract class Chaser : MonoBehaviour
{
    [Header("Chaser")]
    public bool chase = true;
    public Transform targetTransform;
    
    [HideInInspector] public NavMeshAgent agent;

    // Start is called before the first frame update
    public void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Disabling auto-braking allows for continuous movement
        // between points (ie, the agent doesn't slow down as it
        // approaches a destination point).
        agent.autoBraking = false;
    }

    public abstract void HandleTargetsInRange(int numTargetsInRange);

    protected void ChaseTarget()
    {
        Vector3 targetPosition = targetTransform.position;
        Vector3 thisPosition = transform.position;
        Vector3 dirToTarget = thisPosition - targetPosition;
        Vector3 newPos = thisPosition - dirToTarget;

        agent.SetDestination(newPos);
    }
}
