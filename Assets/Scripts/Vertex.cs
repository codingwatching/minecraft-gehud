using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Voxilarium.Utilities;

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

        private uint dataA, dataB;

        public static NativeArray<VertexAttributeDescriptor> CreateDescriptors(Allocator allocator)
        {
            var descriptors = new NativeArray<VertexAttributeDescriptor>(1, allocator);
            descriptors[0] = new(VertexAttribute.TexCoord0, VertexAttributeFormat.UInt32, 2);
            return descriptors;
        }

        public Vertex(uint x, uint y, uint z, uint u, uint v, Normal normal, uint r, uint g, uint b, uint s)
        {
            var i = IndexUtility.CoordinateToIndex(u, v, 17);

            var n = (uint)normal;

            // Layout:
            // A
            // x - 5bit, y - 5bit, z - 5bit, i - 9bit, n - 3bit
            // B
            // r - 6bit, g - 6bit, b - 6bit, s - 6bit 

            // A
            var yBit = 5;
            var zBit = 5;
            var iBit = 9;
            var nBit = 3;

            var inBit = iBit + nBit;
            var zinBit = zBit + inBit;
            var yzinBit = yBit + zinBit;

            // B
            var gBit = 6;
            var bBit = 6;
            var sBit = 6;

            var bsBit = bBit + sBit;
            var gbsBit = gBit + bsBit;

            dataA = x << yzinBit | y << zinBit | z << inBit | i << nBit | n;
            dataB = r << gbsBit | g << bsBit | b << sBit | s;
        }
    }
}
