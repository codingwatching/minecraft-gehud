using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Voxilarium
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlayerMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (input, worldTransform, orientation, transform) in SystemAPI
                .Query<RefRO<PlayerInput>, RefRO<LocalToWorld>, RefRO<PlayerOrientation>, RefRW<LocalTransform>>()
                .WithAll<Simulate>())
            {
                var rotation = quaternion.AxisAngle(new float3(0.0f, 1.0f, 0.0f), orientation.ValueRO.Value);

                var translation = math.mul(rotation, new float3(1.0f, 0.0f, 0.0f)) * input.ValueRO.Movement.x
                    + math.mul(rotation, new float3(0.0f, 0.0f, 1.0f)) * input.ValueRO.Movement.y;

                transform.ValueRW = transform.ValueRO.Translate(5.0f * deltaTime * translation);
                transform.ValueRW = transform.ValueRO.Translate(new float3(0.0f, input.ValueRO.Elevation * deltaTime * 5.0f, 0.0f));
            }
        }
    }
}
