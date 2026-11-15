using System;
using System.Collections.Generic;
using UnityEngine;
using CozyGlade.Building;

namespace CozyGlade.World
{
    [Serializable]
    public class WorldData
    {
        public string version = "1.0.0";
        public List<WallData> walls = new();
        public List<FenceData> fences = new();
        public List<PathData> paths = new();
        public List<PropData> props = new();

        // --- Walls ---
        public void AddWall(WallData wall)
        {
            if (wall == null) return;
            if (!walls.Exists(w => w.id == wall.id))
            {
                walls.Add(wall);
            }
        }

        public bool RemoveWall(string wallId)
        {
            return walls.RemoveAll(w => w.id == wallId) > 0;
        }

        public WallData GetWall(string wallId)
        {
            return walls.Find(w => w.id == wallId);
        }

        // --- Fences ---
        public void AddFence(FenceData fence)
        {
            if (fence == null) return;
            if (!fences.Exists(f => f.id == fence.id))
            {
                fences.Add(fence);
            }
        }

        public bool RemoveFence(string fenceId)
        {
            return fences.RemoveAll(f => f.id == fenceId) > 0;
        }

        public FenceData GetFence(string fenceId)
        {
            return fences.Find(f => f.id == fenceId);
        }

        // --- Paths ---
        public void AddPath(PathData path)
        {
            if (path == null) return;
            if (!paths.Exists(p => p.id == path.id))
            {
                paths.Add(path);
            }
        }

        public bool RemovePath(string pathId)
        {
            return paths.RemoveAll(p => p.id == pathId) > 0;
        }

        public PathData GetPath(string pathId)
        {
            return paths.Find(p => p.id == pathId);
        }

        // --- Props ---
        public void AddProp(PropData prop)
        {
            if (prop == null) return;
            if (!props.Exists(p => p.id == prop.id))
            {
                props.Add(prop);
            }
        }

        public bool RemoveProp(string propId)
        {
            return props.RemoveAll(p => p.id == propId) > 0;
        }

        public PropData GetProp(string propId)
        {
            return props.Find(p => p.id == propId);
        }

        // --- Clear ---
        public void Clear()
        {
            walls.Clear();
            fences.Clear();
            paths.Clear();
            props.Clear();
        }
    }
}
