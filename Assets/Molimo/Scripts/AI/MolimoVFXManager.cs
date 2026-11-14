using UnityEngine;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Módulo de VFX do Molimo AI adaptado para 2D.
    /// O TrailRenderer e as partículas agora herdam a ordem de desenho (Sorting Layer) 
    /// do sprite do Molimo para não ficarem escondidos no cenário.
    /// </summary>
    public class MolimoVFXManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Trail Renderer (auto por movimento)")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private float trailMovementThreshold = 0.005f;
        [SerializeField, Range(0f, 0.5f)] private float trailLingerTime = 0.15f;

        [Header("VFX de Impacto")]
        [Tooltip("ParticleSystem local (filho do Molimo). Será clonado no impacto.")]
        [SerializeField] private ParticleSystem localImpactParticle;
        [SerializeField, Range(0f, 2f)] private float vfxDestroyPadding = 0.5f;

        [Header("Ordenação 2D")]
        [SerializeField] private SpriteRenderer molimoSprite;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        #endregion

        #region Internal State

        private Vector3 _lastPosition;
        private float _stillTimer;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
            if (molimoSprite == null) molimoSprite = GetComponent<SpriteRenderer>();

            if (trailRenderer != null)
            {
                trailRenderer.emitting = false;
                // Faz o trail respeitar a ordem de desenho do sprite (para não ficar atrás do chão)
                if (molimoSprite != null)
                {
                    trailRenderer.sortingLayerID = molimoSprite.sortingLayerID;
                    trailRenderer.sortingOrder = molimoSprite.sortingOrder - 1;
                }
            }

            if (localImpactParticle == null)
                localImpactParticle = GetComponentInChildren<ParticleSystem>(true);

            if (localImpactParticle != null)
                localImpactParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _lastPosition = transform.position;
        }

        private void Update()
        {
            UpdateTrailFromMovement();
        }

        #endregion

        #region Trail (auto por movimento)

        private void UpdateTrailFromMovement()
        {
            if (trailRenderer == null) return;

            Vector3 delta = transform.position - _lastPosition;
            _lastPosition = transform.position;

            bool isMoving = delta.sqrMagnitude > (trailMovementThreshold * trailMovementThreshold);

            if (isMoving)
            {
                _stillTimer = 0f;
                if (!trailRenderer.emitting) trailRenderer.emitting = true;
            }
            else
            {
                _stillTimer += Time.deltaTime;
                if (trailRenderer.emitting && _stillTimer >= trailLingerTime)
                    trailRenderer.emitting = false;
            }
        }

        #endregion

        #region Impact VFX

        public void SpawnImpactVFX(Vector3 position, Quaternion rotation)
        {
            if (localImpactParticle == null) return;

            GameObject clone = Instantiate(localImpactParticle.gameObject, position, rotation);
            clone.name = "ImpactVFX_Temp";
            clone.SetActive(true);

            ParticleSystem clonePS = clone.GetComponent<ParticleSystem>();
            if (clonePS != null)
            {
                // Em 2D, precisamos forçar o Particle System a ficar na camada certa
                ParticleSystemRenderer psRenderer = clonePS.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null && molimoSprite != null)
                {
                    psRenderer.sortingLayerID = molimoSprite.sortingLayerID;
                    psRenderer.sortingOrder = molimoSprite.sortingOrder + 1; // Na frente do molimo
                }

                clonePS.Play(true);

                float life = clonePS.main.duration + clonePS.main.startLifetime.constantMax + vfxDestroyPadding;
                Destroy(clone, life);
            }
            else
            {
                Destroy(clone, vfxDestroyPadding);
            }
        }

        #endregion
    }
}