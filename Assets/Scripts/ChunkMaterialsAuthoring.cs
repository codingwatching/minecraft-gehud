using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class ChunkMaterialsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Material opaqueMaterial;
        [SerializeField]
        private Material transparentMaterial;

        private class Baker : Baker<ChunkMaterialsAuthoring>
        {
            public override void Bake(ChunkMaterialsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new ChunkMaterials
                {
                    OpaqueMaterial = authoring.opaqueMaterial,
                    TransparentMaterial = authoring.transparentMaterial,
                });
            }
        }
    }
}
