using Unity.Collections;
using UnityEngine.Rendering;

namespace Voxilarium
{
    public struct Vertex
    {
        public enum Normal
        {
            Right,
            Left,
            Top,
            Bottom,
            Front,
            Back,
        }

        private uint dataA, dataB, dataC;

        public static NativeArray<VertexAttributeDescriptor> CreateDescriptors(Allocator allocator)
        {
            var descriptors = new NativeArray<VertexAttributeDescriptor>(1, allocator);
            descriptors[0] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.UInt32, 3);
            return descriptors;
        }

        public Vertex(uint x, uint y, uint z, int uvIndex, Normal normal, uint r, uint g, uint b, uint s)
        {
            var n = (uint)normal;

            // Layout:
            // A
            // x - 5bit, y - 5bit, z - 5bit, n - 3bit
            // B
            // r - 6bit, g - 6bit, b - 6bit, s - 6bit 
            // C
            // uv - 16bit

            // A
            var yBit = 5;
            var zBit = 5;
            var nBit = 3;

            var znBit = zBit + nBit;
            var yznBit = yBit + znBit;

            // B
            var gBit = 6;
            var bBit = 6;
            var sBit = 6;

            var bsBit = bBit + sBit;
            var gbsBit = gBit + bsBit;

            dataA = x << yznBit | y << znBit | z << nBit | n;
            dataB = r << gbsBit | g << bsBit | b << sBit | s;
            dataC = (uint)uvIndex;
        }
    }
}
