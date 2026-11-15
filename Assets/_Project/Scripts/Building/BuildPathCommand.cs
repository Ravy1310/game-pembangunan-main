using CozyGlade.Core;
using CozyGlade.World;
using UnityEngine;

namespace CozyGlade.Building
{
    public class BuildPathCommand : ICommand
    {
        private readonly WorldData _worldData;
        private readonly WorldView _worldView;
        private readonly PathData _pathData;

        public BuildPathCommand(WorldData worldData, WorldView worldView, PathData pathData)
        {
            _worldData = worldData;
            _worldView = worldView;
            _pathData = pathData;
        }

        public void Do()
        {
            if (_worldData == null || _pathData == null) return;
            _worldData.AddPath(_pathData);
            if (_worldView != null)
            {
                _worldView.SpawnPathVisual(_pathData, true);
            }
        }

        public void Undo()
        {
            if (_worldData == null || _pathData == null) return;
            _worldData.RemovePath(_pathData.id);
            if (_worldView != null)
            {
                _worldView.DestroyPathVisual(_pathData.id, true);
            }
        }
    }
}
