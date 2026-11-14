using UnityEngine;
using System;

namespace MolimoByLulucaGameDev
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Vida (HP)")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;

        [Tooltip("Stamina com que o Player começa em jogo. " +
                 "0 = começa a caminhar e recupera (recomendado). " +
                 "-1 = começa cheio (comportamento antigo, arranca a correr).")]
        [SerializeField] private float initialStamina = 0f;

        [SerializeField] private float sprintDrainRate = 15f;
        [SerializeField] private float staminaRegenRate = 20f;
        [SerializeField] private float staminaRegenDelay = 1f;
        [SerializeField] private float dashStaminaCost = 25f;

        [Header("Estado de Exaustão")]
        [SerializeField, Range(0f, 1f)] private float staminaWeakThreshold = 0.25f;

        [Header("Referências")]
        [SerializeField] private PlayerVisualController visualController;

        // Eventos
        public event Action OnDied;
        public event Action<float, float> OnHealthChanged;  // (current, max)
        public event Action<float, float> OnStaminaChanged; // (current, max)

        // Propriedades públicas (HUD, Vignette, Molimo, etc)
        public bool IsDead { get; private set; }
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public float DashStaminaCost => dashStaminaCost;
        public bool IsStaminaWeak => maxStamina > 0f && (currentStamina / maxStamina) < staminaWeakThreshold;
        public bool IsDrainActive { get; private set; }

        private float lastSprintTime;

        private void Reset()
        {
            visualController = GetComponent<PlayerVisualController>();
        }

        private void Awake()
        {
            if (visualController == null) visualController = GetComponent<PlayerVisualController>();

            currentHealth = maxHealth;

            // initialStamina < 0 → começa cheio (legacy).
            // initialStamina >= 0 → começa no valor indicado (default = 0 → Walking).
            currentStamina = initialStamina < 0f
                ? maxStamina
                : Mathf.Clamp(initialStamina, 0f, maxStamina);

            // Regeneração começa IMEDIATAMENTE (sem esperar staminaRegenDelay),
            // caso contrário o Player fica 1s com a barra parada em 0 no arranque.
            lastSprintTime = Time.time - staminaRegenDelay;

            IsDead = false;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        // ==========================================================
        // Lógica de Stamina
        // ==========================================================
        public void TickStamina(float deltaTime, bool isSprinting)
        {
            if (IsDead) return;

            if (isSprinting)
            {
                IsDrainActive = true;
                lastSprintTime = Time.time;

                currentStamina -= sprintDrainRate * deltaTime;
                if (currentStamina <= 0f) currentStamina = 0f;
            }
            else
            {
                IsDrainActive = false;

                if (Time.time >= lastSprintTime + staminaRegenDelay && currentStamina < maxStamina)
                {
                    currentStamina += staminaRegenRate * deltaTime;
                    if (currentStamina > maxStamina) currentStamina = maxStamina;
                }
            }

            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        public bool HasStaminaForDash() => currentStamina >= dashStaminaCost;

        public void ConsumeStamina(float amount)
        {
            currentStamina -= amount;
            if (currentStamina < 0f) currentStamina = 0f;
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }

        // ==========================================================
        // Lógica de Dano, Cura e Vida
        // ==========================================================

        // Para o PlayerDebugController que chama sem direção
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, Vector2.zero);
        }

        public void TakeDamage(float amount, Vector2 knockbackDirection)
        {
            if (IsDead) return;

            currentHealth -= amount;

            // Toca a animação de "Hurt" e pisca o sprite de vermelho
            visualController?.TriggerHurt();

            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;

            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDied?.Invoke();
        }

        public void ResetForRevive(float healthOverride = -1f)
        {
            IsDead = false;

            currentHealth = healthOverride >= 0f ? healthOverride : maxHealth;
            currentStamina = maxStamina;

            // Após revive, a regeneração pode começar imediatamente.
            lastSprintTime = Time.time - staminaRegenDelay;

            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnStaminaChanged?.Invoke(currentStamina, maxStamina);
        }
    }
}