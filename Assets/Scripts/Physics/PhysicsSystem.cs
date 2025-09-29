using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Voxilarium
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PhysicsSystem : ISystem
    {
        public static readonly float3 Gravity = new(0.0f, -15.0f, 0.0f);
        public static readonly float ContactOffset = 0.08f;

        public static bool Raycast(EntityManager entityManager, in Chunks chunks, in Blocks blocks, in Ray ray, float maxDistance, out RaycastHit raycastHit)
        {
            var xOrigin = ray.origin.x;
            var yOrigin = ray.origin.y;
            var zOrigin = ray.origin.z;

            var xDirection = ray.direction.x;
            var yDirectoin = ray.direction.y;
            var zDirection = ray.direction.z;

            var time = 0.0f;
            var xCoordinate = math.floor(xOrigin);
            var yCoordinate = math.floor(yOrigin);
            var zCoordinate = math.floor(zOrigin);

            var xStep = xDirection > 0.0f ? 1.0f : -1.0f;
            var yStep = yDirectoin > 0.0f ? 1.0f : -1.0f;
            var zStep = zDirection > 0.0f ? 1.0f : -1.0f;

            var xDelta = xDirection == 0.0f ? float.PositiveInfinity : math.abs(1.0f / xDirection);
            var yDelta = yDirectoin == 0.0f ? float.PositiveInfinity : math.abs(1.0f / yDirectoin);
            var zDelta = zDirection == 0.0f ? float.PositiveInfinity : math.abs(1.0f / zDirection);

            var xDistance = xStep > 0.0f ? xCoordinate + 1.0f - xOrigin : xOrigin - xCoordinate;
            var yDistance = yStep > 0.0f ? yCoordinate + 1.0f - yOrigin : yOrigin - yCoordinate;
            var zDistance = zStep > 0.0f ? zCoordinate + 1.0f - zOrigin : zOrigin - zCoordinate;

            var xMax = xDelta < float.PositiveInfinity ? xDelta * xDistance : float.PositiveInfinity;
            var yMax = yDelta < float.PositiveInfinity ? yDelta * yDistance : float.PositiveInfinity;
            var zMax = zDelta < float.PositiveInfinity ? zDelta * zDistance : float.PositiveInfinity;

            var steppedIndex = -1;

            Vector3 endPosition;
            Vector3 endCoordinate;
            Vector3 normal;

            while (time <= maxDistance)
            {
                var voxelCoordinate = new int3((int)xCoordinate, (int)yCoordinate, (int)zCoordinate);
                var voxel = chunks.GetVoxel(entityManager, voxelCoordinate);
                if (blocks[voxel.Block].IsSolid)
                {
                    endPosition.x = xOrigin + time * xDirection;
                    endPosition.y = yOrigin + time * yDirectoin;
                    endPosition.z = zOrigin + time * zDirection;

                    endCoordinate.x = xCoordinate;
                    endCoordinate.y = yCoordinate;
                    endCoordinate.z = zCoordinate;

                    normal.x = normal.y = normal.z = 0.0f;

                    if (steppedIndex == 0)
                    {
                        normal.x = -xStep;
                    }

                    if (steppedIndex == 1)
                    {
                        normal.y = -yStep;
                    }

                    if (steppedIndex == 2)
                    {
                        normal.z = -zStep;
                    }

                    raycastHit = new()
                    {
                        point = endCoordinate,
                        normal = normal
                    };

                    return true;
                }

                if (xMax < yMax)
                {
                    if (xMax < zMax)
                    {
                        xCoordinate += xStep;
                        time = xMax;
                        xMax += xDelta;
                        steppedIndex = 0;
                    }
                    else
                    {
                        zCoordinate += zStep;
                        time = zMax;
                        zMax += zDelta;
                        steppedIndex = 2;
                    }
                }
                else
                {
                    if (yMax < zMax)
                    {
                        yCoordinate += yStep;
                        time = yMax;
                        yMax += yDelta;
                        steppedIndex = 1;
                    }
                    else
                    {
                        zCoordinate += zStep;
                        time = zMax;
                        zMax += zDelta;
                        steppedIndex = 2;
                    }
                }
            }

            endCoordinate.x = xCoordinate;
            endCoordinate.y = yCoordinate;
            endCoordinate.z = zCoordinate;

            endPosition.x = xOrigin + time * xDirection;
            endPosition.y = yOrigin + time * yDirectoin;
            endPosition.z = zOrigin + time * zDirection;
            normal.x = normal.y = normal.z = 0.0f;

            raycastHit = new()
            {
                point = endCoordinate,
                normal = normal
            };

            return false;
        }

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Chunks>();
            state.RequireForUpdate<Blocks>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            var chunks = SystemAPI.GetSingletonRW<Chunks>();
            var blocks = SystemAPI.GetSingletonRW<Blocks>();

            foreach (var (hitbox, position) in SystemAPI
                .Query<RefRW<Hitbox>, RefRW<HitboxPosition>>()
                .WithAll<Simulate>())
            {
                var extents = hitbox.ValueRO.Bounds.Extents;
                var offset = hitbox.ValueRO.Bounds.Center;

                if (hitbox.ValueRO.UseGravity)
                {
                    hitbox.ValueRW.Velocity += Gravity * deltaTime;
                }

                position.ValueRW.Value += hitbox.ValueRW.Velocity * deltaTime;

                if (hitbox.ValueRW.Velocity.x < 0.0f)
                {
                    var x = (int)math.floor(position.ValueRW.Value.x + offset.x - extents.x - ContactOffset);
                    for (var y = (int)math.floor(position.ValueRW.Value.y + offset.y - extents.y + ContactOffset); y <= (int)math.floor(position.ValueRW.Value.y + offset.y + extents.y - ContactOffset); y++)
                    {
                        for (var z = (int)math.floor(position.ValueRW.Value.z + offset.z - extents.z + ContactOffset); z <= (int)math.floor(position.ValueRW.Value.z + offset.z + extents.z - ContactOffset); z++)
                        {
                            var voxel = chunks.ValueRO.GetVoxel(state.EntityManager, new int3(x, y, z));
                            if (blocks.ValueRO[voxel.Block].IsSolid)
                            {
                                hitbox.ValueRW.Velocity = new float3(0.0f, hitbox.ValueRW.Velocity.y, hitbox.ValueRW.Velocity.z);
                                position.ValueRW.Value = new float3(x + 1.0f - offset.x + extents.x + ContactOffset, position.ValueRW.Value.y, position.ValueRW.Value.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.x > 0.0f)
                {
                    var x = (int)math.floor(position.ValueRW.Value.x + offset.x + extents.x + ContactOffset);
                    for (var y = (int)math.floor(position.ValueRW.Value.y + offset.y - extents.y + ContactOffset); y <= (int)math.floor(position.ValueRW.Value.y + offset.y + extents.y - ContactOffset); y++)
                    {
                        for (var z = (int)math.floor(position.ValueRW.Value.z + offset.z - extents.z + ContactOffset); z <= (int)math.floor(position.ValueRW.Value.z + offset.z + extents.z - ContactOffset); z++)
                        {
                            var voxel = chunks.ValueRO.GetVoxel(state.EntityManager, new int3(x, y, z));
                            if (blocks.ValueRO[voxel.Block].IsSolid)
                            {
                                hitbox.ValueRW.Velocity = new float3(0.0f, hitbox.ValueRW.Velocity.y, hitbox.ValueRW.Velocity.z);
                                position.ValueRW.Value = new float3(x - offset.x - extents.x - ContactOffset, position.ValueRW.Value.y, position.ValueRW.Value.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.z < 0.0f)
                {
                    var z = (int)math.floor(position.ValueRW.Value.z + offset.z - extents.z - ContactOffset);
                    for (var y = (int)math.floor(position.ValueRW.Value.y + offset.y - extents.y + ContactOffset); y <= (int)math.floor(position.ValueRW.Value.y + offset.y + extents.y - ContactOffset); y++)
                    {
                        for (var x = (int)math.floor(position.ValueRW.Value.x + offset.x - extents.x + ContactOffset); x <= (int)math.floor(position.ValueRW.Value.x + offset.x + extents.x - ContactOffset); x++)
                        {
                            var voxel = chunks.ValueRO.GetVoxel(state.EntityManager, new int3(x, y, z));
                            if (blocks.ValueRO[voxel.Block].IsSolid)
                            {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, hitbox.ValueRW.Velocity.y, 0.0f);
                                position.ValueRW.Value = new float3(position.ValueRW.Value.x, position.ValueRW.Value.y, z + 1.0f - offset.z + extents.z + ContactOffset);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.z > 0.0f)
                {
                    var z = (int)math.floor(position.ValueRW.Value.z + offset.z + extents.z + ContactOffset);
                    for (var y = (int)math.floor(position.ValueRW.Value.y + offset.y - extents.y + ContactOffset); y <= (int)math.floor(position.ValueRW.Value.y + offset.y + extents.y - ContactOffset); y++)
                    {
                        for (var x = (int)math.floor(position.ValueRW.Value.x + offset.x - extents.x + ContactOffset); x <= (int)math.floor(position.ValueRW.Value.x + offset.x + extents.x - ContactOffset); x++)
                        {
                            var voxel = chunks.ValueRO.GetVoxel(state.EntityManager, new int3(x, y, z));
                            if (blocks.ValueRO[voxel.Block].IsSolid)
                            {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, hitbox.ValueRW.Velocity.y, 0.0f);
                                position.ValueRW.Value = new float3(position.ValueRW.Value.x, position.ValueRW.Value.y, z - offset.z - extents.z - ContactOffset);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.y < 0.0f)
                {
                    var y = (int)math.floor(position.ValueRW.Value.y + offset.y - extents.y - ContactOffset);
                    for (var x = (int)math.floor(position.ValueRW.Value.x + offset.x - extents.x + ContactOffset); x <= (int)math.floor(position.ValueRW.Value.x + offset.x + extents.x - ContactOffset); x++)
                    {
                        for (var z = (int)math.floor(position.ValueRW.Value.z + offset.z - extents.z + ContactOffset); z <= (int)math.floor(position.ValueRW.Value.z + offset.z + extents.z - ContactOffset); z++)
                        {
                            var voxel = chunks.ValueRO.GetVoxel(state.EntityManager, new int3(x, y, z));
                            if (blocks.ValueRO[voxel.Block].IsSolid)
                            {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, 0.0f, hitbox.ValueRW.Velocity.z);
                                position.ValueRW.Value = new float3(position.ValueRW.Value.x, y + 1.0f - offset.y + extents.y + ContactOffset, position.ValueRW.Value.z);
                                break;
                            }
                        }
                    }
                }

                if (hitbox.ValueRW.Velocity.y > 0.0f)
                {
                    var y = (int)math.floor(position.ValueRW.Value.y + offset.y + extents.y + ContactOffset);
                    for (var x = (int)math.floor(position.ValueRW.Value.x + offset.x - extents.x + ContactOffset); x <= (int)math.floor(position.ValueRW.Value.x + offset.x + extents.x - ContactOffset); x++)
                    {
                        for (var z = (int)math.floor(position.ValueRW.Value.z + offset.z - extents.z + ContactOffset); z <= (int)math.floor(position.ValueRW.Value.z + offset.z + extents.z - ContactOffset); z++)
                        {
                            var voxel = chunks.ValueRO.GetVoxel(state.EntityManager, new int3(x, y, z));
                            if (blocks.ValueRO[voxel.Block].IsSolid)
                            {
                                hitbox.ValueRW.Velocity = new float3(hitbox.ValueRW.Velocity.x, 0.0f, hitbox.ValueRW.Velocity.z);
                                position.ValueRW.Value = new float3(position.ValueRW.Value.x, y - offset.y - extents.y - ContactOffset, position.ValueRW.Value.z);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(CopyCommandBufferToInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct ApplyPhisicsSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HitboxPosition>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (position, transform) in SystemAPI
                .Query<RefRO<HitboxPosition>, RefRW<LocalTransform>>()
                .WithAll<Simulate>())
            {
                transform.ValueRW.Position = position.ValueRO.Value;
            }
        }
    }
}
