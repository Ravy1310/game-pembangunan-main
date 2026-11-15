using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyGlade.Building
{
    [Serializable]
    public enum WallStyle
    {
        Plaster,
        Stone
    }

    [Serializable]
    public enum OpeningType
    {
        Window1,
        Window2,
        Door
    }

    [Serializable]
    public class OpeningData
    {
        public int segment;
        public float t; // 0..1 along segment
        public OpeningType type;
    }

    [Serializable]
    public enum RoofType
    {
        Gable,
        Pyramid
    }

    [Serializable]
    public enum RoofMaterial
    {
        RedTile,
        BlueTile,
        Thatch
    }

    [Serializable]
    public class RoofData
    {
        public RoofType type = RoofType.Gable;
        public RoofMaterial material = RoofMaterial.RedTile;
        public float pitch = 35f;
        public float overhang = 0.3f;
    }

    [Serializable]
    public class WallData
    {
        public string id = Guid.NewGuid().ToString("N");
        public List<Vector3> points = new();
        public bool closed;
        public float height = 2.4f;
        public float thickness = 0.28f;
        public WallStyle style = WallStyle.Plaster;
        public List<OpeningData> openings = new();
        public RoofData roof;

        public WallData Clone()
        {
            var clone = new WallData
            {
                id = this.id,
                closed = this.closed,
                height = this.height,
                thickness = this.thickness,
                style = this.style,
                roof = this.roof != null ? new RoofData
                {
                    type = this.roof.type,
                    material = this.roof.material,
                    pitch = this.roof.pitch,
                    overhang = this.roof.overhang
                } : null
            };

            for (int i = 0; i < points.Count; i++)
            {
                clone.points.Add(points[i]);
            }

            for (int i = 0; i < openings.Count; i++)
            {
                clone.openings.Add(new OpeningData
                {
                    segment = openings[i].segment,
                    t = openings[i].t,
                    type = openings[i].type
                });
            }

            return clone;
        }
    }
}
