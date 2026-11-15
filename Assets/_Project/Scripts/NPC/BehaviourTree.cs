using System;
using System.Collections.Generic;

namespace CozyGlade.Npc
{
    public enum NodeState { Running, Success, Failure }

    public abstract class BehaviourNode
    {
        protected readonly NpcActor Actor;

        protected BehaviourNode(NpcActor actor) => Actor = actor;

        public NodeState State { get; protected set; } = NodeState.Running;

        public abstract NodeState Tick();
        public virtual void Reset() => State = NodeState.Running;
    }

    public class SequenceNode : BehaviourNode
    {
        readonly List<BehaviourNode> _children;
        int _index;

        public SequenceNode(NpcActor actor, params BehaviourNode[] children) : base(actor)
        {
            _children = new List<BehaviourNode>(children);
        }

        public override NodeState Tick()
        {
            while (_index < _children.Count)
            {
                NodeState result = _children[_index].Tick();
                if (result == NodeState.Running)
                {
                    State = NodeState.Running;
                    return State;
                }
                if (result == NodeState.Failure)
                {
                    Reset();
                    State = NodeState.Failure;
                    return State;
                }
                _index++;
            }

            Reset();
            State = NodeState.Success;
            return State;
        }

        public override void Reset()
        {
            _index = 0;
            foreach (BehaviourNode child in _children) child.Reset();
            State = NodeState.Running;
        }
    }

    public class SelectorNode : BehaviourNode
    {
        readonly List<BehaviourNode> _children;
        int _index;

        public SelectorNode(NpcActor actor, params BehaviourNode[] children) : base(actor)
        {
            _children = new List<BehaviourNode>(children);
        }

        public override NodeState Tick()
        {
            while (_index < _children.Count)
            {
                NodeState result = _children[_index].Tick();
                if (result == NodeState.Running)
                {
                    State = NodeState.Running;
                    return State;
                }
                if (result == NodeState.Success)
                {
                    Reset();
                    State = NodeState.Success;
                    return State;
                }
                _index++;
            }

            Reset();
            State = NodeState.Failure;
            return State;
        }

        public override void Reset()
        {
            _index = 0;
            foreach (BehaviourNode child in _children) child.Reset();
            State = NodeState.Running;
        }
    }

    public class RepeatNode : BehaviourNode
    {
        readonly BehaviourNode _child;

        public RepeatNode(NpcActor actor, BehaviourNode child) : base(actor) => _child = child;

        public override NodeState Tick()
        {
            _child.Tick();
            if (_child.State != NodeState.Running) _child.Reset();
            State = NodeState.Running;
            return State;
        }

        public override void Reset()
        {
            _child.Reset();
            State = NodeState.Running;
        }
    }

    public class ConditionNode : BehaviourNode
    {
        readonly Func<NpcActor, bool> _test;

        public ConditionNode(NpcActor actor, Func<NpcActor, bool> test) : base(actor) => _test = test;

        public override NodeState Tick() => State = _test(Actor) ? NodeState.Success : NodeState.Failure;
    }

    public class TaskNode : BehaviourNode
    {
        readonly NpcTask _task;
        bool _active;

        public TaskNode(NpcActor actor, NpcTask task) : base(actor) => _task = task;

        public override NodeState Tick()
        {
            if (!_active)
            {
                _task.Begin(Actor);
                _active = true;
            }

            NodeState result = _task.Tick(Actor);
            if (result != NodeState.Running)
            {
                _task.End(Actor);
                _active = false;
            }

            State = result;
            return result;
        }

        public override void Reset()
        {
            if (_active)
            {
                _task.Cancel(Actor);
                _active = false;
            }
            State = NodeState.Running;
        }
    }

    public class InterruptNode : BehaviourNode
    {
        readonly Func<NpcActor, bool> _condition;
        readonly BehaviourNode _interrupt;
        readonly BehaviourNode _fallback;

        public InterruptNode(NpcActor actor, Func<NpcActor, bool> condition, BehaviourNode interrupt, BehaviourNode fallback) : base(actor)
        {
            _condition = condition;
            _interrupt = interrupt;
            _fallback = fallback;
        }

        public override NodeState Tick()
        {
            bool interrupting = _condition(Actor);
            BehaviourNode active = interrupting ? _interrupt : _fallback;
            BehaviourNode idle = interrupting ? _fallback : _interrupt;

            idle.Reset();
            active.Tick();

            State = active.State;
            return State;
        }

        public override void Reset()
        {
            _interrupt.Reset();
            _fallback.Reset();
            State = NodeState.Running;
        }
    }

    public abstract class NpcTask
    {
        public virtual void Begin(NpcActor actor) { }
        public abstract NodeState Tick(NpcActor actor);
        public virtual void End(NpcActor actor) { }
        public virtual void Cancel(NpcActor actor) => End(actor);
    }

    public static class NpcBehaviour
    {
        public static BehaviourNode Build(NpcActor actor)
        {
            BehaviourNode activities = new RepeatNode(actor, new SelectorNode(actor,
                new TaskNode(actor, new SocializeTask()),
                new TaskNode(actor, new WanderTask()),
                new TaskNode(actor, new IdleTask(actor.idleTime))));

            return new InterruptNode(actor, a => a.IsChatting, new TaskNode(actor, new ChatListenTask()), activities);
        }
    }
}
