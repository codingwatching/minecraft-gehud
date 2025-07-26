namespace Voxilarium
{
    public struct Voxel
    {
        public const int Air = 0;
        public const int Stone = 1;

        public byte Block;

        public Voxel(byte block)
        {
            Block = block;
        }
    }
}
