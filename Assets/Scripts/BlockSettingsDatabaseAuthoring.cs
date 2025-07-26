using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class BlockSettingsDatabaseAuthoring : MonoBehaviour
    {
        [SerializeField]
        private List<BlockSettings> data = new();

        private class Baker : Baker<BlockSettingsDatabaseAuthoring>
        {
            public override void Bake(BlockSettingsDatabaseAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new BlockSettingsDatabase
                {
                    Data = authoring.data
                });
            }
        }
    }
}
