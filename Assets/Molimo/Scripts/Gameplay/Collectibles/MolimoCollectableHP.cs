using System.Collections;
using UnityEngine;

namespace MolimoByLulucaGameDev
{
    [DisallowMultipleComponent]
    public class MolimoCollectableHP : MonoBehaviour
    {
        public enum OscillationMode { None, Wasp, RandomFloat }
        public enum ItemState { Idle, Attracted, Collected }

        [Header("Cura")]
        [SerializeField, Range(1, 100)] private int healCharge = 1;

        [Header("Movimento 2D")]
        [SerializeField] private float defaultPullSpeed = 8f;
        [SerializeField] private float arrivalDistance = 0.4f;
        [SerializeField] private float minHeightOffset = 0.15f;
        [SerializeField] private OscillationMode oscillationMode = OscillationMode.Wasp;

        [Header("Oscilação - Wasp")]
        [SerializeField] private float waspFrequency = 12f;
        [SerializeField] private float waspAmplitude = 0.4f;

        [Header("Rotação 2D")]
        [SerializeField] private bool addRotation = true;
        [SerializeField] private float rotationSpeed = 180f;

        [Header("Som")]
        [SerializeField] private SoundData[] attractSoundDataList;
        [SerializeField, Range(0.05f, 2f)] private float fadeDuration = 0.3f;

        public ItemState State { get; private set; } = ItemState.Idle;
        public int HealCharge => healCharge;
        public bool IsAttracted => State == ItemState.Attracted;

        private Transform _target;
        private MolimoHealthContainer _targetContainer;
        private float _pullSpeed;
        private float _seed;

        private GameObject _activeSfxObject;
        private AudioSource _activeAudioSource;

        private void Awake()
        {
            _seed = Random.value * 100f;
        }

        private void Update()
        {
            if (State != ItemState.Attracted) return;
            if (_target == null)
            {
                State = ItemState.Idle;
                return;
            }

            MoveTowardsTarget();
            CheckArrival();
        }

        private void OnDestroy()
        {
            if (_activeSfxObject != null) Destroy(_activeSfxObject);
        }

        public void AttractTo(Transform target, float speed = -1f)
        {
            if (State != ItemState.Idle || target == null) return;

            State = ItemState.Attracted;
            _target = target;
            _pullSpeed = speed > 0f ? speed : defaultPullSpeed;
            _targetContainer = target.GetComponentInParent<MolimoHealthContainer>();

            if (TryGetComponent(out Rigidbody2D rb2d)) rb2d.isKinematic = true;
            if (TryGetComponent(out Collider2D col2d)) col2d.isTrigger = true;

            PlayAttractSound();
        }

        private void MoveTowardsTarget()
        {
            Vector2 currentPos = transform.position;
            Vector2 targetPos = _target.position;

            Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPos, _pullSpeed * Time.deltaTime);

            if (oscillationMode == OscillationMode.Wasp)
            {
                Vector2 dir = (targetPos - currentPos).normalized;
                Vector2 side = new Vector2(-dir.y, dir.x);
                float tW = Time.time * waspFrequency + _seed;
                nextPos += side * (Mathf.Cos(tW) * waspFrequency * waspAmplitude * Time.deltaTime);
            }

            if (nextPos.y < targetPos.y + minHeightOffset) nextPos.y = targetPos.y + minHeightOffset;

            transform.position = nextPos;

            if (addRotation)
                transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }

        private void CheckArrival()
        {
            if (Vector2.Distance(transform.position, _target.position) > arrivalDistance) return;
            DepositAndDestroy();
        }

        private void DepositAndDestroy()
        {
            if (State == ItemState.Collected) return;
            State = ItemState.Collected;

            if (_targetContainer != null) _targetContainer.TryAddHP(healCharge);

            StopAttractSound();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        private void PlayAttractSound()
        {
            if (attractSoundDataList == null || attractSoundDataList.Length == 0) return;

            SoundData data = attractSoundDataList[Random.Range(0, attractSoundDataList.Length)];
            if (data == null || data.GetClip() == null) return;

            _activeSfxObject = new GameObject($"SFX_{data.soundName}");
            _activeSfxObject.transform.position = transform.position;

            _activeAudioSource = _activeSfxObject.AddComponent<AudioSource>();
            _activeAudioSource.clip = data.GetClip();
            _activeAudioSource.volume = data.volume;
            _activeAudioSource.pitch = data.GetPitch();
            _activeAudioSource.spatialBlend = 0f;
            _activeAudioSource.Play();
        }

        private void StopAttractSound()
        {
            if (_activeAudioSource == null || _activeSfxObject == null) return;
            var cleaner = _activeSfxObject.AddComponent<AudioSourceAutoCleaner>();
            cleaner.FadeAndDestroy(_activeAudioSource, fadeDuration);
            _activeSfxObject = null;
            _activeAudioSource = null;
        }
    }

    internal class AudioSourceAutoCleaner : MonoBehaviour
    {
        public void FadeAndDestroy(AudioSource source, float duration)
        {
            StartCoroutine(FadeRoutine(source, duration));
        }

        private IEnumerator FadeRoutine(AudioSource source, float duration)
        {
            if (source == null) { Destroy(gameObject); yield break; }
            float startVolume = source.volume;
            float timer = 0f;

            while (timer < duration && source != null)
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
                yield return null;
            }

            if (source != null) source.Stop();
            Destroy(gameObject);
        }
    }
}