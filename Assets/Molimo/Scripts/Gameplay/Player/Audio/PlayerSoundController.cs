using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace MolimoByLulucaGameDev
{
    [RequireComponent(typeof(AudioSource))]
    public class PlayerSoundController : MonoBehaviour
    {
        [Header("Configurações do Mixer")]
        [SerializeField] private AudioMixerGroup defaultMixerGroup;

        [Header("Locomoção - Passos")]
        [SerializeField] private SoundData[] stepSound;
        [Space(5)]
        [SerializeField] private SoundData[] stepSoundGrass;
        [SerializeField] private LayerMask grassLayer;
        [Space(5)]
        [SerializeField] private SoundData[] stepSoundWater;
        [SerializeField] private LayerMask waterLayer;

        [Header("Locomoção - Ações")]
        [SerializeField] private SoundData[] jumpSound;
        [SerializeField] private SoundData[] dashSound;
        [SerializeField] private SoundData[] climbSound;
        [SerializeField] private SoundData[] skidSound;

        [Header("Combate")]
        [SerializeField] private SoundData[] slashSounds;
        [SerializeField] private SoundData[] blockSounds;
        [SerializeField] private SoundData[] clashSounds;
        [SerializeField] private SoundData[] weaponSwingSounds;

        [Header("Voz (Voice) & Estado")]
        [SerializeField] private SoundData[] voiceCombatSound;
        [SerializeField] private SoundData[] hurtSound;
        [SerializeField] private SoundData[] deathSound;
        [SerializeField] private SoundData[] respawnSound;

        [Header("Interação / Itens")]
        [Tooltip("Sons tocados ao apanhar armas ou itens do chão.")]
        [SerializeField] private SoundData[] weaponPickupSound;

        [Header("Detecção de Superfície")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.3f;

        [Header("Configurações de Morte")]
        [SerializeField] private bool playDeathOnHealthDepleted = true;
        [SerializeField] private PlayerHealth health;

        [Header("Configurações do Sistema")]
        [SerializeField] private bool preloadClipsOnStart = true;
        [SerializeField] private bool interruptActiveDescend = true;

        private AudioSource _audioSource;
        private AudioSource _descendAudioSource;
        private AudioLowPassFilter _descendLowPass;
        private Coroutine _descendRoutine;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.loop = false;
            _audioSource.spatialBlend = 0f;

            if (defaultMixerGroup != null)
                _audioSource.outputAudioMixerGroup = defaultMixerGroup;

            EnsureDescendAudioSource();
            if (health == null) health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            if (health != null) health.OnDied += HandleHealthDied;
        }

        private void OnDisable()
        {
            if (health != null) health.OnDied -= HandleHealthDied;

            if (_descendRoutine != null)
            {
                StopCoroutine(_descendRoutine);
                _descendRoutine = null;
            }
        }

        private void Start()
        {
            if (!preloadClipsOnStart) return;
            PreloadAllClips();
            WarmupAllClips();
        }

        public void PlaySoundCategory(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName)) return;

            switch (categoryName.ToLower().Trim())
            {
                case "step":
                case "footstep":
                    HandleStepSound();
                    break;
                case "jump": PlayRandomSoundFromList(jumpSound); break;
                case "dash": PlayRandomSoundFromList(dashSound); break;
                case "climb": PlayRandomSoundFromList(climbSound); break;
                case "skid": PlayRandomSoundFromList(skidSound); break;
                case "block": PlayRandomSoundFromList(blockSounds); break;
                case "clash": PlayRandomSoundFromList(clashSounds); break;
                case "weaponswing": PlayRandomSoundFromList(weaponSwingSounds); break;
                case "slash": PlayRandomSoundFromList(slashSounds); break;

                case "voice":
                case "voicecombat":
                    PlayRandomSoundFromList(voiceCombatSound);
                    break;

                // NOVO: Categoria para apanhar armas
                case "weaponpickup":
                case "pickup":
                    PlayRandomSoundFromList(weaponPickupSound);
                    break;

                case "hurt": PlayRandomSoundFromList(hurtSound); break;
                case "death": PlayRandomSoundFromList(deathSound); break;
                case "respawn": PlayRandomSoundFromList(respawnSound); break;

                default:
                    Debug.LogWarning($"[PlayerSoundController] Categoria '{categoryName}' não existe.");
                    break;
            }
        }

        private void HandleHealthDied()
        {
            if (!playDeathOnHealthDepleted) return;
            PlaySoundCategory("Death");
        }

        private void HandleStepSound()
        {
            Vector2 pos = groundCheckPoint != null ? groundCheckPoint.position : transform.position;

            if (grassLayer != 0 && stepSoundGrass != null && stepSoundGrass.Length > 0)
            {
                if (Physics2D.OverlapCircle(pos, groundCheckRadius, grassLayer) != null)
                {
                    PlayRandomSoundFromList(stepSoundGrass);
                    return;
                }
            }

            if (waterLayer != 0 && stepSoundWater != null && stepSoundWater.Length > 0)
            {
                if (Physics2D.OverlapCircle(pos, groundCheckRadius, waterLayer) != null)
                {
                    PlayRandomSoundFromList(stepSoundWater);
                    return;
                }
            }

            PlayRandomSoundFromList(stepSound);
        }

        private void PlayRandomSoundFromList(SoundData[] arr)
        {
            if (arr == null || arr.Length == 0) return;
            ExecuteSoundData(arr[Random.Range(0, arr.Length)]);
        }

        private void ExecuteSoundData(SoundData soundData)
        {
            if (soundData == null || soundData.GetClip() == null) return;

            if (soundData.enablePitchDescend)
            {
                PlayWithPitchDescend(soundData);
                return;
            }

            ApplySoundDataProperties(_audioSource, soundData);
            _audioSource.pitch = soundData.GetPitch();
            _audioSource.PlayOneShot(soundData.GetClip(), soundData.GetVolume());
        }

        private void ApplySoundDataProperties(AudioSource src, SoundData sd)
        {
            if (sd.mixerGroup != null && src.outputAudioMixerGroup != sd.mixerGroup)
                src.outputAudioMixerGroup = sd.mixerGroup;

            src.spatialBlend = sd.spatialBlend;
            src.priority = sd.priority;
            src.minDistance = sd.minDistance;
            src.maxDistance = sd.maxDistance;
        }

        private void PreloadAllClips()
        {
            LoadClips(stepSound);
            LoadClips(stepSoundGrass);
            LoadClips(stepSoundWater);
            LoadClips(jumpSound);
            LoadClips(dashSound);
            LoadClips(climbSound);
            LoadClips(skidSound);
            LoadClips(blockSounds);
            LoadClips(clashSounds);
            LoadClips(weaponSwingSounds);
            LoadClips(slashSounds);
            LoadClips(voiceCombatSound);
            LoadClips(hurtSound);
            LoadClips(deathSound);
            LoadClips(respawnSound);
            LoadClips(weaponPickupSound); // NOVO
        }

        private static void LoadClips(SoundData[] arr)
        {
            if (arr == null) return;
            foreach (var s in arr)
            {
                if (s?.GetClip() != null && s.GetClip().loadState != AudioDataLoadState.Loaded)
                    s.GetClip().LoadAudioData();
            }
        }

        private void WarmupAllClips()
        {
            var tempGO = new GameObject("TempWarmup");
            tempGO.transform.SetParent(transform, false);
            var tempSource = tempGO.AddComponent<AudioSource>();
            tempSource.playOnAwake = false;
            tempSource.volume = 0f;
            tempSource.mute = true;

            WarmupList(stepSound, tempSource);
            WarmupList(stepSoundGrass, tempSource);
            WarmupList(stepSoundWater, tempSource);
            WarmupList(jumpSound, tempSource);
            WarmupList(dashSound, tempSource);
            WarmupList(climbSound, tempSource);
            WarmupList(skidSound, tempSource);
            WarmupList(blockSounds, tempSource);
            WarmupList(clashSounds, tempSource);
            WarmupList(weaponSwingSounds, tempSource);
            WarmupList(slashSounds, tempSource);
            WarmupList(voiceCombatSound, tempSource);
            WarmupList(hurtSound, tempSource);
            WarmupList(deathSound, tempSource);
            WarmupList(respawnSound, tempSource);
            WarmupList(weaponPickupSound, tempSource); // NOVO

            Destroy(tempGO, 0.5f);
        }

        private void WarmupList(SoundData[] arr, AudioSource source)
        {
            if (arr == null) return;
            foreach (var s in arr)
                if (s?.GetClip() != null) source.PlayOneShot(s.GetClip(), 0f);
        }

        private void EnsureDescendAudioSource()
        {
            if (_descendAudioSource != null) return;

            var go = new GameObject("DescendAudioSource");
            go.transform.SetParent(transform, false);
            _descendAudioSource = go.AddComponent<AudioSource>();
            _descendAudioSource.playOnAwake = false;
            _descendAudioSource.loop = false;
            _descendAudioSource.spatialBlend = 0f;

            if (defaultMixerGroup != null)
                _descendAudioSource.outputAudioMixerGroup = defaultMixerGroup;

            _descendLowPass = go.AddComponent<AudioLowPassFilter>();
            _descendLowPass.cutoffFrequency = 22000f;
            _descendLowPass.enabled = false;
        }

        private void PlayWithPitchDescend(SoundData soundData)
        {
            if (_descendAudioSource == null) EnsureDescendAudioSource();

            if (_descendRoutine != null)
            {
                if (!interruptActiveDescend) return;
                StopCoroutine(_descendRoutine);
                _descendAudioSource.Stop();
            }

            _descendRoutine = StartCoroutine(DescendRoutine(soundData));
        }

        private IEnumerator DescendRoutine(SoundData sd)
        {
            ApplySoundDataProperties(_descendAudioSource, sd);
            _descendAudioSource.clip = sd.GetClip();
            _descendAudioSource.volume = sd.GetVolume();
            _descendAudioSource.pitch = sd.descendStartPitch;

            bool useLowPass = sd.descendUseLowPass && _descendLowPass != null;
            if (useLowPass)
            {
                _descendLowPass.enabled = true;
                _descendLowPass.cutoffFrequency = sd.descendLowPassStart;
            }
            else if (_descendLowPass != null) _descendLowPass.enabled = false;

            _descendAudioSource.Play();

            float descentTime = sd.descendDuration - sd.descendFadeOutDuration;
            float elapsed = 0f;

            while (elapsed < descentTime)
            {
                elapsed += Time.deltaTime;
                float curve = sd.descendPitchCurve.Evaluate(elapsed / descentTime);
                _descendAudioSource.pitch = Mathf.Lerp(sd.descendStartPitch, sd.descendEndPitch, curve);

                if (useLowPass)
                {
                    float cutoff = Mathf.Exp(Mathf.Lerp(Mathf.Log(sd.descendLowPassStart), Mathf.Log(sd.descendLowPassEnd), curve));
                    _descendLowPass.cutoffFrequency = cutoff;
                }
                yield return null;
            }

            _descendAudioSource.pitch = sd.descendEndPitch;

            if (sd.descendFadeOutDuration > 0f)
            {
                float startVol = _descendAudioSource.volume;
                float fElapsed = 0f;
                while (fElapsed < sd.descendFadeOutDuration)
                {
                    fElapsed += Time.deltaTime;
                    _descendAudioSource.volume = Mathf.Lerp(startVol, 0f, fElapsed / sd.descendFadeOutDuration);
                    yield return null;
                }
            }

            _descendAudioSource.Stop();
            _descendLowPass.enabled = false;
            _descendRoutine = null;
        }
    }
}