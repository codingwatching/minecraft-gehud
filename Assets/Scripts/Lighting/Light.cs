namespace Voxilarium
{
    public struct Light
    {
        public const byte Min = 0;
        public const byte Max = 15;

        private ushort value;

        public readonly byte Get(LightChanel chanel)
        {
            return (byte)(value >> ((byte)chanel << 2) & 0xF);
        }

        public void Set(LightChanel chanel, byte value)
        {
            this.value = (ushort)(this.value & (0xFFFF & ~(0xF << (byte)chanel * 4)) | value << ((byte)chanel << 2));
        }

        public byte Red
        {
            readonly get => (byte)(value & 0xF);
            set => this.value = (ushort)(this.value & 0xFFF0 | value);
        }

        public byte Green
        {
            readonly get => (byte)(value >> 4 & 0xF);
            set => this.value = (ushort)(this.value & 0xFF0F | value << 4);
        }

        public byte Blue
        {
            readonly get => (byte)(value >> 8 & 0xF);
            set => this.value = (ushort)(this.value & 0xF0FF | value << 8);
        }

        public byte Sun
        {
            readonly get => (byte)(value >> 12 & 0xF);
            set => this.value = (ushort)(this.value & 0x0FFF | value << 12);
        }

        public override readonly string ToString()
        {
            return $"(R: {Red}, G: {Green}, B: {Blue}, S: {Sun})";
        }
    }
}
