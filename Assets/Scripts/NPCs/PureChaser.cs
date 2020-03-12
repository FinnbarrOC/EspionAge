﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PureChaser : MonoBehaviour
{
    public Transform targetTransform;
    public float startChaseRadius = 100f;

    private ChildRootMotionController rootMotionController;
    private NavMeshAgent agent;
    private bool shouldChase = false;

    public const float reportReachedDistance = 2f;

    public event Chaser.CollideWithPlayerAction OnCollideWithPlayer;
    public delegate void ReachedDestinationAction();
    public event ReachedDestinationAction OnReachDestination;

    private void Awake()
    {
        agent = Utils.GetRequiredComponent<NavMeshAgent>(this);
        rootMotionController = Utils.GetRequiredComponentInChildren<ChildRootMotionController>(this);
    }

    public void SetSpeed(float speed)
    {
        agent.speed = speed;
    }

    public void SetAnimationSpeed(float speed)
    {
        rootMotionController.SetAnimationSpeed(speed);
    }

    public void SetDestination(Vector3 position)
    {
        agent.SetDestination(position);
    }

    private void ChaseTarget()
    {
        if (shouldChase && targetTransform)
        {
            SetDestination(targetTransform.position);
        }
    }

    private void Update()
    {
        if (!agent.isOnNavMesh)
        {
            SetMoving(false);
            return;
        }

        CheckRemainingDistance();

        // below are all about using targetTransform
        if (!targetTransform || agent.speed <= 0f)
        {
            SetMoving(false);
            return;
        }
        else if (shouldChase)
        {
            SetMoving(true);
        }

        if (!shouldChase && Vector3.Distance(transform.position, targetTransform.position) <= startChaseRadius)
        {
            shouldChase = true;
        }

        ChaseTarget();
    }

    private void SetMoving(bool isMoving)
    {
        rootMotionController.SetBool(Constants.ANIMATION_STEVE_MOVING, isMoving);
    }

    private void CheckRemainingDistance()
    {
        if (Vector3.Distance(transform.position, agent.destination) <= reportReachedDistance)
        {
            OnReachDestination?.Invoke();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(Constants.TAG_PLAYER))
        {
            OnCollideWithPlayer?.Invoke();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, startChaseRadius);
    }
}
