namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Contrato universal para qualquer entidade que possa receber dano.
    /// Inimigos implementam esta interface para que MolimoCombat aplique dano
    /// sem depender de tipos concretos.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }
        void TakeDamage(float amount);
    }
}