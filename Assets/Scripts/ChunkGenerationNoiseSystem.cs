using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Minecraft
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ChunkGenerationNoiseSystem : ISystem
    {
        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChunkGenerationNoiseSettings>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            if (SystemAPI.HasSingleton<ChunkGenerationNoise>())
            {
                return;
            }

            var settings = SystemAPI.ManagedAPI.GetSingleton<ChunkGenerationNoiseSettings>();
            state.EntityManager.CreateSingleton
            (
                new ChunkGenerationNoise
                {
                    Continentalness = new Noise(settings.Continentalness, Allocator.Persistent),
                    Erosion = new Noise(settings.Erosion, Allocator.Persistent),
                    PeaksAndValleys = new Noise(settings.PeaksAndValleys, Allocator.Persistent),
                }
            );
        }
    }
}
