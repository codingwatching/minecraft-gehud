using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Voxilarium
{
    [UpdateAfter(typeof(PlayerInputSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerMovementSystem : ISystem
    {
        private const float jumpHeight = 1.5f;
        private const float speed = 5.0f;

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var (input, orientation, hitbox) in SystemAPI
                .Query<RefRO<PlayerInput>, RefRO<PlayerOrientation>, RefRW<Hitbox>>()
                .WithAll<Simulate>())
            {
                var rotation = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), orientation.ValueRO.Value);

                var translation = math.mul(rotation, new float3(1.0f, 0.0f, 0.0f)) * input.ValueRO.Movement.x
                    + math.mul(rotation, new float3(0.0f, 0.0f, 1.0f)) * input.ValueRO.Movement.y;

                translation *= new int3(speed);

                var velocity = hitbox.ValueRO.Velocity;

                velocity.x = translation.x;
                velocity.z = translation.z;

                if (input.ValueRO.Jump.IsSet)
                {
                    velocity -= math.sign(PhysicsSystem.Gravity) * math.sqrt(2.0f * jumpHeight * math.abs(PhysicsSystem.Gravity));
                }

                hitbox.ValueRW.Velocity = velocity;
            }
        }
    }
}
