using UnityEngine;

namespace CozyGlade.Tools
{
    public interface ITool
    {
        string Id { get; }
        void OnEnter();
        void OnExit();
        void Tick(Vector3 pointerWorld);
        void OnPressed(Vector3 pointerWorld);
        void OnReleased(Vector3 pointerWorld);
        void OnCancel();
    }
}
