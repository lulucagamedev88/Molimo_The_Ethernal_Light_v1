using UnityEngine;
using UnityEngine.InputSystem;

namespace MolimoByLulucaGameDev
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Referências de Componentes")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private PlayerSoundController soundController;
        [SerializeField] private AttackHitbox hitboxTester;
        [SerializeField] private PlayerVFX playerVFX;
        [SerializeField] private WeaponEquip weaponEquip;
        [SerializeField] private PlayerVisualController visualController;

        [Header("Configuração do Combo Triplo")]
        [SerializeField, Range(0.1f, 0.9f)] private float bufferWindowStartPercent = 0.5f;
        [SerializeField] private float comboResetDelay = 0.2f;
        [SerializeField] private float fallbackAttackDuration = 0.3f;

        [Header("Steps por Ataque")]
        [Tooltip("Step instantâneo do Attack1 (comportamento antigo).")]
        [SerializeField] private float attack1StepForward = 0.05f;
        [Tooltip("Distância total do lunge do Attack2.")]
        [SerializeField] private float attack2StepForward = 0.15f;
        [Tooltip("Distância total do lunge do HeavyFinish (maior que Attack2).")]
        [SerializeField] private float heavyFinishStepForward = 0.35f;

        [Header("Duração dos Lunges (gradual)")]
        [Tooltip("Se > 0, o step é gradual (lunge). Se 0, cai no step instantâneo legado.")]
        [SerializeField, Min(0f)] private float attack2LungeDuration = 0.15f;
        [SerializeField, Min(0f)] private float heavyFinishLungeDuration = 0.25f;

        [Header("Ghost Trail — Attack2 (ligeiro)")]
        [SerializeField] private bool ghostOnAttack2 = true;
        [SerializeField, Min(0.01f)] private float attack2GhostDuration = 0.15f;
        [SerializeField, Range(4, 15)] private int attack2GhostCount = 5;
        [SerializeField, Range(0f, 1f)] private float attack2GhostStartAlpha = 0.25f;
        [SerializeField, Range(0f, 1f)] private float attack2GhostEndAlpha = 0f;
        [SerializeField, Min(0.01f)] private float attack2GhostLifetime = 0.15f;

        [Header("Ghost Trail — HeavyFinish (normal)")]
        [SerializeField] private bool ghostOnHeavyFinish = true;
        [SerializeField, Min(0.01f)] private float heavyFinishGhostDuration = 0.25f;
        [SerializeField, Range(4, 15)] private int heavyFinishGhostCount = 8;
        [SerializeField, Range(0f, 1f)] private float heavyFinishGhostStartAlpha = 0.4f;
        [SerializeField, Range(0f, 1f)] private float heavyFinishGhostEndAlpha = 0f;
        [SerializeField, Min(0.01f)] private float heavyFinishGhostLifetime = 0.25f;

        [Header("HeavyFinish — Tipos de Hit por ActivateHitbox")]
        [Tooltip("Sequência de AttackHitType. Índice 0 = 1º ActivateHitbox, índice 1 = 2º, etc. " +
                 "Se a sequência for menor que o nº de hits, o último tipo é repetido.")]
        [SerializeField]
        private AttackHitType[] heavyFinishHitSequence = new AttackHitType[]
        {
            AttackHitType.HeavyGlue,      // Hitbox 1 → cola
            AttackHitType.HeavyGlue,      // Hitbox 2 → re-cola (refresh)
            AttackHitType.HeavyKnockback  // Hitbox 3 → empurra
        };

        [Header("Slash VFX — Flip Horizontal por Step")]
        [Tooltip("Inverte o slash VFX no eixo X durante o Attack1.")]
        [SerializeField] private bool flipSlashOnAttack1 = false;
        [Tooltip("Inverte o slash VFX no eixo X durante o Attack2.")]
        [SerializeField] private bool flipSlashOnAttack2 = true;
        [Tooltip("Inverte o slash VFX no eixo X durante o Attack3 (Heavy Finish).")]
        [SerializeField] private bool flipSlashOnAttack3 = false;

        [Header("Animation Events (Segurança)")]
        [Tooltip("Tempo extra após o fim do clip antes de libertar o AttackLock caso o evento 'CancelAttackLock' não dispare.")]
        [SerializeField] private float watchdogGrace = 0.15f;

        [Tooltip("Se o evento 'ActivateHitbox' não disparar até ao fim do clip, executa o hit à força e avisa no Console.")]
        [SerializeField] private bool fallbackDeliverHitIfEventMissing = true;

        public bool IsActive { get; private set; } = true;

        private const string Attack1Trigger = "Attack1";
        private const string Attack2Trigger = "Attack2";
        private const string HeavyFinishTrigger = "HeavyFinish";

        private InputAction attackAction;
        private bool attackRequested;
        private int currentComboStep = 0;
        private float attackTimer = 0f;
        private float currentAttackDuration = 0f;
        private bool inputBuffered = false;
        private bool isAttacking = false;

        // Attack lock
        private bool attackLockReleased = false;
        private int lastStepFrame = -1;

        // Hit delivery (controlado pelo Animation Event 'ActivateHitbox')
        private bool hitEventReceivedThisStep = false;
        private int lastHitFrame = -1; // Controla o hit por frame para permitir multi-hits
        private Vector2 pendingHitDirection = Vector2.right;

        // Estado do hit atual dentro do step (usado para rotear hit type no HeavyFinish).
        private int hitCounterThisStep = 0;

        private void Reset()
        {
            movement = GetComponent<PlayerMovement>();
            playerInput = GetComponent<PlayerInput>();
            soundController = GetComponent<PlayerSoundController>();
            hitboxTester = GetComponent<AttackHitbox>();
            playerVFX = GetComponent<PlayerVFX>();
            weaponEquip = GetComponent<WeaponEquip>();
            visualController = GetComponent<PlayerVisualController>();
        }

        private void Awake()
        {
            if (movement == null) movement = GetComponent<PlayerMovement>();
            if (playerInput == null) playerInput = GetComponent<PlayerInput>();
            if (soundController == null) soundController = GetComponent<PlayerSoundController>();
            if (hitboxTester == null) hitboxTester = GetComponent<AttackHitbox>();
            if (playerVFX == null) playerVFX = GetComponent<PlayerVFX>();
            if (weaponEquip == null) weaponEquip = GetComponent<WeaponEquip>();
            if (visualController == null) visualController = GetComponent<PlayerVisualController>();

            var asset = playerInput.actions;
            if (asset != null) attackAction = asset.FindAction("Attack", throwIfNotFound: false);
        }

        public void OnAttack(InputValue value)
        {
            if (IsActive && weaponEquip != null && weaponEquip.HasSword && value.isPressed)
                attackRequested = true;
        }

        private void Update()
        {
            if (!IsActive) return;

            if (attackAction != null && attackAction.enabled && attackAction.WasPressedThisFrame())
            {
                if (weaponEquip != null && weaponEquip.HasSword) attackRequested = true;
            }

            if (attackRequested)
            {
                attackRequested = false;
                HandleAttackInput();
            }

            if (!isAttacking) return;

            attackTimer += Time.deltaTime;

            // --- Watchdog do AttackLock (caso 'CancelAttackLock' não dispare) ---
            if (!attackLockReleased && attackTimer >= currentAttackDuration + watchdogGrace)
            {
                attackLockReleased = true;
                movement.CancelAttackLock();
            }

            // --- Fallback do hit (caso 'ActivateHitbox' não dispare nenhuma vez) ---
            if (!hitEventReceivedThisStep && attackTimer >= currentAttackDuration)
            {
                if (fallbackDeliverHitIfEventMissing)
                {
                    Debug.LogWarning($"[PlayerCombat] Nenhum evento 'ActivateHitbox' disparou no clip do step {currentComboStep}. A executar hit de fallback. Verifica os eventos no clip.");
                    DeliverHit();
                }
            }

            // --- Buffer: encadeia assim que o lock for libertado ---
            if (attackLockReleased && inputBuffered)
            {
                inputBuffered = false;
                AdvanceCombo();
                return;
            }

            // --- Reset natural do combo ---
            if (attackTimer >= currentAttackDuration + comboResetDelay)
            {
                ResetCombo();
            }
        }

        private void HandleAttackInput()
        {
            if (movement.IsDashing || !IsActive) return;

            if (weaponEquip == null || !weaponEquip.HasSword)
            {
                ResetCombo();
                return;
            }

            if (!isAttacking)
            {
                ExecuteAttackStep(1);
                return;
            }

            bool bufferAllowed = attackLockReleased
                || attackTimer >= currentAttackDuration * bufferWindowStartPercent;

            if (bufferAllowed && !inputBuffered) inputBuffered = true;
        }

        private void AdvanceCombo()
        {
            if (currentComboStep == 1) ExecuteAttackStep(2);
            else if (currentComboStep == 2) ExecuteAttackStep(3);
            else if (currentComboStep == 3) ResetCombo();
        }

        private void ExecuteAttackStep(int step)
        {
            currentComboStep = step;
            isAttacking = true;
            attackTimer = 0f;
            inputBuffered = false;
            attackLockReleased = false;
            hitEventReceivedThisStep = false;
            hitCounterThisStep = 0;
            lastHitFrame = -1;
            lastStepFrame = Time.frameCount;

            string clipNameToSearch = Attack1Trigger;
            if (step == 2) clipNameToSearch = Attack2Trigger;
            else if (step == 3) clipNameToSearch = HeavyFinishTrigger;

            currentAttackDuration = GetAnimationClipLength(clipNameToSearch);

            movement.ForceCardinalFacing();
            pendingHitDirection = movement.FacingDirection; // captura após snap cardinal
            movement.StartAttackLock(currentAttackDuration);

            visualController?.TriggerAttack(step);
            soundController?.PlaySoundCategory("Slash");

            // ── Step / Lunge + Ghost por step ──
            switch (step)
            {
                case 1:
                    // Step instantâneo legado — comportamento intocado.
                    if (attack1StepForward > 0f)
                        movement.RequestAttackStep(attack1StepForward);
                    break;

                case 2:
                    // Lunge gradual (ou instantâneo, se lungeDuration = 0).
                    if (attack2StepForward > 0f)
                    {
                        if (attack2LungeDuration > 0f)
                            movement.RequestAttackLunge(attack2StepForward, attack2LungeDuration);
                        else
                            movement.RequestAttackStep(attack2StepForward);
                    }

                    if (ghostOnAttack2)
                        visualController?.TriggerAttackGhostEffect(
                            attack2GhostDuration, attack2GhostCount,
                            attack2GhostStartAlpha, attack2GhostEndAlpha, attack2GhostLifetime);
                    break;

                case 3:
                    if (heavyFinishStepForward > 0f)
                    {
                        if (heavyFinishLungeDuration > 0f)
                            movement.RequestAttackLunge(heavyFinishStepForward, heavyFinishLungeDuration);
                        else
                            movement.RequestAttackStep(heavyFinishStepForward);
                    }

                    if (ghostOnHeavyFinish)
                        visualController?.TriggerAttackGhostEffect(
                            heavyFinishGhostDuration, heavyFinishGhostCount,
                            heavyFinishGhostStartAlpha, heavyFinishGhostEndAlpha, heavyFinishGhostLifetime);
                    break;
            }

            bool shouldFlipVfx = step switch
            {
                1 => flipSlashOnAttack1,
                2 => flipSlashOnAttack2,
                3 => flipSlashOnAttack3,
                _ => false
            };
            playerVFX?.PlaySlashVfx(movement.FacingDirection, flipHorizontal: shouldFlipVfx);
        }

        /// <summary>
        /// Chamado via Animation Event 'ActivateHitbox'.
        /// Permite múltiplos hits no mesmo step (HeavyFinish: 3 hitboxes).
        /// O tipo de hit é roteado pela sequência configurada (heavyFinishHitSequence).
        /// </summary>
        public void NotifyAttackAnimationHit()
        {
            if (!isAttacking) return;
            if (Time.frameCount <= lastStepFrame) return; // Ignora eventos do step anterior
            if (Time.frameCount == lastHitFrame) return;  // Previne duplo hit no MESMO frame

            lastHitFrame = Time.frameCount;

            AttackHitType hitType = GetHitTypeForCurrentHit();
            hitCounterThisStep++;

            DeliverHit(hitType);
        }

        /// <summary>
        /// Decide o tipo de hit com base no step atual e no contador de hits.
        /// Attack1 / Attack2 → sempre Normal.
        /// HeavyFinish → consulta heavyFinishHitSequence.
        /// </summary>
        private AttackHitType GetHitTypeForCurrentHit()
        {
            if (currentComboStep == 3 && heavyFinishHitSequence != null && heavyFinishHitSequence.Length > 0)
            {
                int idx = Mathf.Min(hitCounterThisStep, heavyFinishHitSequence.Length - 1);
                return heavyFinishHitSequence[idx];
            }
            return AttackHitType.Normal;
        }

        /// <summary>
        /// Chamado via Animation Event 'CancelAttackLock' no fim do clip.
        /// </summary>
        public void NotifyAttackAnimationEnd()
        {
            if (!isAttacking || attackLockReleased) return;
            if (Time.frameCount <= lastStepFrame) return;

            attackLockReleased = true;
            movement?.CancelAttackLock();

            if (inputBuffered)
            {
                inputBuffered = false;
                AdvanceCombo();
            }
        }

        private void DeliverHit(AttackHitType hitType = AttackHitType.Normal)
        {
            hitEventReceivedThisStep = true;
            hitboxTester?.PerformHitTest(pendingHitDirection, hitType);
        }

        private void ResetCombo()
        {
            if (movement != null && !attackLockReleased) movement.CancelAttackLock();

            isAttacking = false;
            currentComboStep = 0;
            attackTimer = 0f;
            inputBuffered = false;
            attackLockReleased = false;
            hitEventReceivedThisStep = false;
            hitCounterThisStep = 0;
            lastHitFrame = -1;
        }

        private float GetAnimationClipLength(string clipName)
        {
            Animator activeAnimator = visualController != null ? visualController.CurrentAnimator : null;
            if (activeAnimator == null || activeAnimator.runtimeAnimatorController == null) return fallbackAttackDuration;

            float maxLength = 0f;
            foreach (var clip in activeAnimator.runtimeAnimatorController.animationClips)
            {
                if (clip.name.Contains(clipName) && clip.length > maxLength) maxLength = clip.length;
            }
            return maxLength > 0f ? maxLength : fallbackAttackDuration;
        }

        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;
            if (!active) ResetCombo();
        }
    }
}