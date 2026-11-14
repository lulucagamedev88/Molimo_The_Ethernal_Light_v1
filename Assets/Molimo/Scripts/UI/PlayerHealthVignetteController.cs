using UnityEngine;
using UnityEngine.UI;

namespace MolimoByLulucaGameDev
{
    [System.Serializable]
    public class VignetteLayer
    {
        [Header("Image")]
        public Image image;

        [Header("Visibilidade")]
        [Range(0f, 1f)] public float startHealth = 0.4f;
        public bool useFadeIn = true;

        [Header("Buraco")]
        public AnimationCurve holeSizeByHealth = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Range(0.01f, 0.5f)] public float holeFeather = 0.08f;

        [Header("Pulso do Buraco")]
        public bool pulseHoleWithBeat = true;
        [Range(0f, 0.3f)] public float holePulseAmount = 0.05f;

        [Header("Pulso (Alpha)")]
        [Tooltip("Opacidade no PICO do batimento (Tum). Deixe 0 para a vinheta DESAPARECER no Tum.")]
        [Range(0f, 1f)] public float minPulseAlpha = 0f;

        [HideInInspector] public Material runtimeMaterial;
    }

    [ExecuteAlways]
    public class PlayerHealthVignetteController : MonoBehaviour
    {
        [Header("Referência do Jogador")]
        [SerializeField] private PlayerHealth health;

        [Header("Vinheta de Veias (Surgimento ~40%)")]
        [SerializeField]
        private VignetteLayer veinsLayer = new VignetteLayer
        {
            startHealth = 0.4f,
            useFadeIn = true,
            holeFeather = 0.08f,
            minPulseAlpha = 0f // Padrão atualizado para desaparecer no Tum
        };

        [Header("Vinheta Sem Veias (Surgimento ~30%)")]
        [SerializeField]
        private VignetteLayer plainLayer = new VignetteLayer
        {
            startHealth = 0.3f,
            useFadeIn = true,
            holeFeather = 0.10f,
            minPulseAlpha = 0f // Padrão atualizado para desaparecer no Tum
        };

        [Header("Pulso Cardíaco (Compartilhado)")]
        [SerializeField]
        private AnimationCurve bpmByHealth = new AnimationCurve(
            new Keyframe(0.00f, 30f),
            new Keyframe(0.05f, 180f),
            new Keyframe(0.20f, 140f),
            new Keyframe(0.50f, 90f),
            new Keyframe(1.00f, 65f)
        );

        [SerializeField, Range(0f, 1f)] private float dubIntensity = 0.65f;

        [Header("Som do Pulso (Heartbeat)")]
        [SerializeField] private bool enablePulseSound = true;
        [SerializeField] private SoundData pulseSound;
        [SerializeField] private SoundData dubSound;
        [SerializeField, Range(0f, 1f)] private float pulseSoundStartHealth = 0.4f;
        [SerializeField] private AudioSource pulseAudioSource;

        private static readonly int HoleRadiusID = Shader.PropertyToID("_HoleRadius");
        private static readonly int FeatherID = Shader.PropertyToID("_Feather");
        private static readonly int HoleAspectID = Shader.PropertyToID("_HoleAspect");
        private static readonly int PulseIntensityID = Shader.PropertyToID("_PulseIntensity");

        private const float LUB_CYCLE = 0.15f;
        private const float DUB_CYCLE = 0.35f;

        private float healthRatio = 1f;
        private float pulsePhase;
        private float previousCycle;
        private bool lubFiredThisCycle;
        private bool dubFiredThisCycle;

        private void Awake()
        {
            if (!Application.isPlaying) return;

            SetLayerImagesEnabled(true);

            if (health == null) health = FindAnyObjectByType<PlayerHealth>();
            if (health == null)
            {
                Debug.LogError("[PlayerHealthVignette] PlayerHealth não encontrado.", this);
                enabled = false;
                return;
            }

            SetupLayer(veinsLayer);
            SetupLayer(plainLayer);
            SetupAudioSource();

            healthRatio = health.MaxHealth > 0f ? health.CurrentHealth / health.MaxHealth : 1f;

            health.OnHealthChanged += HandleHealthChanged;
            health.OnDied += HandleDied;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) SetLayerImagesEnabled(false);
#endif
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            ApplyLayer(veinsLayer, beat: 0f);
            ApplyLayer(plainLayer, beat: 0f);
        }

        private void OnDestroy()
        {
            if (!Application.isPlaying) return;
            if (health != null)
            {
                health.OnHealthChanged -= HandleHealthChanged;
                health.OnDied -= HandleDied;
            }
            DestroyLayerMaterial(veinsLayer);
            DestroyLayerMaterial(plainLayer);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            float bpm = Mathf.Max(1f, bpmByHealth.Evaluate(healthRatio));
            pulsePhase += Time.deltaTime * (bpm / 60f);

            float beat = ComputeHeartbeat(pulsePhase);
            ApplyLayer(veinsLayer, beat);
            ApplyLayer(plainLayer, beat);

            float currentCycle = pulsePhase % 1f;
            DetectAndPlayPulseSound(currentCycle);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying) SetLayerImagesEnabled(false);
        }
