using Voxilarium.Utilities;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;

namespace Voxilarium
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

                        bool isVisible = x != startX && x != endX && z != startZ && z != endZ;

                        var chunkEntity = buffer.GetEntity(chunkCoordinate);
                        if (chunkEntity != Entity.Null)
                        {
                            if (isVisible)
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
                            var newChunkEntity = ChunkUtility.SpawnChunk(state.EntityManager, chunkCoordinate, isVisible);
                            var index = buffer.ToIndex(chunkCoordinate);
                            buffer.Chunks[index] = newChunkEntity;
                        }
                    }
                }
            }
        }

        private EntityQuery loadingRequests;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<LastLoadingColumn>(state.SystemHandle);
            state.EntityManager.AddComponent<ChunkReloadingRequest>(state.EntityManager.CreateEntity());

            loadingRequests = SystemAPI.QueryBuilder().WithAll<ChunkLoadingRequest>().Build();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var buffer = SystemAPI.GetSingletonRW<ChunkBuffer>();
            var lastLoadingColumn = state.EntityManager.GetComponentDataRW<LastLoadingColumn>(state.SystemHandle);

            foreach (var playerTransform in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<Player, GhostOwnerIsLocal>())
            {
                var chunkCoordinate = CoordinateUtility.ToChunk(new int3(playerTransform.ValueRO.Position));

                var column = new int2(chunkCoordinate.x, chunkCoordinate.z);

                if (column.x != lastLoadingColumn.ValueRO.Value.x && column.y != lastLoadingColumn.ValueRO.Value.y)
                {
                    var requestEntity = commandBuffer.CreateEntity();

                    commandBuffer.AddComponent
                    (
                        requestEntity,
                        new ChunkLoadingRequest
                        {
                            NewCenter = column
                        }
                    );

                    lastLoadingColumn.ValueRW.Value = column;
                }
            }

            foreach (var (_, entity) in SystemAPI.Query<ChunkReloadingRequest>().WithEntityAccess())
            {
                var requestEntity = commandBuffer.CreateEntity();

                commandBuffer.AddComponent(requestEntity, new ChunkLoadingRequest
                {
                    NewCenter = lastLoadingColumn.ValueRO.Value
                });

                commandBuffer.DestroyEntity(entity);
            }

            var loadingRquestEntities = loadingRequests.ToEntityArray(Allocator.Temp);

            foreach (var entity in loadingRquestEntities)
            {
                var request = state.EntityManager.GetComponentData<ChunkLoadingRequest>(entity);

                buffer.ValueRW.UpdateCenter(request.NewCenter, commandBuffer);
                UpdateLoading(ref state, ref buffer.ValueRW, request.NewCenter, buffer.ValueRO.Height, buffer.ValueRO.Distance, commandBuffer);
                commandBuffer.DestroyEntity(entity);
            }

            loadingRquestEntities.Dispose();
        }
    }
}
