using System.Collections;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    [DisallowMultipleComponent]
    public class PlayerVFX : MonoBehaviour
    {
        [Header("Global Settings")]
        [SerializeField] private bool vfxEnabled = true;

        #region Component References
        [Header("Referências 2D")]
        [SerializeField] private PlayerMovement movement;
        [SerializeField] private SpriteRenderer playerSprite;
        #endregion

        #region Dust Settings
        [Header("Poeira de Movimento (Dust)")]
        [SerializeField] private ParticleSystem runDustPS;
        [SerializeField] private ParticleSystem jumpDustPS;
        [SerializeField] private ParticleSystem landDustPS;
        [SerializeField] private ParticleSystem dashDustPS;
        [SerializeField] private float minimumSpeedToEmit = 2.0f;
        #endregion

        #region Combat VFX
        [Header("Combat VFX")]
        [Tooltip("Efeito de corte (Slash) que acompanha a direção do ataque.")]
        [SerializeField] private ParticleSystem slashVfxPS;
        [SerializeField] private float slashRotationOffset = 0f;
        #endregion

        #region Action VFX Systems
        [Header("Action VFX")]
        [SerializeField] private ParticleSystem hurtImpactPS;
        [SerializeField] private ParticleSystem healBurstPS;
        [SerializeField] private ParticleSystem levelUpPS;
        [SerializeField] private ParticleSystem blockImpactPS;
        [SerializeField] private ParticleSystem blockAuraPS;
        #endregion

        #region Charge VFX Settings
        [Header("Charge VFX Settings (Molimo Healer)")]
        [SerializeField] private ParticleSystem chargePS;
        [SerializeField] private bool requireAuthorizationForCharge = true;
        [SerializeField, Range(0.1f, 3f)] private float chargeRampInDuration = 0.8f;
        [SerializeField, Range(0.5f, 4f)] private float chargeRampInPower = 2f;
        [SerializeField, Range(0.1f, 3f)] private float chargeRampOutDuration = 0.5f;
        [SerializeField, Range(0.2f, 4f)] private float chargeRampOutPower = 0.5f;
        #endregion

        #region Internal State
        private bool isCurrentlyEmittingDust;
        private bool isCurrentlyBlocking;
        private bool isCurrentlyCharging;
        private Coroutine _chargeRoutine;
        private float _chargeMaxRate = 1f;
        private bool _chargeMaxRateCaptured;
        private bool _chargeAuthorized;

        private Vector3 _originalSlashScale;
        #endregion

        #region Properties
        public bool VFXEnabled
        {
            get => vfxEnabled;
            set
            {
                vfxEnabled = value;
                if (!vfxEnabled) StopAllContinuousVFX();
            }
        }
        public bool IsEmittingDust => isCurrentlyEmittingDust;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeReferences();

            // Guarda a escala original do Slash para podermos inverter e voltar ao normal
            if (slashVfxPS != null)
                _originalSlashScale = slashVfxPS.transform.localScale;
        }

        private void Update() { if (vfxEnabled) UpdateMovementDust(); }
        private void OnDisable() => StopAllContinuousVFX();
        #endregion

        #region Initialization
        private void InitializeReferences()
        {
            if (movement == null) movement = GetComponentInParent<PlayerMovement>();
            if (playerSprite == null) playerSprite = GetComponentInParent<SpriteRenderer>();

            if (chargePS != null && !_chargeMaxRateCaptured)
            {
                _chargeMaxRate = chargePS.emission.rateOverTimeMultiplier;
                _chargeMaxRateCaptured = true;
                var em = chargePS.emission;
                em.rateOverTimeMultiplier = 0f;
                chargePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            SyncAllVFXSortingLayers();
        }

        private void SyncAllVFXSortingLayers()
        {
            if (playerSprite == null) return;

            SetSortingLayer(runDustPS, 0);
            SetSortingLayer(jumpDustPS, 0);
            SetSortingLayer(landDustPS, 0);
            SetSortingLayer(dashDustPS, 0);

            SetSortingLayer(slashVfxPS, 1);

            SetSortingLayer(blockAuraPS, 2);
            SetSortingLayer(chargePS, 2);
            SetSortingLayer(healBurstPS, 3);
        }

        private void SetSortingLayer(ParticleSystem ps, int explicitOrder)
        {
            if (ps == null || playerSprite == null) return;
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingLayerID = playerSprite.sortingLayerID;
                renderer.sortingOrder = explicitOrder;
            }
        }
        #endregion

        #region Combat Logic
        // ATENÇÃO: O método agora aceita 2 argumentos (direção e flip)
        public void PlaySlashVfx(Vector2 direction, bool flipHorizontal = false)
        {
            if (!vfxEnabled || slashVfxPS == null) return;

            float angleZ = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float angleX = (Mathf.Abs(direction.x) > 0.1f) ? -60f : -30f;

            // Aplica a inversão no eixo X se o parâmetro for true
            if (flipHorizontal)
                slashVfxPS.transform.localScale = new Vector3(-_originalSlashScale.x, _originalSlashScale.y, _originalSlashScale.z);
            else
                slashVfxPS.transform.localScale = _originalSlashScale;

            slashVfxPS.transform.localRotation = Quaternion.Euler(angleX, 0f, angleZ + slashRotationOffset);
            slashVfxPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            slashVfxPS.Play(true);
        }
        #endregion

        #region Dust Logic (Correr)
        private void UpdateMovementDust()
        {
            if (runDustPS == null || movement == null) return;

            float currentSpeed = movement.CurrentSpeed;
            bool shouldEmit = currentSpeed >= minimumSpeedToEmit && movement.IsActive && !movement.IsDashing;

            if (shouldEmit != isCurrentlyEmittingDust)
            {
                isCurrentlyEmittingDust = shouldEmit;
                if (shouldEmit) runDustPS.Play(true);
                else runDustPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
        #endregion

        #region One-Shot Action VFX
        public void PlayJumpDustVFX() { if (vfxEnabled) PlayParticleSystem(jumpDustPS); }
        public void PlayLandDustVFX() { if (vfxEnabled) PlayParticleSystem(landDustPS); }
        public void PlayDashDustVFX() { if (vfxEnabled) PlayParticleSystem(dashDustPS); }
        public void PlayHurtVFX() { if (vfxEnabled) PlayParticleSystem(hurtImpactPS); }
        public void PlayHealVFX() { if (vfxEnabled) PlayParticleSystem(healBurstPS); }
        public void PlayLevelUpVFX() { if (vfxEnabled) PlayParticleSystem(levelUpPS); }
        public void PlayBlockImpactVFX() { if (vfxEnabled) PlayParticleSystem(blockImpactPS); }

        private void PlayParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
        #endregion

        #region Block & Charge
        public void SetBlockVFXState(bool isBlocking)
        {
            if (!vfxEnabled) { if (blockAuraPS != null) blockAuraPS.Stop(); return; }
            if (isBlocking == isCurrentlyBlocking) return;
            isCurrentlyBlocking = isBlocking;
            if (blockAuraPS != null) { if (isBlocking) blockAuraPS.Play(true); else blockAuraPS.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
        }

        public void AuthorizeChargeVFX() => _chargeAuthorized = true;
        public void SetChargeVFXState(bool isCharging)
        {
            if (chargePS == null) return;
            if (!vfxEnabled) { ForceStopCharge(); return; }
            if (isCharging)
            {
                if (requireAuthorizationForCharge && !_chargeAuthorized) return;
                _chargeAuthorized = false;
            }
            if (isCharging == isCurrentlyCharging) return;
            isCurrentlyCharging = isCharging;
            if (_chargeRoutine != null) StopCoroutine(_chargeRoutine);
            _chargeRoutine = isCharging ? StartCoroutine(ChargeRampInRoutine()) : StartCoroutine(ChargeRampOutRoutine());
        }
        private IEnumerator ChargeRampInRoutine()
        {
            var emission = chargePS.emission; emission.rateOverTimeMultiplier = 0f; chargePS.Play(true);
            float t = 0f; while (t < chargeRampInDuration) { t += Time.deltaTime; emission.rateOverTimeMultiplier = _chargeMaxRate * Mathf.Pow(Mathf.Clamp01(t / chargeRampInDuration), chargeRampInPower); yield return null; }
            emission.rateOverTimeMultiplier = _chargeMaxRate; _chargeRoutine = null;
        }
        private IEnumerator ChargeRampOutRoutine()
        {
            var emission = chargePS.emission; float startRate = emission.rateOverTimeMultiplier;
            float t = 0f; while (t < chargeRampOutDuration) { t += Time.deltaTime; emission.rateOverTimeMultiplier = startRate * (1f - Mathf.Pow(Mathf.Clamp01(t / chargeRampOutDuration), chargeRampOutPower)); yield return null; }
            emission.rateOverTimeMultiplier = 0f; chargePS.Stop(true, ParticleSystemStopBehavior.StopEmitting); _chargeRoutine = null;
        }
        private void ForceStopCharge()
        {
            if (_chargeRoutine != null) { StopCoroutine(_chargeRoutine); _chargeRoutine = null; }
            if (chargePS != null) { var emission = chargePS.emission; emission.rateOverTimeMultiplier = 0f; chargePS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
            isCurrentlyCharging = false; _chargeAuthorized = false;
        }
        #endregion

        public void StopAllContinuousVFX()
        {
            isCurrentlyEmittingDust = false; isCurrentlyBlocking = false;
            if (runDustPS != null) runDustPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (dashDustPS != null) dashDustPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (blockAuraPS != null) blockAuraPS.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ForceStopCharge();
        }
    }
}