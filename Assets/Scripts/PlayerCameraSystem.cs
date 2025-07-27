using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Voxilarium
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct PlayerCameraSystem : ISystem
    {
        void ISystem.OnUpdate(ref SystemState state)
        {
            foreach (var view in SystemAPI.Query<RefRO<PlayerView>>().WithAll<GhostOwnerIsLocal>())
            {
                var transform = state.EntityManager.GetComponentData<LocalToWorld>(view.ValueRO.Entity);
                Camera.main.transform.SetPositionAndRotation(transform.Position, transform.Rotation);
            }
        }
    }
}
