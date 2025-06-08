using Unity.Entities;
using UnityEngine;

namespace Minecraft
{
    [DisallowMultipleComponent]
    public class PlayerAuthoring : MonoBehaviour
    {
        private class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Cube>(entity);
            }
        }
    }
}
