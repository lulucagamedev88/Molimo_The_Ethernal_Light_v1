using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class WeaponEquip : MonoBehaviour
    {
        [SerializeField] private PlayerVisualController visualController;

        public bool HasSword { get; private set; }
        public bool HasShield { get; private set; }

        // Propriedade útil para o Combate (Bater)
        public bool HasAnyWeapon => HasSword || HasShield;

        // Propriedade útil para a Defesa (Bloquear)
        public bool CanAttack => HasSword;
        public bool CanDefend => HasShield;

        private WeaponPickup _heldSword;
        private WeaponPickup _heldShield;

        private void Reset() => visualController = GetComponent<PlayerVisualController>();

        private void Awake()
        {
            if (visualController == null) visualController = GetComponent<PlayerVisualController>();
        }

        public void EquipWeapon(WeaponPickup swordPickup)
        {
            if (HasSword) return; // Impede apanhar duas espadas
            HasSword = true;
            _heldSword = swordPickup;
            Debug.Log("<color=green>[Player]</color> O Player SACOU a Espada! (Pode Atacar)");
            RefreshVisual();
        }

        public void EquipShield(WeaponPickup shieldPickup)
        {
            if (HasShield) return; // Impede apanhar dois escudos
            HasShield = true;
            _heldShield = shieldPickup;
            Debug.Log("<color=cyan>[Player]</color> O Player SACOU o Escudo! (Pode Defender)");
            RefreshVisual();
        }

        public void DropAllWeapons(Vector2 knockbackDirection)
        {
            if (HasSword && _heldSword != null)
            {
                HasSword = false;
                _heldSword.DropFromPlayer(transform.position, -knockbackDirection);
                _heldSword = null;
            }

            if (HasShield && _heldShield != null)
            {
                HasShield = false;
                _heldShield.DropFromPlayer(transform.position, -knockbackDirection);
                _heldShield = null;
            }

            RefreshVisual();
        }

        private void RefreshVisual()
        {
            // REGRA DE OURO: O visual Player_Weapon só ativa se tiver AMBAS as armas.
            bool hasCompleteArsenal = HasSword && HasShield;

            visualController?.SetHasWeapons(hasCompleteArsenal);
        }
    }
}