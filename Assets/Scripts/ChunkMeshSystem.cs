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
using static UnityEngine.EventSystems.EventTrigger;

namespace Minecraft
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

        private const float chunkSizeHalf = Chunk.Size / 2.0f;

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

        private void ApplyJob(ref SystemState state, Entity entity, Mesh.MeshDataArray data, in EntityCommandBuffer commandBuffer)
        {
            state.EntityManager.SetComponentEnabled<DirtyChunk>(entity, false);
            commandBuffer.RemoveComponent<ChunkMeshData>(entity);

            var mesh = new Mesh();
            Mesh.ApplyAndDisposeWritableMeshData(data, mesh, UpdateFlags);

            if (!state.EntityManager.HasComponent<RenderMeshArray>(entity))
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
                    entity,
                    state.EntityManager,
                    new RenderMeshDescription(ShadowCastingMode.Off),
                    new RenderMeshArray(materials, meshes),
                    MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0, 0)
                );

                commandBuffer.SetComponent(entity, new RenderBounds
                {
                    Value = new AABB
                    {
                        Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf),
                        Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                    }
                });

                var coordinate = state.EntityManager.GetComponentData<Chunk>(entity).Coordinate;
                var position = coordinate * Chunk.Size;

                commandBuffer.SetComponent(entity, new WorldRenderBounds
                {
                    Value = new AABB
                    {
                        Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf) + position,
                        Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                    }
                });

                var rendererEntity = state.EntityManager.CreateEntity();
                commandBuffer.SetName(rendererEntity, $"TransparentChunk({coordinate.x}, {coordinate.y}, {coordinate.z})");

                RenderMeshUtility.AddComponents
                (
                    rendererEntity,
                    state.EntityManager,
                    new RenderMeshDescription(ShadowCastingMode.Off),
                    state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity),
                    MaterialMeshInfo.FromRenderMeshArrayIndices(1, 0, 1)
                );

                commandBuffer.SetComponent(rendererEntity, new RenderBounds
                {
                    Value = new AABB
                    {
                        Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf),
                        Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                    }
                });

                commandBuffer.SetComponent(rendererEntity, new WorldRenderBounds
                {
                    Value = new AABB
                    {
                        Center = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf) + position,
                        Extents = new float3(chunkSizeHalf, chunkSizeHalf, chunkSizeHalf)
                    }
                });

                commandBuffer.AddComponent(rendererEntity, new LocalToWorld
                {
                    Value = float4x4.Translate(position)
                });

                var buffer = commandBuffer.AddBuffer<SubChunk>(entity);
                buffer.Add(new SubChunk
                {
                    Value = rendererEntity
                });
            }
            else
            {
                state.EntityManager.GetSharedComponentManaged<RenderMeshArray>(entity).MeshReferences[0] = mesh;
            }
        }
    }
}
