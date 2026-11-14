using UnityEngine;
using UnityEngine.UI;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// HUD do jogador: controla as barras de Vida e Stamina.
    /// Arquitetura event-driven: escuta PlayerHealth.OnHealthChanged / OnStaminaChanged,
    /// guarda o valor-alvo e suaviza a animação da barra no Update.
    /// </summary>
    public class PlayerStatsHUDController : MonoBehaviour
    {
        [Header("Referência do Jogador")]
        [Tooltip("Deixe vazio para localizar PlayerHealth na cena automaticamente.")]
        [SerializeField] private PlayerHealth health;

        [Header("Barras")]
        [SerializeField] private Image healthFillImage;
        [SerializeField] private Image staminaFillImage;

        [Header("Animação das Barras")]
        [Tooltip("Velocidade de preenchimento/esvaziamento (0–1 por segundo). Ex: 2 = 50% por segundo.")]
        [SerializeField, Min(0f)] private float barChangeSpeed = 2f;

        [Tooltip("Se marcado, o dreno (perda) é instantâneo — só a regeneração é suave.")]
        [SerializeField] private bool instantDrain = true;

        // Valores-alvo (atualizados pelos eventos; suavizados no Update).
        private float targetHealth = 1f;
        private float targetStamina = 1f;

        private void Awake()
        {
            if (health == null) health = FindAnyObjectByType<PlayerHealth>();
            if (health == null)
            {
                Debug.LogError("[PlayerStatsHUDController] PlayerHealth não encontrado.", this);
                enabled = false;
                return;
            }

            health.OnHealthChanged += HandleHealthChanged;
            health.OnStaminaChanged += HandleStaminaChanged;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnHealthChanged -= HandleHealthChanged;
                health.OnStaminaChanged -= HandleStaminaChanged;
            }
        }

        private void Start() => SnapBars();

        private void Update()
        {
            if (healthFillImage != null)
                healthFillImage.fillAmount = Smooth(healthFillImage.fillAmount, targetHealth);
            if (staminaFillImage != null)
                staminaFillImage.fillAmount = Smooth(staminaFillImage.fillAmount, targetStamina);
        }

        // ==========================================================
        // Handlers de eventos
        // ==========================================================
        private void HandleHealthChanged(float current, float max)
            => targetHealth = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        private void HandleStaminaChanged(float current, float max)
            => targetStamina = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // ==========================================================
        // Suavização
        // ==========================================================
        private float Smooth(float current, float target)
        {
            if (instantDrain && target < current) return target; // dreno imediato
            return Mathf.MoveTowards(current, target, barChangeSpeed * Time.deltaTime);
        }

        /// <summary>Força as barras para o valor atual (uso no carregamento da cena).</summary>
        public void SnapBars()
        {
            if (health == null) return;

            targetHealth = health.MaxHealth > 0f ? Mathf.Clamp01(health.CurrentHealth / health.MaxHealth) : 0f;
            targetStamina = health.MaxStamina > 0f ? Mathf.Clamp01(health.CurrentStamina / health.MaxStamina) : 0f;

            if (healthFillImage != null) healthFillImage.fillAmount = targetHealth;
            if (staminaFillImage != null) staminaFillImage.fillAmount = targetStamina;
        }
    }
}