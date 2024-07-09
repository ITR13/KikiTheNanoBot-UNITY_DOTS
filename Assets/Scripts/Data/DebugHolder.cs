using Unity.Entities;
using UnityEngine;

namespace Data
{
    public struct DebugHolder : IComponentData
    {
        public UnityObjectRef<Mesh> Mesh;
        public UnityObjectRef<Material> Material;
    }
}