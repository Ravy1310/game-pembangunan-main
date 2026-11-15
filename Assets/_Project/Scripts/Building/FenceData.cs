using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    [Serializable]
    public enum FenceStyle
    {
        WoodPostRail = 0,
        Stone = 1
    }

    [Serializable]
    public class FenceData
    {
        public string id;
        public List<Vector3> points = new();
        public FenceStyle style = FenceStyle.WoodPostRail;
        public bool closed;

        public FenceData()
        {
            id = Guid.NewGuid().ToString("N");
        }

        public FenceData(string id, IEnumerable<Vector3> pts, FenceStyle style = FenceStyle.WoodPostRail, bool closed = false)
        {
            this.id = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
            this.points = new List<Vector3>(pts);
            this.style = style;
            this.closed = closed;
        }

        public FenceData Clone()
        {
            var clone = new FenceData(id, points, style, closed);
            return clone;
        }
    }
}
