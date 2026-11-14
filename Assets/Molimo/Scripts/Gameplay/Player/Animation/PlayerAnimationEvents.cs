using UnityEngine;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Vive nos GameObjects filhos (Player_NonWeapon / Player_Weapon).
    /// Reencaminha Animation Events para os sistemas no pai.
    /// </summary>
    public class PlayerAnimationEvents : MonoBehaviour
    {
        private PlayerSoundController soundController;
        private PlayerCombat combat;

        private void Awake()
        {
            soundController = GetComponentInParent<PlayerSoundController>();
            combat = GetComponentInParent<PlayerCombat>();
        }

        // --- Áudio ---

        public void PlaySoundCategory(string categoryName)
            => soundController?.PlaySoundCategory(categoryName);

        public void Footstep()
            => soundController?.PlaySoundCategory("Step");

        // --- Combate ---

        /// <summary>
        /// Frame de contacto da arma → dispara o hitbox.
        /// Colocar no clímax do swing (meio do clip).
        /// </summary>
        public void ActivateHitbox()
            => combat?.NotifyAttackAnimationHit();

        /// <summary>
        /// Fim do clip → liberta AttackLock e encadeia combo se houver buffer.
        /// </summary>
        public void CancelAttackLock()
            => combat?.NotifyAttackAnimationEnd();
    }
}