using Unity.Burst;
using Unity.Entities;

namespace Voxilarium
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkBufferingSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new ChunkBuffer
            {
                Height = 16
            });

            var requestEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(requestEntity, new ChunkBufferingDistanceRequest
            {
                NewDistance = 3,
            });
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var buffer = state.EntityManager.GetComponentDataRW<ChunkBuffer>(state.SystemHandle);

            foreach (var (request, entity) in SystemAPI.Query<RefRO<ChunkBufferingDistanceRequest>>().WithEntityAccess())
            {
                buffer.ValueRW.UpdateDistance(request.ValueRO.NewDistance);
                commandBuffer.DestroyEntity(entity);
            }

            foreach (var (request, entity) in SystemAPI.Query<RefRO<ChunkBufferingCenterRequest>>().WithEntityAccess())
            {
                buffer.ValueRW.UpdateCenter(request.ValueRO.NewCenter, commandBuffer);
                commandBuffer.DestroyEntity(entity);
            }
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            foreach (var chunkBuffer in SystemAPI.Query<RefRO<ChunkBuffer>>())
            {
                chunkBuffer.ValueRO.Dispose();
            }
        }
    }
}
