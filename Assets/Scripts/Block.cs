namespace Minecraft
{
    public struct Block
    {
        public bool IsSolid;
        public bool IsTransparent;
        public int Absorption;

        public Block(BlockSettings settings)
        {
            IsSolid = settings.IsSolid;
            IsTransparent = settings.IsTransparent;
            Absorption = settings.Absorption;
        }
    }
}
