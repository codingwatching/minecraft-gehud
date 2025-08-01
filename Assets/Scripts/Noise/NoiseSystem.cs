using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Voxilarium
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct NoiseSystem : ISystem
    {
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NoiseDescriptors>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var descriptorsEntity = SystemAPI.ManagedAPI.GetSingletonEntity<NoiseDescriptors>();
            var descriptors = state.EntityManager.GetComponentObject<NoiseDescriptors>(descriptorsEntity);

            commandBuffer.AddComponent
            (
                commandBuffer.CreateEntity(),
                new Noises
                {
                    Continentalness = new Noise(descriptors.Continentalness, Allocator.Persistent),
                    Erosion = new Noise(descriptors.Erosion, Allocator.Persistent),
                    PeaksAndValleys = new Noise(descriptors.PeaksAndValleys, Allocator.Persistent),
                }
            );

            commandBuffer.DestroyEntity(descriptorsEntity);
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton(out Noises noises))
            {
                noises.Dispose();
            }
        }
    }
}
