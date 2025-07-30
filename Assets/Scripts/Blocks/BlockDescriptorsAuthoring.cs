using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class BlockDescriptorAuthoring : MonoBehaviour
    {
        [SerializeField]
        private List<BlockDescriptor> items = new();

        private class Baker : Baker<BlockDescriptorAuthoring>
        {
            public override void Bake(BlockDescriptorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                AddComponentObject
                (
                    entity,
                    new BlockDescriptors
                    {
                        Items = authoring.items
                    }
                );
            }
        }
    }
}
