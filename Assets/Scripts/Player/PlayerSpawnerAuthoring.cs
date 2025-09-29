using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class PlayerSpawnerAuthoring : MonoBehaviour
    {
        [SerializeField]
        private GameObject player;

        private class Baker : Baker<PlayerSpawnerAuthoring>
        {
            public override void Bake(PlayerSpawnerAuthoring authoring)
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
