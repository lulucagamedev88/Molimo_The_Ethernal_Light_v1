using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Ghost trail disparado pelo Dash e pelos ataques (Attack2 / HeavyFinish).
    /// Instancia cópias do sprite atual do player e faz fade-out via SpriteRenderer.color
    /// (vertex color) — funciona em qualquer sprite shader que leia IN.color.
    /// Contagem de ghosts por dash: 4 (mínimo) a 15 (máximo — limite de memória/performance).
    /// O delay entre spawns é derivado: duration / ghostCount.
    /// NÃO copia flip — o clipe de Dash cobre as 8 direções via sprite swap.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class GhostEffect : MonoBehaviour
    {
        // Constantes de clamp — única fonte da verdade.
        private const int MinGhostCount = 4;
        private const int MaxGhostCount = 15;

        [Header("Prefab & Container")]
        [Tooltip("Prefab do ghost. Deve ter SpriteRenderer com material Molimo/GhostSilhouette.")]
        [SerializeField] private GameObject ghostPrefab;

        [Tooltip("Opcional: pai onde os ghosts são adicionados. Se vazio, ficam soltos na cena.")]
        [SerializeField] private Transform ghostContainer;

        [Header("Ghost Count")]
        [Tooltip("Quantidade de ghosts gerados por Dash. " +
                 "Mín 4 (trail perceptível), máx 15 (limite de performance/memória). " +
                 "O delay entre ghosts é calculado como duration / ghostCount.")]
        [SerializeField, Range(4, 15)] private int ghostCount = 8;

        [Header("Timing")]
        [Tooltip("Duração do fade-out de cada ghost, em segundos. " +
                 "Recomendado: >= dashDuration para o trail continuar após o dash.")]
        [SerializeField, Min(0.01f)] private float ghostLifetime = 0.25f;

        [Header("Alpha & Tint")]
        [Tooltip("Alpha inicial do ghost (0-1). 0.4 = trail visível sem cobrir o player.")]
        [SerializeField, Range(0f, 1f)] private float initialAlpha = 0.4f;

        [Tooltip("Alpha final do ghost (0-1).")]
        [SerializeField, Range(0f, 1f)] private float finalAlpha = 0f;

        [Tooltip("Cor sólida da silhueta. Aplicada via SpriteRenderer.color.")]
        [SerializeField] private Color ghostTint = Color.white;

        [Header("Sorting")]
        [Tooltip("Offset de sortingOrder em relação ao player. -1 = atrás.")]
        [SerializeField] private int sortingOrderOffset = -1;

        private SpriteRenderer playerSprite;
        private Coroutine ghostCoroutine;
        private readonly List<SpriteRenderer> activeGhosts = new();

        private void Awake()
        {
            playerSprite = GetComponent<SpriteRenderer>();
            // Garante clamp mesmo se o valor serializado foi corrompido.
            ghostCount = Mathf.Clamp(ghostCount, MinGhostCount, MaxGhostCount);
        }

        private void OnValidate()
        {
            // Clamp no editor ao arrastar o slider / digitar no Inspector.
            ghostCount = Mathf.Clamp(ghostCount, MinGhostCount, MaxGhostCount);
        }

        // ==========================================================
        // API pública
        // ==========================================================

        /// <summary>
        /// Inicia ghost trail usando os valores serializados no Inspector
        /// (comportamento original do Dash).
        /// </summary>
        public void StartGhostEffect(float duration)
        {
            StartGhostEffect(duration, ghostCount, initialAlpha, finalAlpha, ghostLifetime);
        }

        /// <summary>
        /// Inicia ghost trail com parâmetros por invocação (não altera os serializados).
        /// Útil para "rasto ligeiro" em ataques. Valores são clampados internamente.
        /// </summary>
        public void StartGhostEffect(float duration, int count, float startAlpha, float endAlpha, float lifetime)
        {
            if (ghostPrefab == null || playerSprite == null) return;

            if (ghostCoroutine != null)
            {
                StopCoroutine(ghostCoroutine);
                ghostCoroutine = null;
            }

            ghostCoroutine = StartCoroutine(CreateGhosts(duration, count, startAlpha, endAlpha, lifetime));
        }

        /// <summary>
        /// Para de spawnar novos ghosts. Se <paramref name="destroyExisting"/> = true,
        /// também destrói os ghosts já instanciados (use em morte/deactivate, para evitar
        /// órfãos opacos caso as coroutines de fade morram junto do player).
        /// </summary>
        public void StopGhostEffect(bool destroyExisting = false)
        {
            if (ghostCoroutine != null)
            {
                StopCoroutine(ghostCoroutine);
                ghostCoroutine = null;
            }

            if (!destroyExisting) return;

            for (int i = activeGhosts.Count - 1; i >= 0; i--)
            {
                var g = activeGhosts[i];
                if (g != null) Destroy(g.gameObject);
            }
            activeGhosts.Clear();
        }

        public void SetGhostLifetime(float time) => ghostLifetime = Mathf.Max(0.01f, time);

        public void SetGhostTransparency(float startAlpha, float endAlpha)
        {
            initialAlpha = Mathf.Clamp01(startAlpha);
            finalAlpha = Mathf.Clamp01(endAlpha);
        }

        /// <summary>
        /// Define a contagem de ghosts por dash. Clampado em [4, 15] — nunca lança exceção,
        /// apenas corrige o valor. Use este método em vez de setar o campo direto.
        /// </summary>
        public void SetGhostCount(int count)
        {
            ghostCount = Mathf.Clamp(count, MinGhostCount, MaxGhostCount);
        }

        // ==========================================================
        // Interno
        // ==========================================================
        private IEnumerator CreateGhosts(float duration, int count, float startAlpha, float endAlpha, float lifetime)
        {
            // 1 frame: garante que o Animator já aplicou o sprite atual
            // antes de capturarmos a primeira cópia.
            yield return null;

            count = Mathf.Clamp(count, MinGhostCount, MaxGhostCount);
            lifetime = Mathf.Max(0.01f, lifetime);
            startAlpha = Mathf.Clamp01(startAlpha);
            endAlpha = Mathf.Clamp01(endAlpha);

            // Delay derivado: cobre exatamente a duração do dash/ataque.
            // Guarda contra duration inválido (0 ou negativo).
            float delay = duration > 0f ? duration / count : 0.01f;
            if (delay <= 0f) delay = 0.01f;

            // Uma única alocação por invocação (não por ghost).
            WaitForSeconds wait = new WaitForSeconds(delay);

            for (int i = 0; i < count; i++)
            {
                CreateGhostInstance(startAlpha, endAlpha, lifetime);

                // Não espera após o último — economiza 1 delay no fim.
                if (i < count - 1)
                    yield return wait;
            }

            ghostCoroutine = null;
        }

        private void CreateGhostInstance(float startAlpha, float endAlpha, float lifetime)
        {
            if (ghostPrefab == null || playerSprite == null) return;

            GameObject ghost = Instantiate(ghostPrefab, transform.position, transform.rotation);

            if (ghostContainer != null)
                ghost.transform.SetParent(ghostContainer, true);

            var ghostSprite = ghost.GetComponent<SpriteRenderer>();
            if (ghostSprite == null)
            {
                Destroy(ghost);
                return;
            }

            // Copia aparência do player (sem flip — Dash clip cobre 8 direções).
            ghostSprite.sprite = playerSprite.sprite;
            ghostSprite.sortingLayerName = playerSprite.sortingLayerName;
            ghostSprite.sortingOrder = playerSprite.sortingOrder + sortingOrderOffset;
            ghost.transform.localScale = transform.localScale;

            SetGhostAlpha(ghostSprite, startAlpha);
            activeGhosts.Add(ghostSprite);
            StartCoroutine(FadeOutGhost(ghostSprite, startAlpha, endAlpha, lifetime));
        }

        private IEnumerator FadeOutGhost(SpriteRenderer sprite, float startAlpha, float endAlpha, float lifetime)
        {
            float elapsed = 0f;
            while (elapsed < lifetime && sprite != null)
            {
                float t = Mathf.Clamp01(elapsed / lifetime);
                SetGhostAlpha(sprite, Mathf.Lerp(startAlpha, endAlpha, t));

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (sprite != null)
            {
                activeGhosts.Remove(sprite);
                Destroy(sprite.gameObject);
            }
        }

        /// <summary>
        /// Aplica cor + alpha via SpriteRenderer.color (vertex color).
        /// Compatível com qualquer sprite shader que leia IN.color.
        /// </summary>
        private void SetGhostAlpha(SpriteRenderer sprite, float alpha)
        {
            if (sprite == null) return;

            Color c = ghostTint;
            c.a = alpha;
            sprite.color = c;
        }
    }
}