using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Voxilarium
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkMeshSystem : ISystem
    {
        public const MeshUpdateFlags UpdateFlags
            = MeshUpdateFlags.DontRecalculateBounds
            | MeshUpdateFlags.DontResetBoneBounds
            | MeshUpdateFlags.DontNotifyMeshUsers
#if !DEBUG
            | MeshUpdateFlags.DontValidateIndices
#endif
            ;

        private struct VertexAttributeDescriptors : IComponentData, IDisposable
        {
            public NativeArray<VertexAttributeDescriptor> Descriptors;

            public void Dispose()
            {
                Descriptors.Dispose();
            }
        }

        private struct Task : IComponentData
        {
            public Entity Chunk;
            public JobHandle Job;
            public Mesh.MeshDataArray Data;
        }

        private EntityQuery tasks;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            tasks = SystemAPI.QueryBuilder().WithAll<Task>().Build();

            state.EntityManager.AddComponentData(state.SystemHandle, new VertexAttributeDescriptors
            {
                Descriptors = Vertex.CreateDescriptors(Allocator.Persistent)
            });

            state.RequireForUpdate<ChunkMaterials>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (data, chunkEntity) in SystemAPI.Query<RefRO<ChunkMeshData>>().WithNone<ThreadedChunk>().WithEntityAccess())
            {
                var job = new ChunkMeshJob
                {
                    Data = data.ValueRO,
                    MeshDataArray = Mesh.AllocateWritableMeshData(1),
                    Descriptors = state.EntityManager.GetComponentData<VertexAttributeDescriptors>(state.SystemHandle).Descriptors
                };

                if (state.EntityManager.IsComponentEnabled<ImmediateChunk>(chunkEntity))
                {
                    job.Schedule().Complete();
                    ApplyJob(ref state, chunkEntity, job.MeshDataArray, commandBuffer);
                    state.EntityManager.SetComponentEnabled<ImmediateChunk>(chunkEntity, false);
                }
                else
                {
                    state.EntityManager.SetComponentEnabled<ThreadedChunk>(chunkEntity, true);
                    var taskEntity = commandBuffer.CreateEntity();
                    commandBuffer.AddComponent(taskEntity, new Task
                    {
                        Chunk = chunkEntity,
                        Data = job.MeshDataArray,
                        Job = job.Schedule(),
                    });
                }
            }

            var taskEntities = tasks.ToEntityArray(Allocator.Temp);

            foreach (var entity in taskEntities)
            {
                var task = state.EntityManager.GetComponentData<Task>(entity);

                if (!task.Job.IsCompleted)
                {
                    continue;
                }

                task.Job.Complete();

                ApplyJob(ref state, task.Chunk, task.Data, commandBuffer);
                state.EntityManager.SetComponentEnabled<ThreadedChunk>(task.Chunk, false);

                commandBuffer.DestroyEntity(entity);
            }

            taskEntities.Dispose();
        }

        private void ApplyJob(ref SystemState state, Entity chunkEntity, Mesh.MeshDataArray data, in EntityCommandBuffer commandBuffer)
        {
            state.EntityManager.SetComponentEnabled<DirtyChunk>(chunkEntity, false);
            state.EntityManager.GetComponentData<ChunkMeshData>(chunkEntity).Dispose();
            commandBuffer.RemoveComponent<ChunkMeshData>(chunkEntity);

            var mesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(data, mesh, UpdateFlags);

            if (!state.EntityManager.HasComponent<RenderMeshArray>(chunkEntity))
            {
                var chunkMaterials = SystemAPI.ManagedAPI.GetSingleton<ChunkMaterials>();

                var materials = new Material[]
                {
                    chunkMaterials.OpaqueMaterial,
                    chunkMaterials.TransparentMaterial
                };

                var meshes = new Mesh[]
                {
                    mesh
                };

                RenderMeshUtility.AddComponents
                (
                    chunkEntity,
                    state.EntityManager,
                    new RenderMeshDescription(ShadowCastingMode.On),
                    new RenderMeshArray(materials, meshes),
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 0)
                );

                const float chunkSizeHalf = Chunk.Size / 2.0f;

                var aabb = new AABB
                {
                    Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf),
                    Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                };

                var renderBounds = new RenderBounds
                {
                    Value = aabb
                };

                var coordinate = state.EntityManager.GetComponentData<Chunk>(chunkEntity).Coordinate;
                var position = coordinate * Chunk.Size;

                aabb.Center += position;

                var worldRenderBounds = new WorldRenderBounds
                {
                    Value = aabb
                };

                commandBuffer.SetComponent(chunkEntity, renderBounds);
                commandBuffer.SetComponent(chunkEntity, worldRenderBounds);

                var transparentChunkEntity = state.EntityManager.CreateEntity();
                commandBuffer.SetName(transparentChunkEntity, "Transparent");

                RenderMeshUtility.AddComponents
                (
                    transparentChunkEntity,
                    state.EntityManager,
                    new RenderMeshDescription(ShadowCastingMode.On),
                    state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(chunkEntity),
                    MaterialMeshInfo.FromRenderMeshArrayIndices(1, 0, 1)
                );

                commandBuffer.SetComponent(transparentChunkEntity, renderBounds);
                commandBuffer.SetComponent(transparentChunkEntity, worldRenderBounds);

                commandBuffer.AddComponent(transparentChunkEntity, new Parent
                {
                    Value = chunkEntity
                });

                var linked = commandBuffer.AddBuffer<LinkedEntityGroup>(chunkEntity);
                linked.Add(new LinkedEntityGroup
                {
                    Value = chunkEntity
                });
                linked.Add(new LinkedEntityGroup
                {
                    Value = transparentChunkEntity
                });
            }
            else
            {
                state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(chunkEntity).MeshReferences[0] = mesh;
            }
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            foreach (var descriptors in SystemAPI.Query<RefRO<VertexAttributeDescriptors>>())
            {
                descriptors.ValueRO.Dispose();
            }

            foreach (var task in SystemAPI.Query<RefRO<Task>>())
            {
                task.ValueRO.Job.Complete();
                task.ValueRO.Data.Dispose();
            }
        }
    }
}
