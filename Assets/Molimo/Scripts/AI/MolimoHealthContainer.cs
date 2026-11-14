using System;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Cesta de HP do Molimo AI adaptada para 2D.
    /// Deteta itens de vida por perto usando Collider2D e delega a movimentação ao próprio item.
    /// </summary>
    public class MolimoHealthContainer : MonoBehaviour
    {
        #region Events

        public static event Action<int, int> OnMolimoHPChanged;
        public static event Action<EnergyTier> OnEnergyTierRaised;
        public event Action<int> OnItemCollected;

        public enum EnergyTier
        {
            Low = 0,        // 0    – 499
            Medium = 1,     // 500  – 999
            Powerful = 2    // 1000
        }

        public void RaiseHPChanged()
        {
            OnMolimoHPChanged?.Invoke(currentHP, maxHP);
        }

        #endregion

        #region Inspector Fields

        [Header("Cesta de HP")]
        [SerializeField, Range(1, 1000)] private int maxHP = 1000;
        [SerializeField, Range(0, 1000)] private int currentHP = 0;

        [Header("Níveis de Energia (Alertas HUD)")]
        [SerializeField, Range(1, 1000)] private int mediumEnergyThreshold = 500;
        [SerializeField, Range(1, 1000)] private int powerfulEnergyThreshold = 1000;

        [Header("Atração de Itens de Vida (2D)")]
        [Tooltip("Layer dos itens coletáveis de vida.")]
        [SerializeField] private LayerMask itemLayer = ~0;

        [Tooltip("Raio de atração em volta do Molimo.")]
        [SerializeField, Range(0.5f, 30f)] private float attractionRadius = 5f;

        [Tooltip("Offset do centro de atração em relação ao Molimo (X, Y).")]
        [SerializeField] private Vector2 centerOffset = Vector2.zero;

        [Tooltip("Velocidade com que o item é puxado.")]
        [SerializeField] private float pullSpeed = 8f;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;
        [SerializeField] private Color gizmoColor = new Color(0f, 1f, 0.5f, 0.3f);

        #endregion

        #region Public Properties

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
        public bool HasHP => currentHP > 0;
        public bool IsFull => currentHP >= maxHP;
        public float AttractionRadius => attractionRadius;

        #endregion

        #region Internal State

        // 2D: Collider2D em vez de Collider
        private readonly Collider2D[] _overlapResults = new Collider2D[20];

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            RaiseHPChanged();
        }

        private void Update()
        {
            if (IsFull) return;
            DetectAndAttractItems();
        }

        #endregion

        #region Item Detection (2D)

        private void DetectAndAttractItems()
        {
            Vector2 center = (Vector2)transform.position + centerOffset;

            // Física 2D: OverlapCircleNonAlloc
            int count = Physics2D.OverlapCircleNonAlloc(
                center, attractionRadius, _overlapResults, itemLayer);

            for (int i = 0; i < count; i++)
            {
                Collider2D col = _overlapResults[i];
                if (col == null) continue;

                MolimoCollectableHP item = col.GetComponentInParent<MolimoCollectableHP>();
                if (item == null) continue;
                if (item.IsAttracted) continue;

                item.AttractTo(transform, pullSpeed);
                Log($"Item atraído: {item.name}");
            }
        }

        #endregion

        #region Energy Tier API

        public EnergyTier GetCurrentTier()
        {
            if (currentHP >= powerfulEnergyThreshold) return EnergyTier.Powerful;
            if (currentHP >= mediumEnergyThreshold) return EnergyTier.Medium;
            return EnergyTier.Low;
        }

        private void FireTierRaisedIfNeeded(EnergyTier before)
        {
            EnergyTier after = GetCurrentTier();
            if (after <= before) return;

            if (before < EnergyTier.Medium && after >= EnergyTier.Medium)
                OnEnergyTierRaised?.Invoke(EnergyTier.Medium);

            if (before < EnergyTier.Powerful && after >= EnergyTier.Powerful)
                OnEnergyTierRaised?.Invoke(EnergyTier.Powerful);
        }

        #endregion

        #region Basket API

        public bool TryAddHP(int amount)
        {
            if (amount <= 0) return false;
            if (IsFull) return false;

            EnergyTier tierBefore = GetCurrentTier();

            int before = currentHP;
            currentHP = Mathf.Clamp(currentHP + amount, 0, maxHP);
            int added = currentHP - before;

            if (added > 0)
            {
                OnMolimoHPChanged?.Invoke(currentHP, maxHP);
                OnItemCollected?.Invoke(added);
                FireTierRaisedIfNeeded(tierBefore);
                Log($"HP adicionado: +{added}. Cesta: {currentHP}/{maxHP} | Tier: {GetCurrentTier()}");
            }

            return added > 0;
        }

        public bool TryConsumeHP(int amount)
        {
            if (amount <= 0) return false;
            if (currentHP < amount) return false;

            currentHP -= amount;
            OnMolimoHPChanged?.Invoke(currentHP, maxHP);
            Log($"HP consumido: -{amount}. Cesta: {currentHP}/{maxHP}");

            return true;
        }

        #endregion

        #region Helpers

        private void Log(string msg)
        {
            if (debugLogs) Debug.Log($"[MolimoHealthContainer] {msg}");
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 center = (Vector2)transform.position + centerOffset;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(center, attractionRadius);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.1f);
            Gizmos.DrawSphere(center, attractionRadius);
        }

        #endregion

        #region Static Reset

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticEvents()
        {
            OnMolimoHPChanged = null;
            OnEnergyTierRaised = null;
        }

        #endregion
    }
}