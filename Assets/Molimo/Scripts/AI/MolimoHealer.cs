using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class MolimoHealer : MonoBehaviour
    {
        private enum HealPhase { Approach, Pulsing }

        [Header("Referências Externas")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private PlayerVFX playerVFX;
        [SerializeField] private MolimoHealthContainer healthContainer;

        [Header("Gatilhos de Cura")]
        [SerializeField] private int healTriggerHealth = 10;

        [Header("Consumo por Pulso")]
        [SerializeField, Range(1, 1000)] private int hpConsumedPerPulse = 20;
        [SerializeField] private int healthRestoredPerUnit = 1;

        [Header("Movimento (2D)")]
        [SerializeField] private float flyToChestSpeed = 8f;
        [SerializeField] private Vector2 chestOffset = new Vector2(0f, 1.2f);
        [SerializeField] private float arrivalDistance = 0.1f;

        [Header("Pulsação")]
        [SerializeField] private float pulseInterval = 0.5f;
        [SerializeField] private float pulseSpeed = 8f;
        [SerializeField, Range(0f, 0.5f)] private float pulseAmplitude = 0.15f;

        private Transform _playerTransform;
        private Vector3 _originalScale = Vector3.one;

        private HealPhase _phase = HealPhase.Approach;
        private float _pulseTimer;
        private bool _isHealing;
        private bool _pulseEffectsActive;

        public bool IsHealing => _isHealing;

        public void Initialize(Transform player, MolimoHealthContainer container)
        {
            _playerTransform = player;
            if (container != null) healthContainer = container;
            ResolveReferences();
            if (healthContainer == null) healthContainer = GetComponent<MolimoHealthContainer>();
            _originalScale = transform.localScale;
        }

        private void ResolveReferences()
        {
            if (_playerTransform == null) return;
            if (playerHealth == null) playerHealth = _playerTransform.GetComponentInParent<PlayerHealth>();
            if (playerVFX == null) playerVFX = _playerTransform.GetComponentInParent<PlayerVFX>();
        }

        public bool CanHeal()
        {
            if (_isHealing) return false;
            ResolveReferences();
            if (healthContainer == null) healthContainer = GetComponent<MolimoHealthContainer>();

            if (playerHealth == null || healthContainer == null) return false;
            if (!healthContainer.HasHP) return false;
            if (playerHealth.IsDead) return false;

            return Mathf.RoundToInt(playerHealth.CurrentHealth) <= healTriggerHealth;
        }

        public void StartHealing()
        {
            _isHealing = true;
            _phase = HealPhase.Approach;
            _pulseTimer = 0f;
            _pulseEffectsActive = false;
        }

        public bool Tick()
        {
            if (!_isHealing) return false;
            ResolveReferences();
            if (healthContainer == null) healthContainer = GetComponent<MolimoHealthContainer>();

            if (healthContainer == null || !healthContainer.HasHP) { FinishHealing(); return false; }
            if (playerHealth == null || playerHealth.IsDead) { FinishHealing(); return false; }
            if (playerHealth.CurrentHealth >= playerHealth.MaxHealth) { FinishHealing(); return false; }

            Vector2 chestPos = (Vector2)_playerTransform.position + chestOffset;

            if (_phase == HealPhase.Approach)
            {
                if (Vector2.Distance(transform.position, chestPos) <= arrivalDistance) OnReachedChest();
                else transform.position = Vector2.MoveTowards(transform.position, chestPos, flyToChestSpeed * Time.deltaTime);
            }
            else if (_phase == HealPhase.Pulsing)
            {
                transform.position = chestPos;
                _pulseTimer += Time.deltaTime;
                transform.localScale = _originalScale * (1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmplitude);

                if (_pulseTimer >= pulseInterval) { _pulseTimer = 0f; ApplyHealPulse(); }
            }

            return true;
        }

        public void CancelHealing()
        {
            if (!_isHealing) { transform.localScale = _originalScale; return; }
            FinishHealing();
        }

        private void OnReachedChest()
        {
            _phase = HealPhase.Pulsing;
            _pulseTimer = 0f;
            if (_pulseEffectsActive) return;
            _pulseEffectsActive = true;

            if (playerVFX != null)
            {
                playerVFX.AuthorizeChargeVFX();
                playerVFX.SetChargeVFXState(true);
            }
        }

        private void ApplyHealPulse()
        {
            if (healthContainer == null || playerHealth == null || !healthContainer.HasHP) return;

            float before = playerHealth.CurrentHealth;
            if (before >= playerHealth.MaxHealth) return;

            int toConsume = Mathf.Min(hpConsumedPerPulse, healthContainer.CurrentHP);
            int healAmount = toConsume * healthRestoredPerUnit;

            playerHealth.Heal(healAmount);

            if (playerHealth.CurrentHealth - before > 0.01f)
                healthContainer.TryConsumeHP(toConsume);
        }

        private void FinishHealing()
        {
            _isHealing = false;
            transform.localScale = _originalScale;

            if (_pulseEffectsActive && playerVFX != null)
                playerVFX.SetChargeVFXState(false);

            _pulseEffectsActive = false;
        }
    }
}