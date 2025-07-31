using System;
using Unity.Entities;
using Unity.NetCode;

namespace Voxilarium
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerInteractionSystem : ISystem
    {
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var input in SystemAPI
                .Query<RefRO<PlayerInput>>()
                .WithAll<GhostOwnerIsLocal>())
            {
            }
        }
    }
}
