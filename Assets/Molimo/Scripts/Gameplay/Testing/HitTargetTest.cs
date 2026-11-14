using System.Collections;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class HitTargetTest : MonoBehaviour
    {
        [Header("Configuração do Feedback")]
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private float scaleAmount = 1.3f;
        [SerializeField] private Color hitColor = Color.red;

        private Vector3 _originalScale;
        private Color _originalColor;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _hitRoutine;

        private void Start()
        {
            _originalScale = transform.localScale;
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null) _originalColor = _spriteRenderer.color;
        }

        public void TakeHit()
        {
            if (_hitRoutine != null) StopCoroutine(_hitRoutine);
            _hitRoutine = StartCoroutine(HitOscillation());
        }

        private IEnumerator HitOscillation()
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float norm = elapsed / duration;

                // Efeito de "pulo" na escala (Oscilação)
                float scaleMod = 1f + Mathf.Sin(norm * Mathf.PI) * (scaleAmount - 1f);
                transform.localScale = _originalScale * scaleMod;

                // Efeito de piscar (vermelho)
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.color = (norm < 0.5f) ? hitColor : _originalColor;
                }

                yield return null;
            }

            // Reseta tudo ao normal
            transform.localScale = _originalScale;
            if (_spriteRenderer != null) _spriteRenderer.color = _originalColor;
            _hitRoutine = null;
        }
    }
}