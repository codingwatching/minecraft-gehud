using Unity.Entities;
using UnityEngine;

namespace Minecraft
{
    [DisallowMultipleComponent]
    public class CubeSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField]
        private GameObject player;

        private class Baker : Baker<CubeSpawnerAuthoring>
        {
            public override void Bake(CubeSpawnerAuthoring authoring)
            {
                var spawner = new PlayerSpawner
                {
                    Player = GetEntity(authoring.player, TransformUsageFlags.Dynamic)
                };

                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent(entity, spawner);
            }
        }
    }
}
