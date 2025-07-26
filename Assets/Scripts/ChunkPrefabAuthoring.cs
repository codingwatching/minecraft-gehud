using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class ChunkPrefabAuthoring : MonoBehaviour
    {
        [SerializeField]
        private GameObject prefab;

        private class Baker : Baker<ChunkPrefabAuthoring>
        {
            public override void Bake(ChunkPrefabAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new ChunkPrefab
                {
                    Prefab = GetEntity(authoring.prefab, TransformUsageFlags.WorldSpace | TransformUsageFlags.Renderable),
                });
            }
        }
    }
}
