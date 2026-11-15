using UnityEngine;

namespace CozyGlade.Fx
{
    /// <summary>Random wandering around the start position. Ground creatures graze/pause; flying ones bob up and down.</summary>
    public class SimpleWander : MonoBehaviour
    {
        public float radius = 4f;
        public float speed = 0.5f;
        public float turnSpeed = 3f;
        public Vector2 pauseRange = new Vector2(1.5f, 5f);
        public bool flying;
        public float bobAmplitude = 0.2f;
        public float bobSpeed = 2f;

        Vector3 _origin, _target;
        float _pause;

        void Start() { _origin = transform.position; PickTarget(); }

        void PickTarget()
        {
            Vector2 c = Random.insideUnitCircle * radius;
            _target = _origin + new Vector3(c.x, 0, c.y);
            _pause = flying ? 0f : Random.Range(pauseRange.x, pauseRange.y);
        }

        void Update()
        {
            if (_pause > 0f) { _pause -= Time.deltaTime; return; }
            Vector3 to = _target - transform.position; to.y = 0;
            if (to.magnitude < 0.12f) { PickTarget(); return; }
            Quaternion look = Quaternion.LookRotation(to.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
            Vector3 p = transform.position + transform.forward * speed * Time.deltaTime;
            if (flying) p.y = _origin.y + Mathf.Sin(Time.time * bobSpeed + _origin.x) * bobAmplitude;
            transform.position = p;
        }
    }
}
