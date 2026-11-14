using UnityEngine;

namespace MolimoByLulucaGameDev
{
    public class MolimoAI : MonoBehaviour
    {
        public enum MolimoState { Normal, AlertingPlayer, WaitingForSwordRespawn, GoToSword, WaitingAtSword, ReturnToPlayer, AttackingEnemy, HealingPlayer }

        [Header("Referências")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerMovement playerMovement;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Módulos")]
        [SerializeField] private MolimoCombat combat;
        [SerializeField] private MolimoHealer healer;
        [SerializeField] private MolimoHealthContainer healthContainer;

        [Header("Acompanhamento")]
        [SerializeField] private Vector2 followOffset = new Vector2(0f, 1.5f);
        [SerializeField] private float followSmoothTime = 0.35f;
        [SerializeField] private float maxFollowSpeed = 8f;

        [Header("Colisão com o Player (Safe Zone)")]
        [Tooltip("O Molimo NUNCA vai parar a uma distância menor do que esta em relação ao pivot do Player.")]
        [SerializeField] private float minDistanceFromPlayerPivot = 1.2f;

        [Header("Desvio de Voo (Não passar pelos pés)")]
        [SerializeField] private float avoidRadius = 1.5f;
        [SerializeField] private float arcHeight = 1.5f;

        [Header("Movimento Flutuante (Aleatório)")]
        [Tooltip("Tempo (segundos) que o player precisa ficar parado para o Molimo trocar de lado novamente.")]
        [SerializeField] private float idleSideSwitchInterval = 3f;
        [Tooltip("Distância X que o Molimo se afasta do centro (quando sorteado).")]
        [SerializeField] private float idleSideDistance = 1.0f;
        [Tooltip("Velocidade de transição ao trocar de lado (menor = mais suave/lento).")]
        [SerializeField] private float sideSwitchSmoothTime = 0.8f;

        [Header("Flutuação Base (Bob)")]
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 2f;

        [Header("Sprint Follow")]
        [SerializeField] private float sprintReactionDelay = 2.5f;
        [SerializeField] private float sprintSpeedMultiplier = 2.2f;
        [Tooltip("Quanto o Molimo se adianta À FRENTE do Player (na direção do movimento).")]
        [SerializeField] private float sprintAheadDistance = 2.5f;
        [SerializeField] private float sprintRecoveryTime = 0.3f;

        [Header("Teleport Catch-Up")]
        [SerializeField] private float teleportDetectDistance = 20f;
        [SerializeField] private float teleportCatchUpSpeedMultiplier = 8f;
        [SerializeField] private float teleportCatchUpDuration = 4f;
        [SerializeField] private float teleportCatchUpEndDistance = 3.5f;

        [Header("Alerta")]
        [SerializeField] private float alertDuration = 1.8f;
        [SerializeField] private float alertMoveSpeed = 6f;

        private MolimoState _state = MolimoState.Normal;
        private Vector3 _currentVelocity;
        private Vector3 _bobOffset;
        private float _bobTime;
        private float _stateTimer;

        private float _playerSprintTimer;
        private bool _isSprintBoosting;
        private float _sprintBoostBlend;
        private float _catchUpBoostTimer;

        // Controle de Troca de Lado (Idle)
        private int _currentSide = 0;
        private float _idleTimer = 0f;
        private bool _wasPlayerIdleLastFrame = false;

        public MolimoState CurrentState => _state;

        private void Awake()
        {
            ResolveReferences();
            if (combat != null) combat.Initialize(playerTransform, followOffset);
            if (healer != null) healer.Initialize(playerTransform, healthContainer);
        }

        private void Start()
        {
            if (playerTransform != null)
                transform.position = GetFollowTargetPosition();

            EnterState(MolimoState.Normal);
        }

        private void Update()
        {
            if (playerTransform == null) return;

            if (_state != MolimoState.HealingPlayer && healer != null && healer.CanHeal())
                EnterState(MolimoState.HealingPlayer);

            UpdateTimersAndBobbing();
            UpdateSprintFollow();
            UpdateTeleportCatchUp();
            UpdateIdleSideSwitch();

            ExecuteCurrentState();
            UpdateFacingDirection();
            UpdateAnimatorState();
        }

        private void ResolveReferences()
        {
            if (playerTransform == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) playerTransform = p.transform; }
            if (playerMovement == null && playerTransform != null) playerMovement = playerTransform.GetComponentInParent<PlayerMovement>();
            if (animator == null) animator = GetComponent<Animator>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (combat == null) combat = GetComponent<MolimoCombat>();
            if (healer == null) healer = GetComponent<MolimoHealer>();
            if (healthContainer == null) healthContainer = GetComponent<MolimoHealthContainer>();
        }

