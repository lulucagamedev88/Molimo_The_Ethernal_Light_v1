
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        #region Settings

        [Header("Detection")]
        [SerializeField] private Transform target;
        [SerializeField] private string targetTag = "Player";
        [SerializeField] private float detectionRadius = 6f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stoppingDistance = 0.8f;

        [Header("Position Safety")]
        [SerializeField] private bool lockZPosition = true;

        #endregion

        #region Components

        private Rigidbody2D rb;

        #endregion

        #region Runtime

        private Vector2 movementDirection;
        private float lockedZPosition;
        private bool isChasing;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();

            lockedZPosition = transform.position.z;

            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(targetTag);

                if (player != null)
                {
                    target = player.transform;
                }
            }
        }

        private void Update()
        {
            UpdateTargetDetection();
        }

        private void FixedUpdate()
        {
            UpdateMovement();
        }

        private void LateUpdate()
        {
            if (lockZPosition)
            {
                Vector3 position = transform.position;
                position.z = lockedZPosition;
                transform.position = position;
            }
        }

        #endregion

        #region Detection

        private void UpdateTargetDetection()
        {
            if (target == null)
            {
                isChasing = false;
                movementDirection = Vector2.zero;
                return;
            }

            Vector2 direction = (Vector2)target.position - rb.position;

            float distance = direction.magnitude;

            isChasing = distance <= detectionRadius &&
                        distance > stoppingDistance;

            if (isChasing)
            {
                movementDirection = direction.normalized;
            }
            else
            {
                movementDirection = Vector2.zero;
            }
        }

        #endregion

        #region Movement

        private void UpdateMovement()
        {
            if (!isChasing || movementDirection == Vector2.zero)
            {
                return;
            }

            Vector2 nextPosition = rb.position +
                                   movementDirection *
                                   moveSpeed *
                                   Time.fixedDeltaTime;

            rb.MovePosition(nextPosition);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stoppingDistance);
        }

        #endregion
    }
}
