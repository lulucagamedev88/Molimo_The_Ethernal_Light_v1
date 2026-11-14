using UnityEngine;
using UnityEngine.InputSystem;

namespace MolimoByLulucaGameDev
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerHealth))]
    public class PlayerBrain : MonoBehaviour
    {
        [Header("Referências de Componentes")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerVisualController visualController;

        [Header("Trava de Sprint Pós-Exaustão")]
        [Tooltip("Após drenar até 0, Sprint fica bloqueado até a stamina chegar neste percentual (0–1).")]
        [SerializeField, Range(0f, 1f)] private float sprintRecoverMin = 0.2f;

        public bool IsActive { get; private set; } = true;
        public bool IsAlive => health != null && !health.IsDead;

        private Vector2 rawMove;
        private bool rawSprint;
        private bool dashRequested;

        // Cache das actions — lidas em Update como safety net.
        private InputAction sprintAction;
        private InputAction dashAction;

        private bool sprintBlockedByExhaustion;

        private void Reset()
        {
            movement = GetComponent<PlayerMovement>();
            health = GetComponent<PlayerHealth>();
            playerInput = GetComponent<PlayerInput>();
            visualController = GetComponent<PlayerVisualController>();
        }

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (health == null) health = GetComponent<PlayerHealth>();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (visualController == null) visualController = GetComponent<PlayerVisualController>();

            var asset = playerInput.actions;
            if (asset != null)
            {
                sprintAction = asset.FindAction("Sprint", throwIfNotFound: false);
                dashAction = asset.FindAction("Dash", throwIfNotFound: false);
            }

            if (sprintAction == null)
                Debug.LogWarning("[PlayerBrain] Action 'Sprint' não encontrada no InputActionAsset.", this);

            if (dashAction == null)
                Debug.LogWarning("[PlayerBrain] Action 'Dash' não encontrada no InputActionAsset.", this);

            health.OnDied += HandleDeath;

            // Estado inicial do Animator: Vivo.
            visualController?.UpdateHealthAnimation(false);
        }

        private void Start()
        {
            // Se a stamina inicial é 0 (ou quase), bloquear sprint desde o frame 1.
            // Sem isto, se o Player carregar Sprint no arranque, o ComputeMode
            // devolvia Sprinting mesmo com stamina a 0, esgotando-a instantaneamente
            // e causando o comportamento "stutter" no primeiro segundo.
            if (health != null && health.CurrentStamina <= 0.001f)
                sprintBlockedByExhaustion = true;
        }

        private void OnDestroy()
        {
            if (health != null) health.OnDied -= HandleDeath;
        }

        // ==========================================================
        // Callbacks de Input
        // ==========================================================
        public void OnMove(InputValue value) { if (IsActive) rawMove = value.Get<Vector2>(); }
        public void OnSprint(InputValue value) { if (IsActive) rawSprint = value.isPressed; }

        public void OnDash(InputValue value)
        {
            if (IsActive && value.isPressed) dashRequested = true;
        }

        // ==========================================================
        // Loop Principal
        // ==========================================================
        private void Update()
        {
            if (!IsActive) return;

            // ── SAFETY NETS ──
            if (sprintAction != null && sprintAction.enabled)
                rawSprint = sprintAction.IsPressed();

            if (dashAction != null && dashAction.enabled && dashAction.WasPressedThisFrame())
                dashRequested = true;

            UpdateExhaustionLock();

            MovementMode mode = ComputeMode();

            // O PlayerMovement agora é o responsável por atualizar a animação de Walk/Run no Animator
            movement.UpdateMovementIntent(rawMove, mode);

            // Consome o pedido de dash uma única vez por frame.
            if (dashRequested)
            {
                dashRequested = false;

                if (health.HasStaminaForDash() && movement.TryDash(rawMove))
                    health.ConsumeStamina(health.DashStaminaCost);
            }

            health.TickStamina(Time.deltaTime, mode == MovementMode.Sprinting);
        }

        private void UpdateExhaustionLock()
        {
            float ratio = health.MaxStamina > 0f
                ? health.CurrentStamina / health.MaxStamina
                : 0f;

            // Bloqueia sprint quando a stamina chega a 0 (com drain ativo OU
            // diretamente, caso tenha sido inicializada a 0).
            if (!sprintBlockedByExhaustion
                && health.CurrentStamina <= 0.001f
                && (health.IsDrainActive || ratio <= 0f))
            {
                sprintBlockedByExhaustion = true;
            }

            if (sprintBlockedByExhaustion && ratio >= sprintRecoverMin)
            {
                sprintBlockedByExhaustion = false;
            }
        }

        /// <summary>
        /// Prioridade: Walking (stamina fraca) > Sprinting > Running.
        /// A verificação de IsStaminaWeak vem ANTES do Sprint para evitar
        /// que o Player entre em Sprint com stamina baixa só porque carregou
        /// o botão antes de bloquear.
        /// </summary>
        private MovementMode ComputeMode()
        {
            if (!movement.HasValidInput(rawMove)) return MovementMode.Idle;

            // Stamina fraca (< staminaWeakThreshold) → sempre Walking.
            // Isto garante que ao iniciar com stamina = 0, o Player ANDA,
            // independentemente do input de Sprint.
            if (health.IsStaminaWeak) return MovementMode.Walking;

            if (rawSprint && !sprintBlockedByExhaustion) return MovementMode.Sprinting;
            return MovementMode.Running;
        }

        // ==========================================================
        // Morte / Revive
        // ==========================================================
        private void HandleDeath()
        {
            Debug.Log("[PlayerBrain] Morte detectada — paralisando Brain.");
            DeactivateBrain();
        }

        private void DeactivateBrain()
        {
            if (!IsActive) return;
            IsActive = false;

            rawMove = Vector2.zero;
            rawSprint = false;
            dashRequested = false;

            movement.UpdateMovementIntent(Vector2.zero, MovementMode.Idle);
            movement.SetActive(false);
            health.TickStamina(0f, false);

            // Avisa o Animator para tocar a animação de morte
            visualController?.UpdateHealthAnimation(true);

            if (playerInput != null) playerInput.DeactivateInput();
        }

        public void Revive(float healthOverride = -1f)
        {
            if (IsActive) return;
            IsActive = true;

            sprintBlockedByExhaustion = false;
            dashRequested = false;
            health.ResetForRevive(healthOverride);
            movement.SetActive(true);

            // Avisa o Animator que o player está vivo novamente
            visualController?.UpdateHealthAnimation(false);

            if (playerInput != null) playerInput.ActivateInput();
        }
    }
}