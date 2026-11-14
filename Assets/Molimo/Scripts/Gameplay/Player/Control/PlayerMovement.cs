using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public enum MovementMode { Idle, Walking, Running, Sprinting }

    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Velocidades de Movimento")]
        [SerializeField, Min(0f)] private float walkSpeed = 3f;
        [SerializeField, Min(0f)] private float runSpeed = 6f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 2f;

        [Header("Aceleração e Desaceleração")]
        [SerializeField, Min(0.01f)] private float accelerationRate = 40f;
        [SerializeField, Min(0.01f)] private float decelerationRate = 30f;
        [SerializeField, Min(0.01f)] private float skidDecelerationRate = 10f;

        [Header("Sincronização do Animator")]
        [SerializeField] private bool syncAnimatorSpeed = true;

        [Tooltip("Velocidade da animação quando o player está em Run (Default: 1.5).")]
        [SerializeField] private float runAnimationSpeed = 1.5f;

        [Tooltip("Velocidade da animação quando o player está em Sprint (Default: 1.2).")]
        [SerializeField] private float sprintAnimationSpeed = 1.2f;

        [Header("Skid (Derrapagem)")]
        [SerializeField, Min(0f)] private float skidMinSpeed = 6f;

        [Header("Dash")]
        [SerializeField, Min(0.01f)] private float dashSpeed = 18f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.2f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.6f;

        [Header("Dash Recovery (Input Lock)")]
        [SerializeField, Min(0f)] private float dashRecoveryDuration = 0.15f;

        [Header("Knockback")]
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.25f;

        [Header("Entrada & Direção")]
        [SerializeField, Range(0f, 0.9f)] private float inputDeadzone = 0.2f;
        [SerializeField, Range(0f, 0.9f)] private float directionChangeDeadzone = 0.4f;
        [SerializeField] private bool snapTo8Directions = true;

        [Header("Referências de Componentes")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private PlayerVisualController visualController;
        [SerializeField] private PlayerSoundController soundController;
        [SerializeField] private PlayerVFX playerVFX;

        private const int DirectionCount = 8;
        private const float AngleStep = 360f / DirectionCount;

        private Vector2 moveInput;
        private MovementMode mode = MovementMode.Idle;
        private bool isActive = true;
        private Vector2 facingDirection = Vector2.down;
        private float currentSpeed;
        private bool wasHasInputLastFrame;

        private bool isDashing;
        private float dashTimer;
        private float dashCooldownTimer;
        private float dashRecoveryTimer;
        private Vector2 dashDirection;

        private float pendingAttackStep = 0f;
        private float attackLockTimer = 0f;
        private bool isSkidActive = false;

        // ── Lunge gradual (Attack2 / HeavyFinish) ──
        private float pendingLungeDistance = 0f;
        private float pendingLungeDuration = 0f;
        private float pendingLungeRemaining = 0f;
        private Vector2 lungeDirection;

        private bool _isKnockedBack = false;
        private Vector2 _knockbackVelocity;
        private float _knockbackTimer = 0f;

        public bool IsWalking { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsActive => isActive;
        public bool IsDashing => isDashing;
        public bool IsInDashRecovery => dashRecoveryTimer > 0f;
        public Vector2 FacingDirection => facingDirection;
        public float SprintSpeed => runSpeed * sprintMultiplier;
        public float DashCooldownRemaining => Mathf.Max(0f, dashCooldownTimer);
        public float CurrentSpeed => currentSpeed;
        public float DashDuration => dashDuration;

        public void UpdateMovementIntent(Vector2 input, MovementMode newMode)
        {
            if (isActive && !isDashing && !_isKnockedBack && dashRecoveryTimer <= 0f && attackLockTimer <= 0f)
            {
                bool wasSprinting = (mode == MovementMode.Sprinting);
                bool stoppedFromSprint = wasSprinting && (newMode == MovementMode.Walking || newMode == MovementMode.Idle);

                if (stoppedFromSprint && currentSpeed >= skidMinSpeed)
                {
                    visualController?.TriggerSkid();
                    soundController?.PlaySoundCategory("Skid");
                    isSkidActive = true;
                }
            }

            moveInput = input;
            mode = newMode;
        }

        public bool TryDash(Vector2 inputDirection)
        {
            if (!isActive || isDashing || _isKnockedBack || dashCooldownTimer > 0f) return false;

            Vector2 dir;
            if (HasValidInput(inputDirection))
                dir = snapTo8Directions ? SnapTo8Directions(inputDirection) : inputDirection.normalized;
            else
                dir = facingDirection;

            dashDirection = dir;
            facingDirection = dir;
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
            dashRecoveryTimer = 0f;
            pendingAttackStep = 0f;
            attackLockTimer = 0f;
            isSkidActive = false;

            // Limpa qualquer lunge pendente.
            pendingLungeDistance = 0f;
            pendingLungeDuration = 0f;
            pendingLungeRemaining = 0f;

            moveInput = Vector2.zero;

            visualController?.TriggerDashGhostEffect(dashDuration);
            soundController?.PlaySoundCategory("Dash");
            playerVFX?.PlayDashDustVFX();

            return true;
        }

        public void ApplyKnockback(Vector2 direction, float force)
        {
            _isKnockedBack = true;
            _knockbackTimer = knockbackDuration;
            _knockbackVelocity = direction * force;

            isDashing = false;
            dashRecoveryTimer = 0f;
            attackLockTimer = 0f;
            isSkidActive = false;
            currentSpeed = 0f;
            moveInput = Vector2.zero;

            // Limpa step/lunge pendentes.
            pendingAttackStep = 0f;
            pendingLungeDistance = 0f;
            pendingLungeDuration = 0f;
            pendingLungeRemaining = 0f;
        }

        public void RequestAttackStep(float distance)
        {
            if (!isActive || isDashing || _isKnockedBack || dashRecoveryTimer > 0f) return;
            pendingAttackStep += distance;
        }

        /// <summary>
        /// Lunge gradual: move o player <paramref name="distance"/> unidades ao longo
        /// de <paramref name="duration"/> segundos, na direção atual do facing.
        /// Usado pelo Attack2 e pelo HeavyFinish para criar trail de ghost visível.
        /// </summary>
        public void RequestAttackLunge(float distance, float duration)
        {
            if (!isActive || isDashing || _isKnockedBack || dashRecoveryTimer > 0f) return;
            if (distance <= 0f || duration <= 0f) return;

            lungeDirection = facingDirection;
            pendingLungeDistance = distance;
            pendingLungeDuration = duration;
            pendingLungeRemaining = distance;
        }

        public void StartAttackLock(float duration)
        {
            if (!isActive || isDashing || _isKnockedBack || dashRecoveryTimer > 0f) return;
            attackLockTimer = duration;
            moveInput = Vector2.zero;
            currentSpeed = 0f;
            isSkidActive = false;

            // Limpa estado de qualquer step/lunge anterior (evita carry-over entre combos).
            pendingAttackStep = 0f;
            pendingLungeDistance = 0f;
            pendingLungeDuration = 0f;
            pendingLungeRemaining = 0f;
        }

        public void CancelAttackLock()
        {
            attackLockTimer = 0f;
        }

        public void ForceCardinalFacing()
        {
            if (!isActive || isDashing || _isKnockedBack || dashRecoveryTimer > 0f) return;
            Vector2 dirToUse = HasValidInput(moveInput) ? moveInput : facingDirection;
            if (Mathf.Abs(dirToUse.x) > Mathf.Abs(dirToUse.y))
                facingDirection = dirToUse.x > 0 ? Vector2.right : Vector2.left;
            else
                facingDirection = dirToUse.y > 0 ? Vector2.up : Vector2.down;
            ApplyAnimatorState();
        }

        public bool HasValidInput(Vector2 input) => input.sqrMagnitude >= inputDeadzone * inputDeadzone;

        public void SetActive(bool active)
        {
            if (isActive == active) return;
            isActive = active;

            if (!active)
            {
                moveInput = Vector2.zero;
                mode = MovementMode.Idle;
                currentSpeed = 0f;
                wasHasInputLastFrame = false;
                isDashing = false;
                dashTimer = 0f;
                dashRecoveryTimer = 0f;
                pendingAttackStep = 0f;
                attackLockTimer = 0f;
                isSkidActive = false;
                _isKnockedBack = false;

                // Limpa lunge pendente.
                pendingLungeDistance = 0f;
                pendingLungeDuration = 0f;
                pendingLungeRemaining = 0f;

                IsWalking = false;
                IsRunning = false;
                if (rb != null) rb.linearVelocity = Vector2.zero;
                if (visualController != null && visualController.CurrentAnimator != null)
                    visualController.CurrentAnimator.speed = 1f;

                ApplyAnimatorState();
            }
        }

        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
            visualController = GetComponent<PlayerVisualController>();
            soundController = GetComponent<PlayerSoundController>();
            playerVFX = GetComponent<PlayerVFX>();
        }

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (visualController == null) visualController = GetComponent<PlayerVisualController>();
            if (soundController == null) soundController = GetComponent<PlayerSoundController>();
            if (playerVFX == null) playerVFX = GetComponent<PlayerVFX>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
        }

        private void FixedUpdate()
        {
            if (dashCooldownTimer > 0f) dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.fixedDeltaTime);

            if (!isActive)
            {
                currentSpeed = 0f; wasHasInputLastFrame = false;
                IsWalking = false; IsRunning = false; ApplyAnimatorState(); return;
            }

            if (_isKnockedBack)
            {
                _knockbackTimer -= Time.fixedDeltaTime;
                currentSpeed = 0f;
                IsWalking = false;
                IsRunning = false;
                ApplyAnimatorState();

                rb.MovePosition(rb.position + _knockbackVelocity * Time.fixedDeltaTime);

                if (_knockbackTimer <= 0f)
                {
                    _isKnockedBack = false;
                    _knockbackVelocity = Vector2.zero;
                }
                return;
            }

            if (isDashing)
            {
                dashTimer -= Time.fixedDeltaTime;
                rb.MovePosition(rb.position + dashDirection * dashSpeed * Time.fixedDeltaTime);
                bool hasInputDuringDash = HasValidInput(moveInput) && mode != MovementMode.Idle;
                IsWalking = hasInputDuringDash && mode == MovementMode.Walking;
                IsRunning = hasInputDuringDash && (mode == MovementMode.Running || mode == MovementMode.Sprinting);
                ApplyAnimatorState();

                if (syncAnimatorSpeed && visualController?.CurrentAnimator != null)
                    visualController.CurrentAnimator.speed = 1f;

                wasHasInputLastFrame = hasInputDuringDash;
                if (dashTimer <= 0f) { isDashing = false; dashRecoveryTimer = dashRecoveryDuration; currentSpeed = 0f; }
                return;
            }

            if (dashRecoveryTimer > 0f)
            {
                dashRecoveryTimer = Mathf.Max(0f, dashRecoveryTimer - Time.fixedDeltaTime);
                currentSpeed = 0f; IsWalking = false; IsRunning = false; ApplyAnimatorState();
                if (syncAnimatorSpeed && visualController?.CurrentAnimator != null)
                    visualController.CurrentAnimator.speed = 1f;
                wasHasInputLastFrame = false; return;
            }

            if (attackLockTimer > 0f)
            {
                attackLockTimer -= Time.fixedDeltaTime;
                currentSpeed = 0f;

                Vector2 totalMove = Vector2.zero;

                // Step instantâneo (Attack1 — comportamento legado mantido).
                if (pendingAttackStep > 0f)
                {
                    totalMove += facingDirection * pendingAttackStep;
                    pendingAttackStep = 0f;
                }

                // Lunge gradual (Attack2 / HeavyFinish).
                if (pendingLungeRemaining > 0f && pendingLungeDuration > 0f)
                {
                    float stepPerFrame = pendingLungeDistance * (Time.fixedDeltaTime / pendingLungeDuration);
                    float move = Mathf.Min(stepPerFrame, pendingLungeRemaining);
                    totalMove += lungeDirection * move;
                    pendingLungeRemaining -= move;
                }

                if (totalMove.sqrMagnitude > 0f)
                    rb.MovePosition(rb.position + totalMove);

                IsWalking = false; IsRunning = false; ApplyAnimatorState();
                if (syncAnimatorSpeed && visualController?.CurrentAnimator != null)
                    visualController.CurrentAnimator.speed = 1f;
                wasHasInputLastFrame = false;
                if (attackLockTimer > 0f) return;
            }

            bool hasInput = HasValidInput(moveInput) && mode != MovementMode.Idle;
            float targetSpeed = hasInput ? GetSpeedForMode(mode) : 0f;

            if (hasInput)
            {
                if (targetSpeed > currentSpeed + 0.01f)
                {
                    currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelerationRate * Time.fixedDeltaTime);
                    isSkidActive = false;
                }
                else
                {
                    float currentDecel = isSkidActive ? skidDecelerationRate : decelerationRate;
                    currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, currentDecel * Time.fixedDeltaTime);
                    if (currentSpeed <= targetSpeed + 0.01f) isSkidActive = false;
                }
            }
            else
            {
                float currentDecel = isSkidActive ? skidDecelerationRate : decelerationRate;
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, currentDecel * Time.fixedDeltaTime);
                if (currentSpeed < 0.1f) isSkidActive = false;
            }

            if (hasInput)
            {
                bool shouldUpdateFacing = !wasHasInputLastFrame || moveInput.sqrMagnitude >= directionChangeDeadzone * directionChangeDeadzone;
                if (shouldUpdateFacing) facingDirection = snapTo8Directions ? SnapTo8Directions(moveInput) : moveInput.normalized;
            }

            float stepForward = pendingAttackStep;
            if (pendingAttackStep > 0f) pendingAttackStep = 0f;

            if (currentSpeed > 0.001f || stepForward > 0f)
                rb.MovePosition(rb.position + facingDirection * (currentSpeed * Time.fixedDeltaTime + stepForward));

            IsWalking = hasInput && mode == MovementMode.Walking;
            IsRunning = hasInput && (mode == MovementMode.Running || mode == MovementMode.Sprinting);

            ApplyAnimatorState();
            UpdateAnimatorSpeed(hasInput);
            wasHasInputLastFrame = hasInput;
        }

        private void UpdateAnimatorSpeed(bool hasInput)
        {
            if (!syncAnimatorSpeed || visualController?.CurrentAnimator == null) return;

            if (hasInput)
            {
                // Define a velocidade da animação com base no estado exato
                if (mode == MovementMode.Running)
                {
                    visualController.CurrentAnimator.speed = runAnimationSpeed;
                }
                else if (mode == MovementMode.Sprinting)
                {
                    visualController.CurrentAnimator.speed = sprintAnimationSpeed;
                }
                else
                {
                    visualController.CurrentAnimator.speed = 1f;
                }
            }
            else
            {
                visualController.CurrentAnimator.speed = 1f;
            }
        }

        private float GetSpeedForMode(MovementMode m) => m switch
        {
            MovementMode.Walking => walkSpeed,
            MovementMode.Running => runSpeed,
            MovementMode.Sprinting => SprintSpeed,
            _ => 0f
        };

        private void ApplyAnimatorState()
        {
            visualController?.UpdateMovementAnimation(facingDirection, mode);
        }

        private static Vector2 SnapTo8Directions(Vector2 input)
        {
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            int snappedStep = Mathf.FloorToInt(angle / AngleStep + 0.5f);
            float rad = (snappedStep * AngleStep) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}