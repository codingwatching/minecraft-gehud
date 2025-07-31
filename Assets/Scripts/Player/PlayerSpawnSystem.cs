using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Voxilarium
{
    public struct PlayerGroundRequest : IComponentData
    {
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerSpawnSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Player>();
            state.RequireForUpdate<Chunks>();
            state.RequireForUpdate<Blocks>();
            state.RequireForUpdate<PlayerGroundRequest>();
            state.EntityManager.CreateSingleton<PlayerGroundRequest>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var chunks = SystemAPI.GetSingletonRW<Chunks>();
            var blocks = SystemAPI.GetSingletonRW<Blocks>();

            for (var y = chunks.ValueRO.Height * Chunk.Size - 1; y >= 0; y--)
            {
                var coordinate = new int3(0, y, 0);
                var voxel = chunks.ValueRO.GetVoxel(state.EntityManager, coordinate);
                if (blocks.ValueRO[voxel.Block].IsSolid)
                {
                    coordinate.y += 1;
                    foreach (var position in SystemAPI
                        .Query<RefRW<HitboxPosition>>()
                        .WithAll<Player, GhostOwnerIsLocal>())
                    {
                        position.ValueRW.Value = coordinate;
                    }

                    commandBuffer.DestroyEntity(SystemAPI.GetSingletonEntity<PlayerGroundRequest>());

                    break;
                }
            }
        }
    }
}
