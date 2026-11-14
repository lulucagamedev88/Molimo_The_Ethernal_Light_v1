namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Tipo de hit aplicado por um AttackHitbox. Permite que o mesmo hitbox
    /// sirva vários propósitos (dano puro, "colar" o inimigo, empurrar) sem
    /// depender do estado do step que o invocou.
    ///
    /// - Normal         → dano base, sem reação extra.
    /// - HeavyGlue      → aplica "glue" (prende o inimigo) + dano (geralmente 0).
    /// - HeavyKnockback → aplica knockback (empurra) + dano.
    /// </summary>
    public enum AttackHitType
    {
        Normal = 0,
        HeavyGlue = 1,
        HeavyKnockback = 2
    }
}