﻿using System.Collections;
using NPCs.Components;
using UnityEngine;

namespace NPCs
{
    public enum PureChaserStates
    {
        // Can add to & actually use this if we refactor PureChaser
    }
    
    [RequireComponent(typeof(Enemy))]
    public class PureChaser : BaseStateAi<PureChaserStates>
    {
        public float startChaseRadius = 100f;
        public float rotateSpeed = 10f;
        [HideInInspector] public Transform targetTransform;
        
        [HideInInspector] public Enemy enemy;
        private ChildRootMotionController rootMotionController;
        private bool shouldChase = false;

        private const float REPORT_REACHED_DISTANCE = 2f;
        

        public delegate void ReachedDestinationAction();
        public event ReachedDestinationAction OnReachDestination;

        protected override void Awake()
        {
            base.Awake();
            rootMotionController = Utils.GetRequiredComponentInChildren<ChildRootMotionController>(this);
            enemy = Utils.GetRequiredComponent<Enemy>(this);
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

        protected override void SetState(PureChaserStates newState)
        {
            // Ignore unless you want to refactor PureChaser later on
            throw new System.NotImplementedException();
        }

        protected void Update()
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
            rootMotionController.SetBool(Constants.ANIMATION_TUTORIALCHASER_MOVING, isMoving);
        }

        public void StartCleaning()
        {
            shouldChase = false;
            SetMoving(false);
            rootMotionController.SetTrigger(Constants.ANIMATION_TUTORIALCHASER_CLEAN);
            rootMotionController.SetFloat(Constants.ANIMATION_TUTORIALCHASER_CYCLEOFFSET, Random.Range(0f, 1f));

            StartCoroutine(RotateSmoothly(Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)));
        }

        private IEnumerator RotateSmoothly(Quaternion goalRotation)
        {
            while (!transform.rotation.Equals(goalRotation))
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, goalRotation, Time.deltaTime * rotateSpeed);
                yield return new WaitForEndOfFrame();
            }
        }

        private void CheckRemainingDistance()
        {
            if (Vector3.Distance(transform.position, agent.destination) <= REPORT_REACHED_DISTANCE)
            {
                OnReachDestination?.Invoke();
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, startChaseRadius);
        }
    }
}
