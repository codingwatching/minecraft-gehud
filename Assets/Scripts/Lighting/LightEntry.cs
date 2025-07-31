using Unity.Mathematics;

namespace Voxilarium
{
    public struct LightEntry
    {
        public int3 Coordinate;
        public byte Level;

        public LightEntry(int3 coordinate, byte level)
        {
            Coordinate = coordinate;
            Level = level;
        }

        public LightEntry(int x, int y, int z, byte level) : this(new int3(x, y, z), level)
        {
        }
    }
}
