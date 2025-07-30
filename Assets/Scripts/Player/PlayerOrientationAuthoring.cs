using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class PlayerOrientationAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerOrientationAuthoring>
        {
            public override void Bake(PlayerOrientationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerOrientation>(entity);
            }
        }
    }
}
