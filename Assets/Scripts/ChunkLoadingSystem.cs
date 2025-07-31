using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
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
        public const int ColumnBatchSize = 1;

        private EntityQuery loadingRequests;

        void ISystem.OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponent<LastLoadingColumn>(state.SystemHandle);
            state.EntityManager.AddComponentData(state.SystemHandle, new ChunkColumnLoadingQueue
            {
                Value = new(Allocator.Persistent),
            });
            state.EntityManager.AddComponent<ChunkReloadingRequest>(state.EntityManager.CreateEntity());

            loadingRequests = SystemAPI.QueryBuilder().WithAll<ChunkLoadingRequest>().Build();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var lastLoadingColumn = state.EntityManager.GetComponentDataRW<LastLoadingColumn>(state.SystemHandle);

            foreach (var playerTransform in SystemAPI.Query<RefRO<LocalToWorld>>().WithAll<Player, GhostOwnerIsLocal>())
            {
                var chunkCoordinate = CoordinateUtility.ToChunk(new int3(playerTransform.ValueRO.Position));

                var column = new int2(chunkCoordinate.x, chunkCoordinate.z);

                if (column.x != lastLoadingColumn.ValueRO.Value.x || column.y != lastLoadingColumn.ValueRO.Value.y)
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

            var loadingQueue = state.EntityManager.GetComponentData<ChunkColumnLoadingQueue>(state.SystemHandle);

            var loadingRquestEntities = loadingRequests.ToEntityArray(Allocator.Temp);

            foreach (var entity in loadingRquestEntities)
            {
                var chunks = SystemAPI.GetSingletonRW<Chunks>();

                var request = state.EntityManager.GetComponentData<ChunkLoadingRequest>(entity);

                chunks.ValueRW.UpdateCenter(request.NewCenter, commandBuffer);
                loadingQueue.UpdateLoading(state.EntityManager, ref chunks.ValueRW, request.NewCenter, commandBuffer);
                commandBuffer.DestroyEntity(entity);
            }

            loadingRquestEntities.Dispose();

            for (var i = 0; i < ColumnBatchSize; i++)
            {
                if (loadingQueue.Value.Length > 0)
                {
                    var column = loadingQueue.Value[^1];
                    loadingQueue.Value.RemoveAt(loadingQueue.Value.Length - 1);

                    var x = column.x;
                    var z = column.y;

                    var illuminationRequired = false;

                    for (var y = 0; y < SystemAPI.GetSingletonRW<Chunks>().ValueRO.Height; y++)
                    {
                        var chunks = SystemAPI.GetSingletonRW<Chunks>();

                        var coordinate = new int3(x, y, z);
                        var entity = chunks.ValueRO.GetEntity(coordinate);

                        if (entity == Entity.Null)
                        {
                            illuminationRequired = true;
                            chunks.ValueRW.SpawnChunk(state.EntityManager, coordinate);
                        }
                    }

                    if (illuminationRequired)
                    {
                        var requestEntity = commandBuffer.CreateEntity();
                        commandBuffer.SetName(requestEntity, $"IlluminationRequest({column.x}, {column.y})");
                        commandBuffer.AddComponent
                        (
                            requestEntity,
                            new IlluminationRequest
                            {
                                Column = column
                            }
                        );
                    }
                }
            }
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            state.EntityManager.GetComponentData<ChunkColumnLoadingQueue>(state.SystemHandle).Dispose();
        }
    }
}
