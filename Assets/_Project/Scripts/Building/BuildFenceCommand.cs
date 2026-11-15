using CozyGlade.Core;
using CozyGlade.World;
using UnityEngine;

namespace CozyGlade.Building
{
    public class BuildFenceCommand : ICommand
    {
        private readonly WorldData _worldData;
        private readonly WorldView _worldView;
        private readonly FenceData _fenceData;

        public BuildFenceCommand(WorldData worldData, WorldView worldView, FenceData fenceData)
        {
            _worldData = worldData;
            _worldView = worldView;
            _fenceData = fenceData;
        }

        public void Do()
        {
            if (_worldData == null || _fenceData == null) return;
            _worldData.AddFence(_fenceData);
            if (_worldView != null)
            {
                _worldView.SpawnFenceVisual(_fenceData, true);
            }
        }

        public void Undo()
        {
            if (_worldData == null || _fenceData == null) return;
            _worldData.RemoveFence(_fenceData.id);
            if (_worldView != null)
            {
                _worldView.DestroyFenceVisual(_fenceData.id, true);
            }
        }
    }
}