#endif

        private void HandleHealthChanged(float current, float max)
            => healthRatio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        private void HandleDied()
        {
            pulsePhase = 0f;
            previousCycle = 0f;
            lubFiredThisCycle = false;
            dubFiredThisCycle = false;
        }

        private void DetectAndPlayPulseSound(float currentCycle)
        {
            if (!enablePulseSound || healthRatio >= pulseSoundStartHealth)
            {
                previousCycle = currentCycle;
                return;
            }

            bool wrapped = currentCycle < previousCycle;

            if (wrapped)
            {
                lubFiredThisCycle = false;
                dubFiredThisCycle = false;
            }

            if (!lubFiredThisCycle && CrossedThreshold(previousCycle, currentCycle, LUB_CYCLE, wrapped))
            {
                PlayPulseSound(isDub: false);
                lubFiredThisCycle = true;
            }

            if (!dubFiredThisCycle && dubSound != null && CrossedThreshold(previousCycle, currentCycle, DUB_CYCLE, wrapped))
            {
                PlayPulseSound(isDub: true);
                dubFiredThisCycle = true;
            }

            previousCycle = currentCycle;
        }

        private static bool CrossedThreshold(float prev, float curr, float threshold, bool wrapped)
        {
            return wrapped
                ? (threshold > prev || threshold <= curr)
                : (prev < threshold && threshold <= curr);
        }

        public void PlayPulseSound(bool isDub = false)
        {
            SoundData sound = isDub ? dubSound : pulseSound;
            if (sound == null || sound.GetClip() == null || pulseAudioSource == null) return;

            if (sound.mixerGroup != null && pulseAudioSource.outputAudioMixerGroup != sound.mixerGroup)
                pulseAudioSource.outputAudioMixerGroup = sound.mixerGroup;

            pulseAudioSource.spatialBlend = sound.spatialBlend;
            pulseAudioSource.priority = sound.priority;
            pulseAudioSource.minDistance = sound.minDistance;
            pulseAudioSource.maxDistance = sound.maxDistance;
            pulseAudioSource.pitch = sound.GetPitch();

            pulseAudioSource.PlayOneShot(sound.GetClip(), sound.GetVolume());
        }

        private void SetupAudioSource()
        {
            if (pulseAudioSource == null)
            {
                pulseAudioSource = GetComponent<AudioSource>();
                if (pulseAudioSource == null)
                    pulseAudioSource = gameObject.AddComponent<AudioSource>();
            }

            pulseAudioSource.playOnAwake = false;
            pulseAudioSource.loop = false;
            pulseAudioSource.spatialBlend = 0f;
            pulseAudioSource.volume = 1f;
        }

        private void SetLayerImagesEnabled(bool value)
        {
            if (veinsLayer?.image != null && veinsLayer.image.enabled != value)
                veinsLayer.image.enabled = value;
            if (plainLayer?.image != null && plainLayer.image.enabled != value)
                plainLayer.image.enabled = value;
        }

        private void SetupLayer(VignetteLayer layer)
        {
            if (layer == null || layer.image == null) return;
            layer.image.raycastTarget = false;
            layer.runtimeMaterial = Instantiate(layer.image.material);
            layer.image.material = layer.runtimeMaterial;
            ApplyHoleAspect(layer);
        }

        private void DestroyLayerMaterial(VignetteLayer layer)
        {
            if (layer?.runtimeMaterial != null) Destroy(layer.runtimeMaterial);
        }

        private void ApplyLayer(VignetteLayer layer, float beat)
        {
            if (layer == null || layer.runtimeMaterial == null) return;

            float visibility = ComputeVisibility(healthRatio, layer);

            float hole = layer.holeSizeByHealth.Evaluate(healthRatio);
            if (layer.pulseHoleWithBeat)
                hole *= 1f + beat * layer.holePulseAmount;

            layer.runtimeMaterial.SetFloat(HoleRadiusID, Mathf.Clamp01(hole));
            layer.runtimeMaterial.SetFloat(FeatherID, layer.holeFeather);

            // INVERSÃO DO PULSO:
            // beat = 1 (Tum)      -> Alpha vai para minPulseAlpha (0 = desaparece).
            // beat = 0 (Silêncio) -> Alpha vai para 1 (aparece).
            float pulseAlpha = Mathf.Lerp(1f, layer.minPulseAlpha, beat);
            layer.runtimeMaterial.SetFloat(PulseIntensityID, pulseAlpha * visibility);
        }

        private float ComputeVisibility(float ratio, VignetteLayer layer)
        {
            if (ratio >= layer.startHealth) return 0f;
            if (!layer.useFadeIn) return 1f;
            float t = Mathf.InverseLerp(layer.startHealth, 0f, ratio);
            return Mathf.SmoothStep(0f, 1f, t);
        }

        private void ApplyHoleAspect(VignetteLayer layer)
        {
            if (layer?.runtimeMaterial == null || layer.image == null) return;
            Rect r = layer.image.rectTransform.rect;
            if (r.height <= 0.001f) return;
            float canvasAspect = r.width / r.height;
            const float desired = 16f / 9f;
            layer.runtimeMaterial.SetFloat(HoleAspectID, canvasAspect / desired);
        }

        private float ComputeHeartbeat(float phase)
        {
            float cycle = phase % 1f;
            float lub = Mathf.Exp(-Mathf.Pow((cycle - LUB_CYCLE) / 0.08f, 2f));
            float dub = dubIntensity * Mathf.Exp(-Mathf.Pow((cycle - DUB_CYCLE) / 0.08f, 2f));
            return Mathf.Clamp01(Mathf.Max(lub, dub));
        }
    }
}