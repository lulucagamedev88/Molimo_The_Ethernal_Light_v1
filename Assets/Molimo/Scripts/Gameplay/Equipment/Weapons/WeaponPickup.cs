using UnityEngine;
using UnityEngine.InputSystem;

namespace MolimoByLulucaGameDev
{
    public enum PickupType { Sword, Shield }

    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class WeaponPickup : MonoBehaviour
    {
        [Header("Configuração")]
        [SerializeField] private PickupType pickupType = PickupType.Sword;

        [Header("Física do Drop")]
        [Tooltip("Força que a arma é empurrada para longe do Player.")]
        [SerializeField] private float dropForce = 5f;

        [Tooltip("Atrito para ela parar de deslizar no chão (Top-Down).")]
        [SerializeField] private float friction = 5f;

        private WeaponEquip weaponEquip;
        private bool canPickup;
        private InputSystem_Actions inputActions;

        private Rigidbody2D rb;
        private Collider2D col;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            // Configuração inicial (A flutuar no chão, pronta para ser apanhada)
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            col.isTrigger = true;
        }

        private void OnEnable()
        {
            if (inputActions != null) inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            if (inputActions != null) inputActions.Player.Disable();
        }

        private void Update()
        {
            if (!canPickup) return;

            // Verifica input do Gamepad (Botão North / Y / Triângulo)
            bool isGamepadNorthPressed = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;

            // Verifica input do Teclado (Tecla E)
            bool isInteractPressed = inputActions != null && inputActions.Player.Interact.WasPressedThisFrame();

            if (isGamepadNorthPressed || isInteractPressed)
                PerformPickup();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                weaponEquip = other.GetComponent<WeaponEquip>();
                if (weaponEquip != null) canPickup = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                canPickup = false;
                weaponEquip = null;
            }
        }

        private void PerformPickup()
        {
            if (weaponEquip == null) return;

            // 1. Equipa a arma no Player
            if (pickupType == PickupType.Sword) weaponEquip.EquipWeapon(this);
            else if (pickupType == PickupType.Shield) weaponEquip.EquipShield(this);

            // 2. Toca o som de apanhar a arma
            PlayerSoundController soundController = weaponEquip.GetComponent<PlayerSoundController>();
            soundController?.PlaySoundCategory("WeaponPickup");

            // 3. Esconde a arma do chão
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Chamado pelo WeaponEquip quando o Player leva dano severo e solta as armas.
        /// </summary>
        public void DropFromPlayer(Vector3 playerPosition, Vector2 throwDirection)
        {
            // 1. Tira a arma de dentro do player e coloca no mundo
            transform.SetParent(null);
            transform.position = playerPosition + (Vector3)(throwDirection * 0.5f);
            gameObject.SetActive(true);

            // 2. Configura a física Top-Down
            rb.isKinematic = false;
            rb.gravityScale = 0f; // ZERO GRAVIDADE! Evita que caia para o infinito no eixo Y
            rb.linearDamping = friction; // Atrito para deslizar e parar como se estivesse no chão
            col.isTrigger = false; // Para não atravessar paredes
            canPickup = false;

            // 3. Aplica a força de arremesso (apenas nos eixos X e Y)
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(throwDirection * dropForce, ForceMode2D.Impulse);

            // 4. Depois de 1.5s, a arma para de se mover e volta a ser coletável
            Invoke(nameof(MakePickupableAgain), 1.5f);
        }

        private void MakePickupableAgain()
        {
            if (gameObject.activeInHierarchy)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector2.zero;
                col.isTrigger = true;
            }
        }
    }
}