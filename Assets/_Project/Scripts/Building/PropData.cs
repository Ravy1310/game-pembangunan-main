using System;
using UnityEngine;

namespace CozyGlade.Building
{
    [Serializable]
    public class PropData
    {
        public string id;
        public string prefabName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale = Vector3.one;

        public PropData()
        {
            id = Guid.NewGuid().ToString("N");
        }

        public PropData(string id, string prefabName, Vector3 pos, Quaternion rot, Vector3 scl)
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            this.prefabName = prefabName;
            this.position = pos;
            this.rotation = rot;
            this.scale = scl;
        }

        public PropData Clone()
        {
            return new PropData(id, prefabName, position, rotation, scale);
        }
    }
}
