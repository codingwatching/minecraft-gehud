namespace Voxilarium
{
    public struct Block
    {
        public bool IsSolid;
        public bool IsTransparent;
        public int Absorption;
        public Textures Textures;

        public Block(BlockSettings settings)
        {
            IsSolid = settings.IsSolid;
            IsTransparent = settings.IsTransparent;
            Absorption = settings.Absorption;
            Textures = settings.Textures;
        }
    }
}
