using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    [Serializable]
    public enum PathStyle
    {
        SteppingStones = 0,
        Cobblestone = 1,
        Road = 2
    }

    [Serializable]
    public class PathData
    {
        public string id;
        public List<Vector3> points = new();
        public PathStyle style = PathStyle.SteppingStones;
        public float width = 1f;

        public PathData()
        {
            id = Guid.NewGuid().ToString("N");
        }

        public PathData(string id, IEnumerable<Vector3> pts, PathStyle style = PathStyle.SteppingStones, float width = 1f)
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            this.points = new List<Vector3>(pts);
            this.style = style;
            this.width = width;
        }

        public PathData Clone()
        {
            var clone = new PathData(id, points, style, width);
            return clone;
        }
    }
}
