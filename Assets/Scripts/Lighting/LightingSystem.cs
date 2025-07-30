using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Voxilarium
{
    public partial struct LightingSystem : ISystem
    {
        public const int ChanelCount = 4;

        [BurstCompile]
        void ISystem.OnCreate(ref SystemState state)
        {
            var add = new NativeArray<NativeQueue<LightingEntry>>(ChanelCount, Allocator.Persistent);
            for (int i = 0; i < add.Length; i++)
            {
                add[i] = new NativeQueue<LightingEntry>(Allocator.Persistent);
            }

            var remove = new NativeArray<NativeQueue<LightingEntry>>(ChanelCount, Allocator.Persistent);
            for (int i = 0; i < remove.Length; i++)
            {
                remove[i] = new NativeQueue<LightingEntry>(Allocator.Persistent);
            }

            state.EntityManager.AddComponentData
            (
                state.SystemHandle,
                new LightingQueues
                {
                    Add = add,
                    Remove = remove
                }
            );

            state.RequireForUpdate<Blocks>();
            state.RequireForUpdate<ChunkBuffer>();
        }

        [BurstCompile]
        void ISystem.OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI
                .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var buffer = SystemAPI.GetSingletonRW<ChunkBuffer>();
            var queues = SystemAPI.GetSingletonRW<LightingQueues>();
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            state.EntityManager.GetComponentData<LightingQueues>(state.SystemHandle).Dispose();
        }
    }
}
