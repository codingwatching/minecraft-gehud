using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class PlayerInputAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerInputAuthoring>
        {
            public override void Bake(PlayerInputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerInput>(entity);
            }
        }
    }
}
