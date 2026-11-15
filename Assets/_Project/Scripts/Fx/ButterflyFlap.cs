using UnityEngine;

namespace CozyGlade.Fx
{
    /// <summary>Flaps the two wing child objects (names end with "wingL"/"wingR") of fx_butterfly.</summary>
    public class ButterflyFlap : MonoBehaviour
    {
        public float flapSpeed = 18f;
        public float flapAngle = 38f;
        Transform _l, _r;
        float _phase;

        void Awake()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith("wingL")) _l = t;
                else if (t.name.EndsWith("wingR")) _r = t;
            }
            _phase = Random.value * 6.28f;
        }

        void Update()
        {
            float a = (Mathf.Sin(Time.time * flapSpeed + _phase) + 0.3f) * flapAngle;
            if (_r) _r.localRotation = Quaternion.Euler(0, 0, a);
            if (_l) _l.localRotation = Quaternion.Euler(0, 0, -a);
        }
    }
}
