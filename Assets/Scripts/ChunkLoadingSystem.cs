using Minecraft.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using static Unity.Entities.SystemAPI;

namespace Minecraft
{
    [BurstCompile]
    [UpdateAfter(typeof(ChunkBufferingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkLoadingSystem : ISystem
    {
        private static void UpdateLoading(ref SystemState state, ref ChunkBuffer buffer, in int2 column, int height, int distance, in EntityCommandBuffer commandBuffer)
        {
            var startX = column.x - distance - ChunkBuffer.BufferDistance;
            var endX = column.x + distance + ChunkBuffer.BufferDistance;
            var startZ = column.y - distance - ChunkBuffer.BufferDistance;
            var endZ = column.y + distance + ChunkBuffer.BufferDistance;

            for (var x = startX; x <= endX; x++)
            {
                for (var z = startZ; z <= endZ; z++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        var chunkCoordinate = new int3(x, y, z);

                        bool isRendered = x != startX && x != endX && z != startZ && z != endZ;

                        buffer.GetEntity(chunkCoordinate, out var chunkEntity);
                        if (chunkEntity != Entity.Null)
                        {
                            if (isRendered)
                            {
                                ChunkUtility.ShowChunk(state.EntityManager, commandBuffer, chunkEntity);
                            }
                            else
                            {
                                ChunkUtility.HideChunk(state.EntityManager, commandBuffer, chunkEntity);
                            }
                        }
                        else
                        {
                            var newChunkEntity = state.EntityManager.CreateEntity();
                            commandBuffer.AddComponent(newChunkEntity, new ChunkSpawnRequest
                            {
                                Coordinate = chunkCoordinate,
                                IsVisible = isRendered
                            });

                            var index = buffer.ToIndex(chunkCoordinate);
                            buffer.Chunks[index] = newChunkEntity;
                        }
                    }
                }
            }
        }

        [BurstCompile]
        readonly void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<LastLoadingColumn>(state.SystemHandle);
            state.EntityManager.AddComponent<ChunkReloadingRequest>(state.EntityManager.CreateEntity());
        }

        [BurstCompile]
        readonly void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var buffer = GetSingletonRW<ChunkBuffer>();
            var lastLoadingColumn = state.EntityManager.GetComponentData<LastLoadingColumn>(state.SystemHandle).Value;

            foreach (var (_, entity) in Query<ChunkReloadingRequest>().WithEntityAccess())
            {
                var requestEntity = state.EntityManager.CreateEntity();
                commandBuffer.AddComponent(requestEntity, new ChunkLoadingRequest
                {
                    NewCenter = lastLoadingColumn
                });

                commandBuffer.DestroyEntity(entity);
            }

            foreach (var (request, entity) in Query<RefRO<ChunkLoadingRequest>>().WithEntityAccess())
            {
                buffer.ValueRW.UpdateCenter(request.ValueRO.NewCenter, commandBuffer);
                UpdateLoading(ref state, ref buffer.ValueRW, request.ValueRO.NewCenter, buffer.ValueRO.Height, buffer.ValueRO.Distance, commandBuffer);
                commandBuffer.DestroyEntity(entity);
            }
        }
    }
}
