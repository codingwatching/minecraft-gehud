using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class PlayerViewAuthoring : MonoBehaviour
    {
        [SerializeField]
        private GameObject entity;
        private class Baker : Baker<PlayerViewAuthoring>
        {
            public override void Bake(PlayerViewAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent
                (
                    entity,
                    new PlayerView
                    {
                        Entity = GetEntity(authoring.entity, TransformUsageFlags.Dynamic)
                    }
                );
            }
        }
    }
}
