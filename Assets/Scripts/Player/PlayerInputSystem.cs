using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Voxilarium
{
    public class PlayerControls : IComponentData
    {
        public Controls Value;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
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

                if (controls.Value.Player.Attack.WasPressedThisFrame())
                {
                    playerInput.ValueRW.Attack.Set();
                }

                if (controls.Value.Player.Defend.WasPressedThisFrame())
                {
                    playerInput.ValueRW.Defend.Set();
                }

                if (controls.Value.Player.Jump.WasPressedThisFrame())
                {
                    playerInput.ValueRW.Jump.Set();
                }
            }
        }
    }
}
