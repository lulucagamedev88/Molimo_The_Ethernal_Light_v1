using System.Collections.Generic;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class PlayerSilhouetteController : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer[] targetRenderers;

        [Header("Silhouette")]
        [SerializeField] private Color silhouetteColor = Color.white;
        [SerializeField] private float fadeInDelay = 0.25f; // NOVO: Atraso antes de ficar branco
        [SerializeField, Range(0f, 30f)] private float fadeSpeed = 14f;
        [SerializeField] private bool useFade = true;

        [Header("Sorting Layers")]
        [SerializeField] private string normalLayer = "World";
        [SerializeField] private string silhouetteLayer = "Silhouette";

        private static readonly int SilhouetteColorId = Shader.PropertyToID("_SilhouetteColor");
        private static readonly int SilhouetteAmountId = Shader.PropertyToID("_SilhouetteAmount");

        private readonly HashSet<OccluderSilhouette> _activeOccluders = new();

        private float _currentAmount;
        private float _targetAmount;
        private float _lastApplied = -1f;
        private bool _isCurrentlySilhouette = false;

        // NOVO: Temporizador para o atraso
        private float _delayTimer = 0f;

        private void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<SpriteRenderer>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent(out OccluderSilhouette occ))
            {
                _activeOccluders.Add(occ);
                RecalculateTarget();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out OccluderSilhouette occ))
            {
                _activeOccluders.Remove(occ);
                RecalculateTarget();
            }
        }

        private void LateUpdate()
        {
            RecalculateTarget();

            // Se o player está atrás (Target = 1)
            if (_targetAmount > 0f)
            {
                _delayTimer += Time.deltaTime;

                // Só começa a fazer o Fade In após o tempo de atraso
                if (_delayTimer >= fadeInDelay)
                {
                    if (useFade)
                        _currentAmount = Mathf.MoveTowards(_currentAmount, _targetAmount, fadeSpeed * Time.deltaTime);
                    else
                        _currentAmount = _targetAmount;
                }
            }
            // Se o player está à frente (Target = 0)
            else
            {
                // Reseta o temporizador e força a saída imediata (Fade-Out Instantâneo)
                _delayTimer = 0f;
                _currentAmount = 0f;
            }

            ApplyToRenderers();
        }

        private void RecalculateTarget()
        {
            _targetAmount = 0f;
            float playerY = transform.position.y;

            foreach (var occ in _activeOccluders)
            {
                if (occ == null) continue;

                if (playerY > occ.SortY)
                {
                    _targetAmount = 1f;
                    return;
                }
            }
        }

        private void ApplyToRenderers()
        {
            if (Mathf.Approximately(_lastApplied, _currentAmount)) return;
            _lastApplied = _currentAmount;

            bool shouldBeSilhouette = _currentAmount > 0.01f;

            if (shouldBeSilhouette != _isCurrentlySilhouette)
            {
                _isCurrentlySilhouette = shouldBeSilhouette;
                string targetLayer = _isCurrentlySilhouette ? silhouetteLayer : normalLayer;

                foreach (var sr in targetRenderers)
                {
                    if (sr != null) sr.sortingLayerName = targetLayer;
                }
            }

            foreach (var sr in targetRenderers)
            {
                if (sr == null || sr.material == null) continue;

                sr.material.SetColor(SilhouetteColorId, silhouetteColor);
                sr.material.SetFloat(SilhouetteAmountId, _currentAmount);
            }
        }
    }
}