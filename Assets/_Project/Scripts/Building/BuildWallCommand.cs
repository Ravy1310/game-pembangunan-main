using CozyGlade.Core;
using CozyGlade.World;

namespace CozyGlade.Building
{
    public class BuildWallCommand : ICommand
    {
        private readonly WallData _wallData;
        private readonly WorldData _worldData;

        public WallData Wall => _wallData;

        public BuildWallCommand(WallData wallData, WorldData worldData)
        {
            _wallData = wallData;
            _worldData = worldData;
        }

        public void Do()
        {
            if (_worldData != null && _wallData != null)
            {
                _worldData.AddWall(_wallData);
            }

            if (WorldView.Instance != null && _wallData != null)
            {
                WorldView.Instance.SpawnWallVisual(_wallData, true);
            }
        }

        public void Undo()
        {
            if (_worldData != null && _wallData != null)
            {
                _worldData.RemoveWall(_wallData.id);
            }

            if (WorldView.Instance != null && _wallData != null)
            {
                WorldView.Instance.DestroyWallVisual(_wallData.id, true);
            }
        }
    }
}
