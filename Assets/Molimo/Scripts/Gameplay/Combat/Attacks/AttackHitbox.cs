using System.Collections.Generic;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class AttackHitbox : MonoBehaviour
    {
        [Header("Configuração da Hitbox")]
        [SerializeField] private Vector2 hitboxSize = new Vector2(1f, 1f);
        [SerializeField] private float hitboxOffset = 1f;
        [SerializeField] private LayerMask hitLayers = ~0;

        [Header("Dano e Reações (IAttackReceiver)")]
        [Tooltip("Dano base para hits normais (Attack1 / Attack2).")]
        [SerializeField] private float baseDamage = 10f;

        [Tooltip("Dano para HeavyGlue (hits 1 e 2 do HeavyFinish). Geralmente 0, pois só cola.")]
        [SerializeField] private float heavyGlueDamage = 0f;

        [Tooltip("Dano para HeavyKnockback (hit 3 do HeavyFinish).")]
        [SerializeField] private float heavyKnockbackDamage = 20f;

        [Tooltip("Duração do efeito de 'cola' aplicado nos hits 1 e 2 do HeavyFinish.")]
        [SerializeField] private float glueDuration = 0.4f;

        [Tooltip("Força de knockback aplicado no hit 3 do HeavyFinish.")]
        [SerializeField] private float knockbackForce = 8f;

        [Tooltip("Duração do estado de knockback.")]
        [SerializeField] private float knockbackDuration = 0.25f;

        // Buffers de deduplicação — evitam hits múltiplos no mesmo alvo
        // (ex.: inimigo com vários colliders filhos).
        private readonly HashSet<IAttackReceiver> _hitReceivers = new();
        private readonly HashSet<HitTargetTest> _hitTargets = new();

        // ─────────────────────────────────────────────────────────────
        // API pública
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Executa o hit test na direção indicada.
        /// Chamado pelo PlayerCombat.DeliverHit(hitType).
        /// Compatível com o comportamento antigo (HitTargetTest) e com
        /// o novo sistema baseado em IAttackReceiver.
        /// </summary>
        public void PerformHitTest(Vector2 direction, AttackHitType hitType = AttackHitType.Normal)
        {
            if (direction == Vector2.zero) direction = Vector2.down;

            Vector3 hitboxCenter = transform.position + (Vector3)(direction.normalized * hitboxOffset);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            Collider2D[] hits = Physics2D.OverlapBoxAll(hitboxCenter, hitboxSize, angle, hitLayers);

            _hitReceivers.Clear();
            _hitTargets.Clear();

            foreach (var hit in hits)
            {
                // ─── 1) Efeito de impacto em superfícies (mantido como no original) ───
                if (hit.TryGetComponent(out SurfaceImpact surface))
                {
                    Vector3 impactPoint = hit.ClosestPoint(hitboxCenter);
                    surface.PlayImpact(impactPoint, direction);
                }

                // ─── 2) Prioridade: IAttackReceiver (inimigos migrados) ───
                IAttackReceiver receiver = ResolveReceiver(hit);
                if (receiver != null)
                {
                    if (_hitReceivers.Add(receiver))
                        ApplyHit(receiver, direction, hitType);
                    continue; // Não processar HitTargetTest no mesmo alvo
                }

                // ─── 3) Fallback: HitTargetTest (legacy / testes) ───
                if (hit.TryGetComponent(out HitTargetTest target))
                {
                    if (_hitTargets.Add(target))
                        target.TakeHit();
                }
            }
        }

        // ─────────────────────────────────────────────────────────────
        // Internos
        // ─────────────────────────────────────────────────────────────

        private static IAttackReceiver ResolveReceiver(Collider2D collider)
        {
            if (collider.TryGetComponent(out IAttackReceiver self)) return self;
            return collider.GetComponentInParent<IAttackReceiver>();
        }

        private void ApplyHit(IAttackReceiver target, Vector2 direction, AttackHitType hitType)
        {
            Vector2 dir = direction.normalized;

            float damage = hitType switch
            {
                AttackHitType.HeavyGlue      => heavyGlueDamage,
                AttackHitType.HeavyKnockback => heavyKnockbackDamage,
                _                            => baseDamage
            };

            switch (hitType)
            {
                case AttackHitType.HeavyGlue:
                    target.ApplyGlue(glueDuration, transform.position);
                    break;

                case AttackHitType.HeavyKnockback:
                    target.ApplyKnockback(dir * knockbackForce, knockbackDuration);
                    break;

                // Normal: sem reação extra, só dano
            }

            target.ApplyDamage(damage, transform.position, hitType);
        }

        // ─────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            // Usa a direção atual do PlayerMovement se existir (para o Gizmo refletir o ataque real)
            Vector2 dir = TryGetComponent(out PlayerMovement movement)
                ? movement.FacingDirection
                : Vector2.down;

            Vector3 center = transform.position + (Vector3)(dir.normalized * hitboxOffset);
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), Vector3.one);
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
            Gizmos.matrix = oldMatrix;
        }
    }
}