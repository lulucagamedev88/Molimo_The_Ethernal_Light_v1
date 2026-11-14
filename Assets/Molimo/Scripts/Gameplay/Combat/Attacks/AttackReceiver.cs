using UnityEngine;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Contrato para qualquer entidade que possa receber hits do AttackHitbox.
    /// Inimigos implementam esta interface para reagir a dano, glue e knockback
    /// sem depender de tipos concretos.
    ///
    /// O AttackHitbox chama estes métodos na seguinte ordem (por hit):
    ///   1. ApplyGlue (se HeavyGlue) OU ApplyKnockback (se HeavyKnockback)
    ///   2. ApplyDamage (sempre)
    /// </summary>
    public interface IAttackReceiver
    {
        /// <summary>
        /// Aplica dano. <paramref name="hitType"/> permite ao receiver
        /// distinguir entre hits normais e hits especiais (ex: som diferente).
        /// </summary>
        void ApplyDamage(float amount, Vector3 sourcePosition, AttackHitType hitType = AttackHitType.Normal);

        /// <summary>
        /// Prende o receiver no lugar por <paramref name="duration"/> segundos.
        /// Chamado nos hits 1 e 2 do HeavyFinish.
        /// </summary>
        void ApplyGlue(float duration, Vector3 sourcePosition);

        /// <summary>
        /// Empurra o receiver com <paramref name="force"/> na direção indicada,
        /// pelo tempo de <paramref name="duration"/> segundos.
        /// Chamado no hit 3 do HeavyFinish.
        /// </summary>
        void ApplyKnockback(Vector2 force, float duration);
    }
}