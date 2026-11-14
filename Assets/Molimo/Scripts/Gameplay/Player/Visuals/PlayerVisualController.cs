using System.Collections;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class PlayerVisualController : MonoBehaviour
    {
        [Header("Visuais (Filhos)")]
        [SerializeField] private GameObject nonWeaponVisual;
        [SerializeField] private GameObject weaponVisual;

        [Header("Parâmetros do Animator")]
        [SerializeField] private string paramMoveX = "MoveX";
        [SerializeField] private string paramMoveY = "MoveY";
        [SerializeField] private string paramIsWalking = "IsWalking";
        [SerializeField] private string paramIsRunning = "IsRunning";
        [SerializeField] private string paramDead = "IsDead";

        [Header("Triggers do Animator")]
        [SerializeField] private string paramDash = "Dash";
        [SerializeField] private string paramSkid = "Skid";
        [SerializeField] private string paramHurt = "Hurt";
        [SerializeField] private string paramAttack1 = "Attack1";
        [SerializeField] private string paramAttack2 = "Attack2";
        [SerializeField] private string paramHeavyFinish = "HeavyFinish";

        private Animator nonWeaponAnimator;
        private Animator weaponAnimator;
        private Animator activeAnimator;
        private SpriteRenderer activeSpriteRenderer;

        private GhostEffect activeGhostEffect;

        private Vector2 cachedMove;
        private MovementMode cachedMode;
        private bool cachedDead;

        private Coroutine hitFlashRoutine;
        private Color originalSpriteColor = Color.white;

        public bool ShowingWeapon { get; private set; }
        public Animator CurrentAnimator => activeAnimator;

        private void Awake()
        {
            if (nonWeaponVisual != null)
            {
                nonWeaponAnimator = nonWeaponVisual.GetComponent<Animator>();
                var sr = nonWeaponVisual.GetComponentInChildren<SpriteRenderer>();
                if (sr != null) originalSpriteColor = sr.color;
            }

            if (weaponVisual != null)
            {
                weaponAnimator = weaponVisual.GetComponent<Animator>();
            }

            ApplyVisual(showWeapon: false);
        }

        public void SetHasWeapons(bool hasAnyWeapon)
        {
            if (ShowingWeapon == hasAnyWeapon) return;
            ApplyVisual(hasAnyWeapon);
        }

        // Chamado pelo PlayerMovement
        public void UpdateMovementAnimation(Vector2 move, MovementMode mode)
        {
            cachedMove = move;
            cachedMode = mode;

            if (activeAnimator == null) return;

            activeAnimator.SetFloat(paramMoveX, move.x);
            activeAnimator.SetFloat(paramMoveY, move.y);

            bool isWalking = mode == MovementMode.Walking;
            bool isRunning = mode == MovementMode.Running || mode == MovementMode.Sprinting;

            activeAnimator.SetBool(paramIsWalking, isWalking);
            activeAnimator.SetBool(paramIsRunning, isRunning);
        }

        // Chamado pelo PlayerBrain
        public void UpdateHealthAnimation(bool dead)
        {
            cachedDead = dead;
            if (activeAnimator != null) activeAnimator.SetBool(paramDead, dead);
        }

        // ==========================================================
        // MÉTODOS TRIGGERS
        // ==========================================================

        public void TriggerDashGhostEffect(float dashDuration)
        {
            activeGhostEffect?.StartGhostEffect(dashDuration);
            activeAnimator?.SetTrigger(paramDash);
        }

        /// <summary>
        /// Dispara apenas o ghost trail (sem tocar o trigger de Dash).
        /// Usado pelo Attack2 (ligeiro) e HeavyFinish (normal).
        /// </summary>
        public void TriggerAttackGhostEffect(float duration, int count, float startAlpha, float endAlpha, float lifetime)
        {
            activeGhostEffect?.StartGhostEffect(duration, count, startAlpha, endAlpha, lifetime);
        }

        public void TriggerSkid()
        {
            activeAnimator?.SetTrigger(paramSkid);
        }

        public void TriggerHurt()
        {
            activeAnimator?.SetTrigger(paramHurt);

            if (activeSpriteRenderer != null)
            {
                if (hitFlashRoutine != null) StopCoroutine(hitFlashRoutine);
                hitFlashRoutine = StartCoroutine(FlashDamageRoutine());
            }
        }

        public void TriggerAttack(int comboStep)
        {
            if (activeAnimator == null) return;
            if (comboStep == 1) activeAnimator.SetTrigger(paramAttack1);
            else if (comboStep == 2) activeAnimator.SetTrigger(paramAttack2);
            else if (comboStep == 3) activeAnimator.SetTrigger(paramHeavyFinish);
        }

        private IEnumerator FlashDamageRoutine()
        {
            activeSpriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            activeSpriteRenderer.color = originalSpriteColor;
            hitFlashRoutine = null;
        }

        private void ApplyVisual(bool showWeapon)
        {
            ShowingWeapon = showWeapon;

            if (nonWeaponVisual != null) nonWeaponVisual.SetActive(!showWeapon);
            if (weaponVisual != null) weaponVisual.SetActive(showWeapon);

            activeAnimator = showWeapon ? weaponAnimator : nonWeaponAnimator;
            GameObject activeVisualObj = showWeapon ? weaponVisual : nonWeaponVisual;

            if (activeVisualObj != null)
            {
                activeGhostEffect = activeVisualObj.GetComponentInChildren<GhostEffect>();
                activeSpriteRenderer = activeVisualObj.GetComponentInChildren<SpriteRenderer>();

                if (activeSpriteRenderer != null) originalSpriteColor = activeSpriteRenderer.color;
            }

            // Reaplica o estado ao Animator recém-ativado
            if (activeAnimator != null)
            {
                UpdateMovementAnimation(cachedMove, cachedMode);
                UpdateHealthAnimation(cachedDead);
            }
        }
    }
}