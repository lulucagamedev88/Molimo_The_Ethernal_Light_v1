using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class MolimoCombat : MonoBehaviour
    {
        private enum AttackPhase { None, Dive, Return, Cooldown }

        [Header("Detecção de Inimigos")]
        [SerializeField] private float detectionRadius = 8f;
        [SerializeField] private float maxAttackDistanceFromPlayer = 6f;
        [SerializeField] private LayerMask enemyLayer = ~0;
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Ataque")]
        [SerializeField] private float attackSpeed = 15f;
        [SerializeField] private float returnSpeed = 12f;
        [SerializeField] private float attackCooldown = 3f;
        [SerializeField] private float hitDistance = 0.9f;
        [SerializeField] private float returnArrivalDistance = 0.3f;

        [Header("Referências de Combate")]
        [Tooltip("Hitbox que será ativada quando o Molimo colidir com o inimigo.")]
        [SerializeField] private AttackHitbox attackHitbox;

        [Header("Colisão com o Player (Safe Zone)")]
        [Tooltip("Garante que ao voltar, o Molimo não pare no pivot do Player.")]
        [SerializeField] private float minDistanceFromPlayerPivot = 1.2f;

        private Transform _playerTransform;
        private Vector2 _followOffset;
        private MolimoVFXManager _vfx;

        private Transform _currentTarget;
        private AttackPhase _phase = AttackPhase.None;
        private float _phaseTimer;
        private Vector2 _diveTarget;

        public Transform CurrentTarget => _currentTarget;

        private void Awake()
        {
            if (attackHitbox == null) attackHitbox = GetComponent<AttackHitbox>();
        }

        public void Initialize(Transform player, Vector2 followOffset)
        {
            _playerTransform = player;
            _followOffset = followOffset;
            if (_vfx == null) _vfx = GetComponent<MolimoVFXManager>();
        }

        public void BeginCombat() { _phase = AttackPhase.None; _currentTarget = null; }
        public void Cancel() { _currentTarget = null; _phase = AttackPhase.None; }
        public bool HasAnyTargetsInRange() => FindNearestEnemyInRange() != null;

        public bool Tick(Vector3 bobOffset)
        {
            if (_currentTarget != null && Vector2.Distance(_currentTarget.position, _playerTransform.position) > maxAttackDistanceFromPlayer)
            {
                Cancel();
                return false;
            }

            if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy)
            {
                Transform next = FindNearestEnemyInRange();
                if (next == null) { Cancel(); return false; }
                _currentTarget = next;
                StartDive();
            }

            switch (_phase)
            {
                case AttackPhase.None: StartDive(); break;
                case AttackPhase.Dive: UpdateDive(); break;
                case AttackPhase.Return: UpdateReturn(bobOffset); break;
                case AttackPhase.Cooldown: UpdateCooldown(bobOffset); break;
            }
            return true;
        }

        private void StartDive()
        {
            _phase = AttackPhase.Dive;
            _phaseTimer = 0f;
            _diveTarget = _currentTarget.position;
        }

        private void UpdateDive()
        {
            if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy) { _phase = AttackPhase.None; return; }

            if (Vector2.Distance(transform.position, _diveTarget) <= hitDistance)
            {
                // Ativa a Hitbox no impacto
                if (attackHitbox != null)
                {
                    Vector2 diveDirection = (_diveTarget - (Vector2)transform.position).normalized;
                    if (diveDirection == Vector2.zero && _currentTarget != null)
                        diveDirection = (_currentTarget.position - transform.position).normalized;

                    attackHitbox.PerformHitTest(diveDirection);
                }

                _phase = AttackPhase.Return;
                _phaseTimer = 0f;
                return;
            }

            transform.position = Vector2.MoveTowards(transform.position, _diveTarget, attackSpeed * Time.deltaTime);
        }

        private void UpdateReturn(Vector3 bobOffset)
        {
            Vector2 followPos = (Vector2)GetFollowPosition() + (Vector2)bobOffset;
            transform.position = Vector2.MoveTowards(transform.position, followPos, returnSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, followPos) <= returnArrivalDistance)
            {
                _phase = AttackPhase.Cooldown;
                _phaseTimer = 0f;
            }
        }

        private void UpdateCooldown(Vector3 bobOffset)
        {
            Vector2 followPos = (Vector2)GetFollowPosition() + (Vector2)bobOffset;
            transform.position = Vector2.Lerp(transform.position, followPos, Time.deltaTime * 5f);

            _phaseTimer += Time.deltaTime;
            if (_phaseTimer >= attackCooldown && _currentTarget != null && _currentTarget.gameObject.activeInHierarchy)
                StartDive();
        }

        private Transform FindNearestEnemyInRange()
        {
            if (_playerTransform == null) return null;

            Collider2D[] hits = Physics2D.OverlapCircleAll(_playerTransform.position, detectionRadius, enemyLayer);
            Transform best = null;
            float bestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit == null || !hit.CompareTag(enemyTag)) continue;

                float d = Vector2.Distance(hit.transform.position, _playerTransform.position);
                if (d > maxAttackDistanceFromPlayer) continue;

                if (d < bestDist) { bestDist = d; best = hit.transform; }
            }
            return best;
        }

        private Vector3 GetFollowPosition()
        {
            if (_playerTransform == null) return transform.position;

            Vector3 playerPos = _playerTransform.position;
            Vector3 basePos = playerPos + _playerTransform.right * _followOffset.x + Vector3.up * _followOffset.y;

            // --- SAFE ZONE (NUNCA PARAR NO PIVOT) ---
            Vector3 dirFromPlayer = basePos - playerPos;
            dirFromPlayer.z = 0;

            if (dirFromPlayer.magnitude < minDistanceFromPlayerPivot)
            {
                if (dirFromPlayer.magnitude < 0.01f)
                    dirFromPlayer = Vector3.up;

                basePos = playerPos + dirFromPlayer.normalized * minDistanceFromPlayerPivot;
            }

            return basePos;
        }
    }
}