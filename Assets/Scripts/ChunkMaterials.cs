using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    public class ChunkMaterials : IComponentData
    {
        public Material OpaqueMaterial;
        public Material TransparentMaterial;
    }
}
