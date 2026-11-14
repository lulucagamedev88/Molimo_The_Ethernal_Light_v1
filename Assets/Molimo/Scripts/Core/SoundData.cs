using UnityEngine;
using UnityEngine.Audio;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Dados de áudio para o AudioManager do Molimo.
    /// Suporta 2D/3D via spatialBlend, roteamento via AudioMixerGroup,
    /// e opcionalmente "pitch descend" com low-pass (efeito Battlefield death).
    /// </summary>
    [CreateAssetMenu(fileName = "NewSound", menuName = "Molimo/Sound Data")]
    public class SoundData : ScriptableObject
    {
        // ==========================================================
        // Identificação
        // ==========================================================
        [Header("Identificação")]
        [Tooltip("Nome de identificação (default = nome do asset se vazio).")]
        public string soundName;

        // ==========================================================
        // Clip
        // ==========================================================
        [Header("Clip")]
        [Tooltip("AudioClip a ser reproduzido.")]
        public AudioClip clip;

        // ==========================================================
        // Volume & Pitch
        // ==========================================================
        [Header("Volume & Pitch")]
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0f, 0.5f)] public float pitchRandomness = 0.1f;

        // ==========================================================
        // Reprodução
        // ==========================================================
        [Header("Reprodução")]
        public bool loop = false;

        [Tooltip("Prioridade do AudioSource (0 = máxima, 256 = mínima).")]
        [Range(0, 256)] public int priority = 128;

        // ==========================================================
        // Mixer
        // ==========================================================
        [Header("Mixer")]
        [Tooltip("Grupo do AudioMixer (SFX, Music, Ambient, UI).")]
        public AudioMixerGroup mixerGroup;

        // ==========================================================
        // Espacialização
        // ==========================================================
        [Header("Espacialização")]
        [Tooltip("0 = 2D (HUD) · 1 = 3D (ambiente).")]
        [Range(0f, 1f)] public float spatialBlend = 0f;

        [Tooltip("Distância mínima (só relevante se spatialBlend > 0).")]
        [Min(0f)] public float minDistance = 1f;

        [Tooltip("Distância máxima (só relevante se spatialBlend > 0).")]
        [Min(0f)] public float maxDistance = 50f;

        // ==========================================================
        // Pitch Descend (efeito Battlefield death)
        // ==========================================================
        [Header("Pitch Descend (Opcional)")]
        [Tooltip("Liga o efeito de descida de pitch + muffling (low-pass). " +
                 "Quando ativo, o PlayerSoundController usa um AudioSource dedicado.")]
        public bool enablePitchDescend = false;

        [Tooltip("Pitch inicial. Suba para 1.8–2.0 para efeito mais dramático.")]
        [Range(0.1f, 3f)] public float descendStartPitch = 1.8f;

        [Tooltip("Pitch final (lento/grave). 0.15 = extremo, 0.4 = natural.")]
        [Range(0.05f, 1f)] public float descendEndPitch = 0.15f;

        [Tooltip("Duração total da descida (segundos). 3–4s é o ideal para Battlefield.")]
        [Min(0.1f)] public float descendDuration = 3.5f;

        [Tooltip("Curva da descida. EaseInOut = suave; S-curve = acelera no meio.")]
        public AnimationCurve descendPitchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Fade out de volume nos últimos X segundos. 0 = sem fade.")]
        [Min(0f)] public float descendFadeOutDuration = 0.8f;

        // ==========================================================
        // Low-Pass (parte crucial do efeito Battlefield)
        // ==========================================================
        [Header("Low-Pass (Muffling) — parte crucial do efeito")]
        [Tooltip("Aplica um filtro low-pass que fecha progressivamente (som abafado). " +
                 "LIGUE para o efeito Battlefield completo.")]
        public bool descendUseLowPass = true;

        [Tooltip("Cutoff inicial (Hz). 20000 = totalmente aberto / nítido.")]
        [Range(100f, 20000f)] public float descendLowPassStart = 18000f;

        [Tooltip("Cutoff final (Hz). 400–600 = muito abafado / 'morto'.")]
        [Range(100f, 20000f)] public float descendLowPassEnd = 450f;

        // ==========================================================
        // API pública
        // ==========================================================
        public AudioClip GetClip() => clip;
        public float GetVolume() => volume;

        /// <summary>Pitch com randomização aplicada, sempre em [0.01, 3].</summary>
        public float GetPitch()
        {
            float p = pitch + Random.Range(-pitchRandomness, pitchRandomness);
            return Mathf.Max(0.01f, p);
        }

        // ==========================================================
        // Validação no Editor
        // ==========================================================
        private void OnValidate()
        {
            // maxDistance nunca menor que minDistance.
            if (maxDistance < minDistance) maxDistance = minDistance;

            // Preenche soundName com o nome do asset se estiver vazio.
            if (string.IsNullOrEmpty(soundName)) soundName = name;

            // Sanidade do descend.
            if (enablePitchDescend)
            {
                if (descendDuration < 0.1f) descendDuration = 0.1f;
                if (descendEndPitch > descendStartPitch)
                    descendEndPitch = descendStartPitch * 0.5f;
                if (descendLowPassEnd > descendLowPassStart)
                    descendLowPassEnd = descendLowPassStart * 0.5f;
            }
        }
    }
}