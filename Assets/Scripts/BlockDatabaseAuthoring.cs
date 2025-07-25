using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Minecraft
{
    [DisallowMultipleComponent]
    public class BlockDatabaseAuthoring : MonoBehaviour
    {
        [SerializeField]
        private List<BlockSettings> data = new();

        private class Baker : Baker<BlockDatabaseAuthoring>
        {
            public override void Bake(BlockDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new BlockDatabase
                {
                    Data = authoring.data
                });
            }
        }
    }
}
