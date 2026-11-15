using UnityEngine;
using CozyGlade.Core;
using CozyGlade.World;

namespace CozyGlade.Building
{
    /// <summary>
    /// Command untuk menempatkan prefab rumah low-poly di dunia.
    /// Mendukung Undo (hapus rumah) dan Redo (spawn ulang).
    /// </summary>
    public class BuildHouseCommand : ICommand
    {
        private readonly WorldData  _worldData;
        private readonly PropData   _propData;
        private readonly GameObject _prefab;

        private GameObject _spawnedObject;

        public BuildHouseCommand(WorldData worldData, PropData propData, GameObject prefab)
        {
            _worldData = worldData;
            _propData  = propData;
            _prefab    = prefab;
        }

        public void Do()
        {
            if (_prefab == null) return;

            _spawnedObject = Object.Instantiate(
                _prefab,
                _propData.position,
                _propData.rotation);

            _spawnedObject.name = $"House_{_prefab.name}_{_propData.id.Substring(0, 6)}";

            if (_worldData != null)
                _worldData.AddProp(_propData);
        }

        public void Undo()
        {
            if (_spawnedObject != null)
            {
                Object.Destroy(_spawnedObject);
                _spawnedObject = null;
            }

            if (_worldData != null)
                _worldData.RemoveProp(_propData.id);
        }
    }
}
