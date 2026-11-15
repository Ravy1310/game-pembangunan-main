using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Npc
{
    /// <summary>Villager driven by a small behaviour tree: idle, wander, and chat with other villagers.</summary>
    [RequireComponent(typeof(Animator))]
    public class NpcActor : MonoBehaviour
    {
        public float walkSpeed = 1.15f;
        public float turnSpeed = 6f;
        public float wanderRadius = 4.5f;
        public float socialRadius = 12f;
        public Vector2 idleTime = new Vector2(3f, 7f);
        public Vector2 chatTime = new Vector2(3.5f, 6f);
        public LayerMask obstacleMask = ~0;

        public string Activity { get; private set; } = "Idle";
        public float WalkSpeed => walkSpeed;
        public bool IsChatting => Time.time < _chatUntil;
        public bool Blocked { get; private set; }
        public Vector3 ChatFocus => _chatFocus;
        public Vector3 Home => _home;

        static readonly List<NpcActor> Villagers = new List<NpcActor>();
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int TalkingHash = Animator.StringToHash("Talking");
        static readonly int IdleVariantHash = Animator.StringToHash("IdleVariant");

        Animator _animator;
        BehaviourNode _root;
        Vector3 _home;
        Vector3 _chatFocus;
        float _chatUntil;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _home = transform.position;
            _root = NpcBehaviour.Build(this);
        }

        void OnEnable() => Villagers.Add(this);
        void OnDisable() => Villagers.Remove(this);

        void Update() => _root.Tick();

        public void SetActivity(string activity) => Activity = activity;

        public void SetLocomotion(float speed) => _animator.SetFloat(SpeedHash, speed);

        public void SetTalking(bool talking) => _animator.SetBool(TalkingHash, talking);

        public void SetIdleVariant(int variant) => _animator.SetInteger(IdleVariantHash, variant);

        /// <summary>Moves toward the target; returns true when close enough. Blocked flags a dead end.</summary>
        public bool MoveTowards(Vector3 target, float speed)
        {
            Blocked = false;

            Vector3 to = target - transform.position;
            to.y = 0f;

            if (to.sqrMagnitude <= 0.09f)
            {
                SetLocomotion(0f);
                return true;
            }

            Vector3 direction = to.normalized;
            if (BlockedAhead(direction))
            {
                Blocked = true;
                SetLocomotion(0f);
                return false;
            }

            Quaternion look = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);

            Vector3 next = transform.position + direction * speed * Time.deltaTime;
            if (TryGroundHeight(next, out float ground)) next.y = ground;
            transform.position = next;

            SetLocomotion(speed);
            return false;
        }

        public void FaceTowards(Vector3 point)
        {
            Vector3 to = point - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;

            Quaternion look = Quaternion.LookRotation(to.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
        }

        public Vector3 RandomPointNearHome()
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 offset = Random.insideUnitCircle * wanderRadius;
                Vector3 probe = _home + new Vector3(offset.x, 0f, offset.y);

                if (!TryGroundHeight(probe, out float ground)) continue;

                Vector3 point = new Vector3(probe.x, ground, probe.z);
                if (Physics.CheckSphere(point + Vector3.up * 0.8f, 0.35f, obstacleMask, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.Linecast(transform.position + Vector3.up * 0.9f, point + Vector3.up * 0.9f, obstacleMask, QueryTriggerInteraction.Ignore)) continue;

                return point;
            }

            return _home;
        }

        public NpcActor FindChatPartner(float radius)
        {
            NpcActor best = null;
            float bestSqr = radius * radius;

            foreach (NpcActor other in Villagers)
            {
                if (other == null || other == this || other.IsChatting) continue;

                float sqr = (other.transform.position - transform.position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = other;
                }
            }

            return best;
        }

        public void InviteChat(Vector3 focus, float duration)
        {
            if (IsChatting) return;
            _chatFocus = focus;
            _chatUntil = Time.time + duration;
        }

        bool BlockedAhead(Vector3 direction)
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            if (!Physics.SphereCast(origin, 0.3f, direction, out RaycastHit hit, 0.8f, obstacleMask, QueryTriggerInteraction.Ignore))
                return false;

            return !hit.collider.transform.IsChildOf(transform);
        }

        bool TryGroundHeight(Vector3 point, out float ground)
        {
            Vector3 origin = point + Vector3.up * 3f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 8f, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                ground = hit.point.y;
                return true;
            }

            ground = point.y;
            return false;
        }
    }
}
