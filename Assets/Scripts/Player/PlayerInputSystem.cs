using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Voxilarium
{
    public class PlayerControls : IComponentData
    {
        public Controls Value;
    }

    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial struct PlayerInputSystem : ISystem
    {
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkStreamInGame>();
            state.RequireForUpdate<PlayerSpawner>();

            var controls = new Controls();
            controls.Enable();

            state.EntityManager.AddComponentData
            (
                state.SystemHandle,
                new PlayerControls
                {
                    Value = controls
                }
            );
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var controls = state.EntityManager.GetComponentObject<PlayerControls>(state.SystemHandle);

            foreach (var playerInput in SystemAPI.Query<RefRW<PlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                playerInput.ValueRW = default;
                playerInput.ValueRW.Movement = controls.Value.Player.Move.ReadValue<Vector2>();
                playerInput.ValueRW.Elevation = controls.Value.Player.Elevation.ReadValue<float>();
                playerInput.ValueRW.Look = controls.Value.Player.Look.ReadValue<Vector2>();
            }
        }
    }
}
