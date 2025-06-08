using Unity.Burst;
using Unity.Entities;

namespace Minecraft
{
    [BurstCompile]
    public partial struct ChunkCreationSystem : ISystem
    {
        readonly void ISystem.OnUpdate(ref SystemState state)
        {
        }
    }
}
