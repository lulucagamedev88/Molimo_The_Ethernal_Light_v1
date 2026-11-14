using UnityEngine;
using UnityEngine.InputSystem;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Ferramenta de DEBUG para testar o sistema de saúde do jogador.
    /// Teclas:
    ///   K → mata o jogador instantaneamente.
    ///   L → aplica dano percentual (padrão 10% do HP máximo).
    ///   R → reseta a vida (e a stamina) para o máximo.
    /// Use APENAS em cenas de teste — remova do build final.
    /// </summary>
    public class PlayerDebugController : MonoBehaviour
    {
        [Header("Referência")]
        [Tooltip("Deixe vazio para localizar o PlayerHealth na cena automaticamente.")]
        [SerializeField] private PlayerHealth health;

        [Header("Dano Percentual (tecla L)")]
        [Tooltip("Percentual do HP máximo removido a cada tecla L.")]
        [SerializeField, Range(1f, 100f)] private float damagePercent = 10f;

        private void Awake()
        {
            if (health == null)
                health = FindAnyObjectByType<PlayerHealth>();

            if (health == null)
                Debug.LogError("[PlayerDebugController] PlayerHealth não encontrado na cena.");
        }

        private void Update()
        {
            if (health == null || Keyboard.current == null) return;

            if (Keyboard.current.kKey.wasPressedThisFrame) Kill();
            if (Keyboard.current.lKey.wasPressedThisFrame) DamagePercent();
            if (Keyboard.current.rKey.wasPressedThisFrame) ResetHealth();
        }

        private void Kill()
        {
            Debug.Log("[DEBUG] K — matando o jogador.");
            health.TakeDamage(health.MaxHealth);
        }

        private void DamagePercent()
        {
            float amount = health.MaxHealth * (damagePercent / 100f);
            Debug.Log($"[DEBUG] L — dano de {damagePercent}% ({amount} HP).");
            health.TakeDamage(amount);
        }

        private void ResetHealth()
        {
            Debug.Log("[DEBUG] R — resetando HP/Stamina.");
            var brain = health.GetComponent<PlayerBrain>();
            if (brain != null && !brain.IsActive)
                brain.Revive();             // personagem morto: revive por completo
            else
                health.ResetForRevive();    // vivo: apenas restaura stats
        }
    }
}