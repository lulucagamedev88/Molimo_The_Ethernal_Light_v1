using System;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Receiver de ataques para enemies com Rigidbody2D Dynamic (top-down).
    /// - Glue: para o enemy instantaneamente e bloqueia a IA de se mexer.
    /// - Knockback: aplica velocidade; a LinearDamping do Rigidbody2D
    ///   encarrega-se da desaceleração natural.
    /// - Emite OnDamaged (para VFX de hit) e OnDied (para lógica de morte).
    /// Requer: Rigidbody2D Dynamic (gravityScale=0, freezeRotation, damping alto),
    ///         Collider2D (sólido), e ser detetável pela LayerMask do AttackHitbox.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAttackReceiver : MonoBehaviour, IAttackReceiver
    {
        [Header("Vida")]
        [SerializeField] private float maxHealth = 50f;

        [Header("Glue (hits 1 e 2 do HeavyFinish)")]
        [Tooltip("Se true, enquanto 'glued', a IA (Enemy) não deve mover o enemy.")]
        [SerializeField] private bool blocksMovementWhileGlued = true;

        [Header("Knockback (hit 3 do HeavyFinish)")]
        [Tooltip("Multiplicador aplicado sobre a força recebida (1 = exato).")]
        [SerializeField, Min(0f)] private float knockbackMultiplier = 1f;

        [Header("Morte")]
        [SerializeField] private bool destroyOnDeath = true;
        [SerializeField, Min(0f)] private float destroyDelay = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        // ── Eventos ──
        /// <summary>Dispara SEMPRE que um hit chega (mesmo com damage=0, ex: HeavyGlue).</summary>
        public event Action<float, AttackHitType> OnDamaged;
        public event Action OnDied;

        // ── Estado ──
        private Rigidbody2D rb;
        private float currentHealth;
        private float glueTimer;
        private float knockbackTimer;

        // ── Propriedades ──
        public bool IsAlive => currentHealth > 0f;
        public bool IsGlued => glueTimer > 0f;
        public bool IsBeingKnockedBack => knockbackTimer > 0f;
        public bool BlocksMovement => blocksMovementWhileGlued && IsGlued;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            currentHealth = maxHealth;

            if (rb.bodyType == RigidbodyType2D.Static)
                Debug.LogWarning($"[EnemyAttackReceiver] '{name}' tem Rigidbody2D Static. Knockback não vai funcionar.", this);
        }

        private void FixedUpdate()
        {
            if (glueTimer > 0f)
            {
                glueTimer -= Time.fixedDeltaTime;
                rb.linearVelocity = Vector2.zero;
            }

            if (knockbackTimer > 0f)
                knockbackTimer -= Time.fixedDeltaTime;
        }

        // ==========================================================
        // IAttackReceiver
        // ==========================================================

        public void ApplyDamage(float amount, Vector3 sourcePosition, AttackHitType hitType = AttackHitType.Normal)
        {
            if (!IsAlive) return;

            // Notifica sempre (mesmo com damage=0) → permite feedback visual em HeavyGlue.
            OnDamaged?.Invoke(amount, hitType);

            if (amount <= 0f) return;

            currentHealth -= amount;
            Log($"Dano {amount} ({hitType}). HP: {currentHealth}/{maxHealth}");

            if (currentHealth <= 0f)
                Die();
        }

        public void ApplyGlue(float duration, Vector3 sourcePosition)
        {
            if (!IsAlive) return;
            glueTimer = Mathf.Max(glueTimer, duration);
            rb.linearVelocity = Vector2.zero;
            Log($"Glue por {duration}s.");
        }

        public void ApplyKnockback(Vector2 force, float duration)
        {
            if (!IsAlive) return;

            // Knockback cancela glue.
            glueTimer = 0f;
            knockbackTimer = duration;
            rb.linearVelocity = force * knockbackMultiplier;
            Log($"Knockback força {force.magnitude:F2} por {duration}s.");
        }

        // ==========================================================
        // Morte
        // ==========================================================

        private void Die()
        {
            Log("Morto.");
            OnDied?.Invoke();

            foreach (var c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            rb.linearVelocity = Vector2.zero;
            enabled = false;

            if (destroyOnDeath) Destroy(gameObject, destroyDelay);
        }

        private void Log(string msg)
        {
            if (debugLogs) Debug.Log($"[Enemy:{name}] {msg}");
        }
    }
}