        private void UpdateAnimatorState()
        {
            if (animator == null) return;
            animator.SetBool("IsIdle", _state == MolimoState.Normal || _state == MolimoState.ReturnToPlayer);
            animator.SetBool("IsAlerting", _state == MolimoState.AlertingPlayer);
            animator.SetBool("IsAttacking", _state == MolimoState.AttackingEnemy);
            animator.SetBool("IsCollecting", _state == MolimoState.HealingPlayer);
        }

        private void UpdateFacingDirection()
        {
            if (spriteRenderer == null || playerTransform == null) return;
            Vector3 targetPos = (_state == MolimoState.AttackingEnemy && combat != null && combat.CurrentTarget != null) ? combat.CurrentTarget.position : playerTransform.position;
            float dirX = targetPos.x - transform.position.x;
            if (Mathf.Abs(dirX) > 0.1f) spriteRenderer.flipX = dirX < 0;
        }

        private void UpdateIdleSideSwitch()
        {
            if (playerMovement == null || _state != MolimoState.Normal) return;

            bool isPlayerIdle = !playerMovement.IsWalking && !playerMovement.IsRunning && !playerMovement.IsDashing;

            if (isPlayerIdle != _wasPlayerIdleLastFrame)
            {
                _wasPlayerIdleLastFrame = isPlayerIdle;
                _idleTimer = 0f;
                _currentSide = Random.Range(-1, 2); // -1 (Esquerda), 0 (Centro), 1 (Direita)
            }

            if (isPlayerIdle)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= idleSideSwitchInterval)
                {
                    _currentSide = Random.Range(-1, 2);
                    _idleTimer = 0f;
                }
            }
        }

        private void UpdateSprintFollow()
        {
            bool playerSprinting = playerMovement != null && playerMovement.IsRunning;

            if (playerSprinting)
            {
                _playerSprintTimer += Time.deltaTime;
                if (_playerSprintTimer >= sprintReactionDelay) _isSprintBoosting = true;
            }
            else
            {
                _isSprintBoosting = false;
                _playerSprintTimer = 0f;
            }

            _sprintBoostBlend = Mathf.MoveTowards(_sprintBoostBlend, _isSprintBoosting ? 1f : 0f, (1f / Mathf.Max(0.01f, sprintRecoveryTime)) * Time.deltaTime);
        }

        private void UpdateTeleportCatchUp()
        {
            if (playerTransform == null) return;
            float dist = Vector2.Distance(transform.position, GetFollowTargetPosition());
            if (dist >= teleportDetectDistance) _catchUpBoostTimer = teleportCatchUpDuration;
            else if (_catchUpBoostTimer > 0f) { _catchUpBoostTimer -= Time.deltaTime; if (dist <= teleportCatchUpEndDistance) _catchUpBoostTimer = 0f; }
        }

        private float GetEffectiveSmoothTime()
        {
            if (_state == MolimoState.Normal && _idleTimer > 0f)
                return Mathf.Max(sideSwitchSmoothTime, followSmoothTime);

            return _catchUpBoostTimer > 0f ? 0.05f : (_isSprintBoosting ? followSmoothTime * Mathf.Lerp(1f, 0.35f, _sprintBoostBlend) : followSmoothTime);
        }

        private float GetEffectiveMaxFollowSpeed() => _catchUpBoostTimer > 0f ? maxFollowSpeed * teleportCatchUpSpeedMultiplier : (_isSprintBoosting ? maxFollowSpeed * Mathf.Lerp(1f, sprintSpeedMultiplier, _sprintBoostBlend) : maxFollowSpeed);

        private void UpdateTimersAndBobbing()
        {
            _stateTimer += Time.deltaTime;
            _bobTime += Time.deltaTime * bobFrequency;
            _bobOffset = Vector3.up * (Mathf.Sin(_bobTime) * bobAmplitude);
        }

        private void ExecuteCurrentState()
        {
            Vector3 targetPos = GetFollowTargetPosition() + _bobOffset;
            targetPos = ApplyFlightArc(targetPos);

            switch (_state)
            {
                case MolimoState.Normal:
                    transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, GetEffectiveSmoothTime(), GetEffectiveMaxFollowSpeed(), Time.deltaTime);
                    if (combat != null && combat.HasAnyTargetsInRange()) EnterState(MolimoState.AttackingEnemy);
                    break;
                case MolimoState.AttackingEnemy:
                    if (combat == null || !combat.Tick(_bobOffset)) EnterState(MolimoState.Normal);
                    break;
                case MolimoState.HealingPlayer:
                    if (healer == null || !healer.Tick()) EnterState(MolimoState.Normal);
                    break;
                case MolimoState.ReturnToPlayer:
                    transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, GetEffectiveSmoothTime(), GetEffectiveMaxFollowSpeed(), Time.deltaTime);
                    if (Vector3.Distance(transform.position, targetPos) <= 1.0f) EnterState(MolimoState.Normal);
                    break;
                default:
                    transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _currentVelocity, GetEffectiveSmoothTime(), GetEffectiveMaxFollowSpeed(), Time.deltaTime);
                    break;
            }
        }

        private Vector3 ApplyFlightArc(Vector3 targetPosition)
        {
            if (playerTransform == null) return targetPosition;

            float distToPlayer = Vector2.Distance(transform.position, playerTransform.position);

            if (distToPlayer < avoidRadius)
            {
                float force = 1f - (distToPlayer / avoidRadius);
                targetPosition.y += arcHeight * force;
            }

            return targetPosition;
        }

        private void EnterState(MolimoState newState)
        {
            if (_state == MolimoState.HealingPlayer && newState != MolimoState.HealingPlayer && healer != null && healer.IsHealing) healer.CancelHealing();
            _state = newState;
            _stateTimer = 0f;
            _currentVelocity = Vector3.zero;

            if (newState == MolimoState.AttackingEnemy && combat != null) combat.BeginCombat();
            if (newState == MolimoState.HealingPlayer && healer != null) healer.StartHealing();
        }

        public Vector3 GetFollowTargetPosition()
        {
            float currentOffsetX = followOffset.x;

            if (_state == MolimoState.Normal)
            {
                currentOffsetX += idleSideDistance * _currentSide;
            }

            Vector3 baseTarget = playerTransform.position
                                 + playerTransform.right * currentOffsetX
                                 + Vector3.up * followOffset.y;

            if (_sprintBoostBlend > 0.001f && playerMovement != null)
            {
                Vector3 sprintDirection = playerMovement.FacingDirection;
                baseTarget += sprintDirection * (sprintAheadDistance * _sprintBoostBlend);
            }

            // --- SAFE ZONE (NUNCA PARAR NO PIVOT) ---
            // Verifica a distância do alvo calculado em relação ao centro do Player
            Vector3 dirFromPlayer = baseTarget - playerTransform.position;
            dirFromPlayer.z = 0;

            // Se a posição calculada for cair dentro da bolha de colisão, empurra para fora
            if (dirFromPlayer.magnitude < minDistanceFromPlayerPivot)
            {
                // Se estiver exatamente no centro (0,0), usa uma direção padrão (para cima)
                if (dirFromPlayer.magnitude < 0.01f)
                    dirFromPlayer = Vector3.up;

                baseTarget = playerTransform.position + dirFromPlayer.normalized * minDistanceFromPlayerPivot;
            }

            return baseTarget;
        }
    }
}