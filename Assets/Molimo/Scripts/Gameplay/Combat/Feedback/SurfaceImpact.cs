using UnityEngine;
using Unity.Cinemachine; // ✅ Namespace correto para Cinemachine 3.x no Unity 6

namespace MolimoByLulucaGameDev
{
    /// <summary>
    /// Adicione este script a qualquer objeto do cenário (Metal, Madeira, Grama)
    /// para que ele reaja ao ser atingido pela espada do Player, variando os sons e tremendo a câmera.
    /// </summary>
    public class SurfaceImpact : MonoBehaviour
    {
        [Header("Feedback Visual (VFX)")]
        [Tooltip("O prefab da partícula que vai surgir no momento do impacto.")]
        [SerializeField] private ParticleSystem impactVfxPrefab;

        [Header("Feedback Sonoro (SFX)")]
        [Tooltip("Lista de sons para tocar. O script vai escolher UM aleatoriamente a cada impacto.")]
        [SerializeField] private SoundData[] impactSounds;

        [Tooltip("Onde o som será tocado (opcional). Se vazio, toca no centro do objeto.")]
        [SerializeField] private Transform soundPosition;

        [Header("Feedback de Câmera (Cinemachine)")]
        [Tooltip("Referência ao componente de impulso da câmera.")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        private void Awake()
        {
            // Auto-detecta o componente se você esqueceu de arrastar no Inspector
            if (impulseSource == null)
                impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        public void PlayImpact(Vector3 hitPoint, Vector2 hitDirection)
        {
            PlayVfx(hitPoint, hitDirection);
            PlaySfx();
            PlayCameraShake();
        }

        private void PlayVfx(Vector3 hitPoint, Vector2 hitDirection)
        {
            if (impactVfxPrefab == null) return;

            ParticleSystem vfx = Instantiate(impactVfxPrefab, hitPoint, Quaternion.identity);

            float angle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
            vfx.transform.rotation = Quaternion.Euler(0, 0, angle);

            vfx.Play(true);

            float destroyTime = vfx.main.duration + vfx.main.startLifetime.constantMax;
            Destroy(vfx.gameObject, destroyTime);
        }

        private void PlaySfx()
        {
            if (impactSounds == null || impactSounds.Length == 0) return;

            SoundData soundToPlay = impactSounds[Random.Range(0, impactSounds.Length)];
            if (soundToPlay == null || soundToPlay.GetClip() == null) return;

            Vector3 pos = soundPosition != null ? soundPosition.position : transform.position;

            GameObject sfxObj = new GameObject("ImpactSFX_Temp");
            sfxObj.transform.position = pos;
            AudioSource source = sfxObj.AddComponent<AudioSource>();

            source.clip = soundToPlay.GetClip();
            source.volume = soundToPlay.GetVolume();
            source.pitch = soundToPlay.GetPitch();

            if (soundToPlay.mixerGroup != null)
                source.outputAudioMixerGroup = soundToPlay.mixerGroup;

            source.spatialBlend = soundToPlay.spatialBlend;
            source.priority = soundToPlay.priority;
            source.minDistance = soundToPlay.minDistance;
            source.maxDistance = soundToPlay.maxDistance;

            source.Play();

            Destroy(sfxObj, soundToPlay.GetClip().length);
        }

        private void PlayCameraShake()
        {
            if (impulseSource != null)
            {
                // Dispara o Impulso. A força e a duração são definidas no próprio componente Cinemachine.
                impulseSource.GenerateImpulse();
            }
        }
    }
}