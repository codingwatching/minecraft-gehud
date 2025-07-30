using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Voxilarium
{   
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [UpdateAfter(typeof(PlayerInputSystem))]
    public partial struct PlayerViewSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);

            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (input, view, orientation) in SystemAPI
               .Query<RefRO<PlayerInput>, RefRW<PlayerView>, RefRW<PlayerOrientation>>()
               .WithAll<GhostOwnerIsLocal>())
            {
                view.ValueRW.Pitch = math.clamp(view.ValueRO.Pitch - input.ValueRO.Look.y * deltaTime, -math.PIHALF, math.PIHALF);
                view.ValueRW.Yaw += input.ValueRO.Look.x * deltaTime;

                if (view.ValueRO.Yaw > math.PI2)
                {
                    view.ValueRW.Yaw -= (int)(view.ValueRO.Yaw / math.PI2) * math.PI2;
                }
                else if (view.ValueRO.Yaw < -math.PI2)
                {
                    view.ValueRW.Yaw += (int)(-view.ValueRW.Yaw / math.PI2) * math.PI2;
                }

                var viewTransform = state.EntityManager.GetComponentData<LocalTransform>(view.ValueRO.Entity);
                viewTransform.Rotation = quaternion.EulerXYZ(view.ValueRO.Pitch, view.ValueRW.Yaw, 0.0f);
                commandBuffer.SetComponent(view.ValueRO.Entity, viewTransform);

                orientation.ValueRW.Value = view.ValueRO.Yaw;
            }

            commandBuffer.Playback(state.EntityManager);
        }
    }
}
