using UnityEngine;

namespace CozyGlade.Npc
{
    public class IdleTask : NpcTask
    {
        readonly Vector2 _duration;
        float _until;

        public IdleTask(Vector2 duration) => _duration = duration;

        public override void Begin(NpcActor actor)
        {
            actor.SetActivity("Idle");
            actor.SetTalking(false);
            actor.SetLocomotion(0f);
            actor.SetIdleVariant(Random.value < 0.5f ? 0 : 1);
            _until = Time.time + Random.Range(_duration.x, _duration.y);
        }

        public override NodeState Tick(NpcActor actor) =>
            Time.time >= _until ? NodeState.Success : NodeState.Running;
    }

    public class WanderTask : NpcTask
    {
        Vector3 _target;
        float _retryAt;
        int _attempts;

        public override void Begin(NpcActor actor)
        {
            actor.SetActivity("Wander");
            actor.SetTalking(false);
            _attempts = 0;
            PickTarget(actor);
        }

        void PickTarget(NpcActor actor)
        {
            _target = actor.RandomPointNearHome();
            _retryAt = Time.time + 10f;
        }

        public override NodeState Tick(NpcActor actor)
        {
            if (actor.MoveTowards(_target, actor.WalkSpeed)) return NodeState.Success;

            if (actor.Blocked || Time.time >= _retryAt)
            {
                _attempts++;
                if (_attempts >= 4) return NodeState.Failure;
                PickTarget(actor);
            }

            return NodeState.Running;
        }

        public override void End(NpcActor actor) => actor.SetLocomotion(0f);
        public override void Cancel(NpcActor actor) => actor.SetLocomotion(0f);
    }

    public class SocializeTask : NpcTask
    {
        NpcActor _partner;
        bool _talking;
        float _chatUntil;
        float _deadline;

        public override void Begin(NpcActor actor)
        {
            actor.SetTalking(false);
            actor.SetLocomotion(0f);
            _talking = false;
            _partner = Random.value < 0.5f ? null : actor.FindChatPartner(actor.socialRadius);
            _deadline = Time.time + 18f;
            actor.SetActivity(_partner != null ? "Visit " + _partner.name : "Alone");
        }

        public override NodeState Tick(NpcActor actor)
        {
            if (_partner == null || Time.time >= _deadline) return NodeState.Failure;

            Vector3 to = _partner.transform.position - actor.transform.position;
            to.y = 0f;

            if (to.magnitude > 1.8f)
            {
                if (actor.MoveTowards(_partner.transform.position, actor.WalkSpeed)) return NodeState.Running;
                if (actor.Blocked) return NodeState.Failure;

                return NodeState.Running;
            }

            actor.SetLocomotion(0f);
            actor.FaceTowards(_partner.transform.position);

            if (!_talking)
            {
                if (_partner.IsChatting)
                {
                    _partner = actor.FindChatPartner(actor.socialRadius);
                    return _partner != null ? NodeState.Running : NodeState.Failure;
                }

                _talking = true;
                actor.SetActivity("Chat");
                float duration = Random.Range(actor.chatTime.x, actor.chatTime.y);
                _chatUntil = Time.time + duration;
                _partner.InviteChat(actor.transform.position, duration);
            }

            actor.SetTalking(true);
            return Time.time >= _chatUntil ? NodeState.Success : NodeState.Running;
        }

        public override void End(NpcActor actor)
        {
            actor.SetTalking(false);
            actor.SetLocomotion(0f);
        }

        public override void Cancel(NpcActor actor) => End(actor);
    }

    public class ChatListenTask : NpcTask
    {
        public override void Begin(NpcActor actor)
        {
            actor.SetActivity("Listen");
            actor.SetLocomotion(0f);
            actor.SetTalking(true);
        }

        public override NodeState Tick(NpcActor actor)
        {
            actor.SetLocomotion(0f);
            actor.SetTalking(true);
            actor.FaceTowards(actor.ChatFocus);
            return actor.IsChatting ? NodeState.Running : NodeState.Success;
        }

        public override void End(NpcActor actor) => actor.SetTalking(false);
        public override void Cancel(NpcActor actor) => End(actor);
    }
}
