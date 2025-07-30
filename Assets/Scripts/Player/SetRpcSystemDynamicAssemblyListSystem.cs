using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace Voxilarium
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [CreateAfter(typeof(RpcSystem))]
    public partial struct SetRpcSystemDynamicAssemblyListSystem : ISystem
    {
        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            SystemAPI.GetSingletonRW<RpcCollection>().ValueRW.DynamicAssemblyList = true;
            state.Enabled = false;
        }
    }
}
