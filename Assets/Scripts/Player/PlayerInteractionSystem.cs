using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Voxilarium
{
    [UpdateAfter(typeof(PlayerInputSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerInteractionSystem : ISystem
    {
        private const float interactionDistance = 15.0f;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Chunks>();
            state.RequireForUpdate<Blocks>();
            state.RequireForUpdate<Lighting>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var chunks = SystemAPI.GetSingletonRW<Chunks>();
            var blocks = SystemAPI.GetSingletonRW<Blocks>();
            var lighting = SystemAPI.GetSingletonRW<Lighting>();

            foreach (var (input, view) in SystemAPI
                .Query<RefRO<PlayerInput>, RefRO<PlayerView>>()
                .WithAll<GhostOwnerIsLocal>())
            {
                var orientation = state.EntityManager.GetComponentData<LocalToWorld>(view.ValueRO.Entity);

                var ray = new Ray
                {
                    origin = orientation.Position,
                    direction = orientation.Forward
                };

                if (input.ValueRO.Attack.IsSet)
                {
                    if (PhysicsSystem.Raycast(state.EntityManager, chunks.ValueRO, blocks.ValueRO, ray, interactionDistance, out var hitInfo))
                    {
                        var voxelCoordinate = (int3)math.floor(hitInfo.point);
                        chunks.ValueRW.DestroyVoxel(state.EntityManager, blocks.ValueRO, lighting.ValueRO, voxelCoordinate);
                    }
                }
                else if (input.ValueRO.Defend.IsSet)
                {
                    if (PhysicsSystem.Raycast(state.EntityManager, chunks.ValueRO, blocks.ValueRO, ray, interactionDistance, out var hitInfo))
                    {
                        var voxelCoordinate = (int3)math.floor(hitInfo.point + hitInfo.normal);
                        chunks.ValueRW.PlaceVoxel(state.EntityManager, blocks.ValueRO, lighting.ValueRO, voxelCoordinate, Voxel.Stone);
                    }
                }
            }
        }
    }
}
