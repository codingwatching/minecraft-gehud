using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Voxilarium
{
    public struct Chunk : IComponentData, IDisposable
    {
        public const int Size = 16;
        public const int Area = Size * Size;
        public const int Volume = Area * Size;

        public int3 Coordinate;
        public NativeArray<Voxel> Voxels;

        public Voxel this[int index]
        {
            get => Voxels[index];
            set => Voxels[index] = value;
        }

        public Voxel this[int x, int y, int z]
        {
            get => Voxels[z * Area + y * Size + x];
            set => Voxels[z * Area + y * Size + x] = value;
        }

        public Voxel this[int3 coordinate]
        {
            get => this[coordinate.x, coordinate.y, coordinate.z];
            set => this[coordinate.x, coordinate.y, coordinate.z] = value;
        }

        public void Dispose()
        {
            Voxels.Dispose();
        }
    }
}
