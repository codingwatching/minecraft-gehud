using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Voxilarium
{
    public partial struct BlockSystem : ISystem
    {
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BlockSettingsDatabase>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<Blocks>())
            {
                return;
            }

            var settings = SystemAPI.ManagedAPI.GetSingleton<BlockSettingsDatabase>();
            state.EntityManager.CreateSingleton
            (
                new Blocks
                {
                    Value = new(settings.Data.Select(item => new Block(item)).ToArray(), Allocator.Persistent)
                }
            );
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            foreach (var blocks in SystemAPI.Query<RefRO<Blocks>>())
            {
                blocks.ValueRO.Dispose();
            }
        }
    }
}
