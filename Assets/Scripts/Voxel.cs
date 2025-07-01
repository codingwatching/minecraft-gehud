namespace Minecraft
{
    public struct Voxel
    {
        public static Voxel Air => new(0);

        public static Voxel Stone => new(1);

        public byte Block;

        public Voxel(byte block)
        {
            Block = block;
        }
    }
}
