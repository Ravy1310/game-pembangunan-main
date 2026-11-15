using System.Collections.Generic;
using CozyGlade.Core;
using CozyGlade.World;

namespace CozyGlade.Building
{
    public class ModifyWallOpeningsCommand : ICommand
    {
        private readonly WallData _wall;
        private readonly List<OpeningData> _previousOpenings = new();
        private readonly List<OpeningData> _newOpenings = new();

        public ModifyWallOpeningsCommand(WallData wall, List<OpeningData> newOpenings)
        {
            _wall = wall;

            if (_wall != null)
            {
                for (int i = 0; i < _wall.openings.Count; i++)
                {
                    var op = _wall.openings[i];
                    _previousOpenings.Add(new OpeningData { segment = op.segment, t = op.t, type = op.type });
                }
            }

            if (newOpenings != null)
            {
                for (int i = 0; i < newOpenings.Count; i++)
                {
                    var op = newOpenings[i];
                    _newOpenings.Add(new OpeningData { segment = op.segment, t = op.t, type = op.type });
                }
            }
        }

        public void Do()
        {
            if (_wall == null) return;

            _wall.openings.Clear();
            for (int i = 0; i < _newOpenings.Count; i++)
            {
                var op = _newOpenings[i];
                _wall.openings.Add(new OpeningData { segment = op.segment, t = op.t, type = op.type });
            }

            WorldView.Instance?.SpawnWallVisual(_wall, false);
        }

        public void Undo()
        {
            if (_wall == null) return;

            _wall.openings.Clear();
            for (int i = 0; i < _previousOpenings.Count; i++)
            {
                var op = _previousOpenings[i];
                _wall.openings.Add(new OpeningData { segment = op.segment, t = op.t, type = op.type });
            }

            WorldView.Instance?.SpawnWallVisual(_wall, false);
        }
    }
}
