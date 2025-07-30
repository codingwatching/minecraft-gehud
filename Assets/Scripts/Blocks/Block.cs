namespace Voxilarium
{
    public struct Block
    {
        public bool IsSolid;
        public bool IsTransparent;
        public int Absorption;
        public BlockSprites Sprites;

        public Block(BlockDescriptor descriptor, BlockSprites textures)
        {
            IsSolid = descriptor.IsSolid;
            IsTransparent = descriptor.IsTransparent;
            Absorption = descriptor.Absorption;
            Sprites = textures;
        }
    }
}
