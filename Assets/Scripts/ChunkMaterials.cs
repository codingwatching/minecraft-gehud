using Unity.Entities;
using UnityEngine;

namespace Minecraft
{
    public class ChunkMaterials : IComponentData
    {
        public Material OpaqueMaterial;
        public Material TransparentMaterial;
    }
}
