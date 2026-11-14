using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Gerencia o volume do AudioMixer com persistência via PlayerPrefs.
    /// Converte slider linear (0-1) → decibéis (−80 a 0 dB) para o AudioMixer.
    /// Sliders no Inspector devem estar configurados com Range(0, 1).
    /// </summary>
    public class AudioVolumeController : MonoBehaviour
    {
        [Header("Mixer")]
        [SerializeField] private AudioMixer mixer;

        [Header("Nomes dos Parâmetros Expostos")]
        [Tooltip("Nome EXATO do Exposed Parameter no AudioMixer.")]
        [SerializeField] private string masterParam = "MasterVolume";
        [SerializeField] private string musicParam = "MusicVolume";
        [SerializeField] private string ambientParam = "AmbientVolume";
        [SerializeField] private string sfxParam = "SFXVolume";
        [SerializeField] private string uiParam = "UIVolume";

        [Header("Sliders (opcional)")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider ambientSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private Slider uiSlider;

        // ===== Constantes =====
        private const float MIN_DB = -80f; // silêncio total
        private const float MAX_DB = 0f;   // volume máximo

        // ===== Chaves de PlayerPrefs =====
        private const string KEY_MASTER = "vol_master";
        private const string KEY_MUSIC = "vol_music";
        private const string KEY_AMBIENT = "vol_ambient";
        private const string KEY_SFX = "vol_sfx";
        private const string KEY_UI = "vol_ui";

        // ==========================================================
        // Ciclo de vida
        // ==========================================================
        private void Awake()
        {
            LoadAllVolumes();
            BindSliders();
        }

        // ==========================================================
        // Carregar / salvar
        // ==========================================================
        private void LoadAllVolumes()
        {
            ApplyVolume(masterParam, PlayerPrefs.GetFloat(KEY_MASTER, 1f));
            ApplyVolume(musicParam, PlayerPrefs.GetFloat(KEY_MUSIC, 1f));
            ApplyVolume(ambientParam, PlayerPrefs.GetFloat(KEY_AMBIENT, 1f));
            ApplyVolume(sfxParam, PlayerPrefs.GetFloat(KEY_SFX, 1f));
            ApplyVolume(uiParam, PlayerPrefs.GetFloat(KEY_UI, 1f));
        }

        /// <summary>
        /// Define o volume de um canal e salva em PlayerPrefs.
        /// </summary>
        public void SetVolume(string paramName, float linear01, string playerPrefsKey)
        {
            ApplyVolume(paramName, linear01);
            PlayerPrefs.SetFloat(playerPrefsKey, linear01);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Converte linear (0-1) → dB (−80 a 0) e aplica no Mixer.
        /// Não salva em PlayerPrefs (usar SetVolume para isso).
        /// </summary>
        private void ApplyVolume(string paramName, float linear01)
        {
            if (mixer == null || string.IsNullOrEmpty(paramName)) return;

            linear01 = Mathf.Clamp01(linear01);

            // 0 → -80 dB (mudo); >0 → conversão logarítmica.
            float dB = linear01 <= 0.0001f
                ? MIN_DB
                : Mathf.Log10(linear01) * 20f;

            // Clamp de segurança.
            dB = Mathf.Clamp(dB, MIN_DB, MAX_DB);

            if (!mixer.SetFloat(paramName, dB))
            {
                Debug.LogWarning($"[AudioVolumeController] Parâmetro '{paramName}' " +
                                 "não encontrado no Mixer. Verifique se foi exposto e renomeado.");
            }
        }

        // ==========================================================
        // Sliders (opcional)
        // ==========================================================
        private void BindSliders()
        {
            if (masterSlider != null) { masterSlider.value = PlayerPrefs.GetFloat(KEY_MASTER, 1f); masterSlider.onValueChanged.AddListener(v => SetVolume(masterParam, v, KEY_MASTER)); }
            if (musicSlider != null) { musicSlider.value = PlayerPrefs.GetFloat(KEY_MUSIC, 1f); musicSlider.onValueChanged.AddListener(v => SetVolume(musicParam, v, KEY_MUSIC)); }
            if (ambientSlider != null) { ambientSlider.value = PlayerPrefs.GetFloat(KEY_AMBIENT, 1f); ambientSlider.onValueChanged.AddListener(v => SetVolume(ambientParam, v, KEY_AMBIENT)); }
            if (sfxSlider != null) { sfxSlider.value = PlayerPrefs.GetFloat(KEY_SFX, 1f); sfxSlider.onValueChanged.AddListener(v => SetVolume(sfxParam, v, KEY_SFX)); }
            if (uiSlider != null) { uiSlider.value = PlayerPrefs.GetFloat(KEY_UI, 1f); uiSlider.onValueChanged.AddListener(v => SetVolume(uiParam, v, KEY_UI)); }
        }
    }
}