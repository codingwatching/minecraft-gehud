using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;

namespace Voxilarium
{
    public static class IndexUtility
    {
        public static int CoordinateToIndex(int x, int y, int z, int xMax, int yMax)
        {
            return z * xMax * yMax + y * xMax + x;
        }

        public static int CoordinateToIndex(in int3 coordinate, int xMax, int yMax)
        {
            return CoordinateToIndex(coordinate.x, coordinate.y, coordinate.z, xMax, yMax);
        }

        public static int CoordinateToIndex(int x, int y, int xMax)
        {
            return y * xMax + x;
        }

        public static int CoordinateToIndex(in int2 coordinate, int xMax)
        {
            return CoordinateToIndex(coordinate.x, coordinate.y, xMax);
        }

        public static uint CoordinateToIndex(uint x, uint y, uint xMax)
        {
            return y * xMax + x;
        }

        public static int3 IndexToCoordinate(int index, int xMax, int yMax)
        {
            var z = index / (xMax * yMax);
            index -= z * xMax * yMax;
            var y = index / xMax;
            var x = index % xMax;
            return new(x, y, z);
        }

        public static int2 IndexToCoordinate(int index, int xMax)
        {
            var x = index % xMax;
            var y = index / xMax;
            return new(x, y);
        }
    }
}
