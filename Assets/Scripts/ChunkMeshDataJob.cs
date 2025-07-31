using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Voxilarium
{
    [BurstCompile]
    public struct ChunkMeshDataJob : IJob, IDisposable
    {
        [ReadOnly]
        public int3 Coordinate;
        [ReadOnly]
        public NativeArray<IntPtr> Claster;
        [ReadOnly]
        public NativeArray<Entity> ClasterEntities;
        [WriteOnly]
        public ChunkMeshData Data;
        [ReadOnly]
        public Blocks Blocks;
        [ReadOnly]
        public int AtlasSize;

        private ushort vertexCount;

        public void Execute()
        {
            for (var x = 0; x < Chunk.Size; x++)
            {
                for (var y = 0; y < Chunk.Size; y++)
                {
                    for (var z = 0; z < Chunk.Size; z++)
                    {
                        ProcessVoxel(new int3(x, y, z));
                    }
                }
            }
        }

        private void ProcessVoxel(int3 localVoxelCoordinate)
        {
            var voxel = GetVoxel(localVoxelCoordinate);
            var x = localVoxelCoordinate.x;
            var y = localVoxelCoordinate.y;
            var z = localVoxelCoordinate.z;

            var blockId = voxel.Block;

            if (blockId == Voxel.Air)
            {
                return;
            }

            var block = Blocks.Items[voxel.Block];
            var isTransparent = block.IsTransparent;
            var indices = isTransparent ? Data.TransparentIndices : Data.OpaqueIndices;

            // Right face.
            if (HasFace(x + 1, y + 0, z + 0, blockId))
            {
                var t000 = !IsTransparent(x + 1, y + 0, z + 1);
                var t090 = !IsTransparent(x + 1, y + 1, z + 0);
                var t180 = !IsTransparent(x + 1, y + 0, z - 1);
                var t270 = !IsTransparent(x + 1, y - 1, z + 0);

                var lrtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Red);
                var lr000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Red);
                var lr045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                var lr090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Red);
                var lr135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);
                var lr180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Red);
                var lr225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);
                var lr270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Red);
                var lr315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);

                var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                var lgtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Green);
                var lg000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Green);
                var lg045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                var lg090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Green);
                var lg135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);
                var lg180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Green);
                var lg225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);
                var lg270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Green);
                var lg315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);

                var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                var lbtop = GetLight(x + 1, y + 0, z + 0, LightChanel.Blue);
                var lb000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Blue);
                var lb045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                var lb090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Blue);
                var lb135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);
                var lb180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Blue);
                var lb225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);
                var lb270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Blue);
                var lb315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);

                var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                var lstop = GetLight(x + 1, y + 0, z + 0, LightChanel.Sun);
                var ls000 = GetLight(x + 1, y + 0, z + 1, LightChanel.Sun);
                var ls045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                var ls090 = GetLight(x + 1, y + 1, z + 0, LightChanel.Sun);
                var ls135 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);
                var ls180 = GetLight(x + 1, y + 0, z - 1, LightChanel.Sun);
                var ls225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);
                var ls270 = GetLight(x + 1, y - 1, z + 0, LightChanel.Sun);
                var ls315 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);

                var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                var aof1 = lr1 + lg1 + lb1 + ls1;
                var aof2 = lr2 + lg2 + lb2 + ls2;
                var aof3 = lr3 + lg3 + lb3 + ls3;
                var aof4 = lr4 + lg4 + lb4 + ls4;

                AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, GetUvIndex(block.Sprites.Right, 0, 0), Vertex.Normal.Right, lr1, lg1, lb1, ls1));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, GetUvIndex(block.Sprites.Right, 0, 1), Vertex.Normal.Right, lr2, lg2, lb2, ls2));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, GetUvIndex(block.Sprites.Right, 1, 1), Vertex.Normal.Right, lr3, lg3, lb3, ls3));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, GetUvIndex(block.Sprites.Right, 1, 0), Vertex.Normal.Right, lr4, lg4, lb4, ls4));
            }

            // Left face.
            if (HasFace(x - 1, y + 0, z + 0, blockId))
            {
                var t000 = !IsTransparent(x - 1, y + 0, z - 1);
                var t090 = !IsTransparent(x - 1, y + 1, z + 0);
                var t180 = !IsTransparent(x - 1, y + 0, z + 1);
                var t270 = !IsTransparent(x - 1, y - 1, z + 0);

                var lrtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Red);
                var lr000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Red);
                var lr045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                var lr090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Red);
                var lr135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                var lr180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Red);
                var lr225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);
                var lr270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Red);
                var lr315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);

                var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                var lgtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Green);
                var lg000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Green);
                var lg045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                var lg090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Green);
                var lg135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                var lg180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Green);
                var lg225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);
                var lg270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Green);
                var lg315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);

                var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                var lbtop = GetLight(x - 1, y + 0, z + 0, LightChanel.Blue);
                var lb000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Blue);
                var lb045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                var lb090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Blue);
                var lb135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                var lb180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Blue);
                var lb225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);
                var lb270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Blue);
                var lb315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);

                var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                var lstop = GetLight(x - 1, y + 0, z + 0, LightChanel.Sun);
                var ls000 = GetLight(x - 1, y + 0, z - 1, LightChanel.Sun);
                var ls045 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                var ls090 = GetLight(x - 1, y + 1, z + 0, LightChanel.Sun);
                var ls135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                var ls180 = GetLight(x - 1, y + 0, z + 1, LightChanel.Sun);
                var ls225 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);
                var ls270 = GetLight(x - 1, y - 1, z + 0, LightChanel.Sun);
                var ls315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);

                var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                var aof1 = lr1 + lg1 + lb1 + ls1;
                var aof2 = lr2 + lg2 + lb2 + ls2;
                var aof3 = lr3 + lg3 + lb3 + ls3;
                var aof4 = lr4 + lg4 + lb4 + ls4;

                AddFaceIndices(indices, aof1, aof2, aof3, aof4);

                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, GetUvIndex(block.Sprites.Left, 0, 0), Vertex.Normal.Left, lr1, lg1, lb1, ls1));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, GetUvIndex(block.Sprites.Left, 0, 1), Vertex.Normal.Left, lr2, lg2, lb2, ls2));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, GetUvIndex(block.Sprites.Left, 1, 1), Vertex.Normal.Left, lr3, lg3, lb3, ls3));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, GetUvIndex(block.Sprites.Left, 1, 0), Vertex.Normal.Left, lr4, lg4, lb4, ls4));
            }

            // Top face.
            if (HasFace(x + 0, y + 1, z + 0, blockId))
            {
                var t000 = !IsTransparent(x + 1, y + 1, z + 0);
                var t090 = !IsTransparent(x + 0, y + 1, z + 1);
                var t180 = !IsTransparent(x - 1, y + 1, z + 0);
                var t270 = !IsTransparent(x + 0, y + 1, z - 1);

                var lrtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Red);
                var lr000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Red);
                var lr045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                var lr090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Red);
                var lr135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                var lr180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Red);
                var lr225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                var lr270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Red);
                var lr315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);

                var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                var lgtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Green);
                var lg000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Green);
                var lg045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                var lg090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Green);
                var lg135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                var lg180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Green);
                var lg225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                var lg270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Green);
                var lg315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);

                var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                var lbtop = GetLight(x + 0, y + 1, z + 0, LightChanel.Blue);
                var lb000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Blue);
                var lb045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                var lb090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Blue);
                var lb135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                var lb180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Blue);
                var lb225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                var lb270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Blue);
                var lb315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);

                var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                var lstop = GetLight(x + 0, y + 1, z + 0, LightChanel.Sun);
                var ls000 = GetLight(x + 1, y + 1, z + 0, LightChanel.Sun);
                var ls045 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                var ls090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Sun);
                var ls135 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                var ls180 = GetLight(x - 1, y + 1, z + 0, LightChanel.Sun);
                var ls225 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                var ls270 = GetLight(x + 0, y + 1, z - 1, LightChanel.Sun);
                var ls315 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);

                var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                var aof1 = lr1 + lg1 + lb1 + ls1;
                var aof2 = lr2 + lg2 + lb2 + ls2;
                var aof3 = lr3 + lg3 + lb3 + ls3;
                var aof4 = lr4 + lg4 + lb4 + ls4;

                AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, GetUvIndex(block.Sprites.Top, 0, 0), Vertex.Normal.Top, lr1, lg1, lb1, ls1));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, GetUvIndex(block.Sprites.Top, 0, 1), Vertex.Normal.Top, lr2, lg2, lb2, ls2));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, GetUvIndex(block.Sprites.Top, 1, 1), Vertex.Normal.Top, lr3, lg3, lb3, ls3));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, GetUvIndex(block.Sprites.Top, 1, 0), Vertex.Normal.Top, lr4, lg4, lb4, ls4));
            }

            // Bottom face.
            if (HasFace(x + 0, y - 1, z + 0, blockId))
            {
                var t000 = !IsTransparent(x - 1, y - 1, z + 0);
                var t090 = !IsTransparent(x + 0, y - 1, z + 1);
                var t180 = !IsTransparent(x + 1, y - 1, z + 0);
                var t270 = !IsTransparent(x + 0, y - 1, z - 1);

                var lrtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Red);
                var lr000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Red);
                var lr045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);
                var lr090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Red);
                var lr135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);
                var lr180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Red);
                var lr225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);
                var lr270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Red);
                var lr315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);

                var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                var lgtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Green);
                var lg000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Green);
                var lg045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);
                var lg090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Green);
                var lg135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);
                var lg180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Green);
                var lg225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);
                var lg270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Green);
                var lg315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);

                var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                var lbtop = GetLight(x + 0, y - 1, z + 0, LightChanel.Blue);
                var lb000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Blue);
                var lb045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);
                var lb090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Blue);
                var lb135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);
                var lb180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Blue);
                var lb225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);
                var lb270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Blue);
                var lb315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);

                var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                var lstop = GetLight(x + 0, y - 1, z + 0, LightChanel.Sun);
                var ls000 = GetLight(x - 1, y - 1, z + 0, LightChanel.Sun);
                var ls045 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);
                var ls090 = GetLight(x + 0, y - 1, z + 1, LightChanel.Sun);
                var ls135 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);
                var ls180 = GetLight(x + 1, y - 1, z + 0, LightChanel.Sun);
                var ls225 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);
                var ls270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Sun);
                var ls315 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);

                var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                var aof1 = lr1 + lg1 + lb1 + ls1;
                var aof2 = lr2 + lg2 + lb2 + ls2;
                var aof3 = lr3 + lg3 + lb3 + ls3;
                var aof4 = lr4 + lg4 + lb4 + ls4;

                AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, GetUvIndex(block.Sprites.Bottom, 0, 0), Vertex.Normal.Bottom, lr1, lg1, lb1, ls1));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, GetUvIndex(block.Sprites.Bottom, 0, 1), Vertex.Normal.Bottom, lr2, lg2, lb2, ls2));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, GetUvIndex(block.Sprites.Bottom, 1, 1), Vertex.Normal.Bottom, lr3, lg3, lb3, ls3));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, GetUvIndex(block.Sprites.Bottom, 1, 0), Vertex.Normal.Bottom, lr4, lg4, lb4, ls4));
            }

            // Front face.
            if (HasFace(x + 0, y + 0, z + 1, blockId))
            {
                var t000 = !IsTransparent(x - 1, y + 0, z + 1);
                var t090 = !IsTransparent(x + 0, y + 1, z + 1);
                var t180 = !IsTransparent(x + 1, y + 0, z + 1);
                var t270 = !IsTransparent(x + 0, y - 1, z + 1);

                var lrtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Red);
                var lr000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Red);
                var lr045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Red);
                var lr090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Red);
                var lr135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Red);
                var lr180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Red);
                var lr225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Red);
                var lr270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Red);
                var lr315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Red);

                var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                var lgtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Green);
                var lg000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Green);
                var lg045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Green);
                var lg090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Green);
                var lg135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Green);
                var lg180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Green);
                var lg225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Green);
                var lg270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Green);
                var lg315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Green);

                var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                var lbtop = GetLight(x + 0, y + 0, z + 1, LightChanel.Blue);
                var lb000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Blue);
                var lb045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Blue);
                var lb090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Blue);
                var lb135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Blue);
                var lb180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Blue);
                var lb225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Blue);
                var lb270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Blue);
                var lb315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Blue);

                var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                var lstop = GetLight(x + 0, y + 0, z + 1, LightChanel.Sun);
                var ls000 = GetLight(x - 1, y + 0, z + 1, LightChanel.Sun);
                var ls045 = GetLight(x - 1, y + 1, z + 1, LightChanel.Sun);
                var ls090 = GetLight(x + 0, y + 1, z + 1, LightChanel.Sun);
                var ls135 = GetLight(x + 1, y + 1, z + 1, LightChanel.Sun);
                var ls180 = GetLight(x + 1, y + 0, z + 1, LightChanel.Sun);
                var ls225 = GetLight(x + 1, y - 1, z + 1, LightChanel.Sun);
                var ls270 = GetLight(x + 0, y - 1, z + 1, LightChanel.Sun);
                var ls315 = GetLight(x - 1, y - 1, z + 1, LightChanel.Sun);

                var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                var aof1 = lr1 + lg1 + lb1 + ls1;
                var aof2 = lr2 + lg2 + lb2 + ls2;
                var aof3 = lr3 + lg3 + lb3 + ls3;
                var aof4 = lr4 + lg4 + lb4 + ls4;

                AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 1, GetUvIndex(block.Sprites.Front, 0, 0), Vertex.Normal.Front, lr1, lg1, lb1, ls1));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 1, GetUvIndex(block.Sprites.Front, 0, 1), Vertex.Normal.Front, lr2, lg2, lb2, ls2));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 1, GetUvIndex(block.Sprites.Front, 1, 1), Vertex.Normal.Front, lr3, lg3, lb3, ls3));
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 1, GetUvIndex(block.Sprites.Front, 1, 0), Vertex.Normal.Front, lr4, lg4, lb4, ls4));
            }

            // Back face.
            if (HasFace(x + 0, y + 0, z - 1, blockId))
            {
                var t000 = !IsTransparent(x + 1, y + 0, z - 1);
                var t090 = !IsTransparent(x + 0, y + 1, z - 1);
                var t180 = !IsTransparent(x - 1, y + 0, z - 1);
                var t270 = !IsTransparent(x + 0, y - 1, z - 1);

                var lrtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Red);
                var lr000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Red);
                var lr045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Red);
                var lr090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Red);
                var lr135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Red);
                var lr180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Red);
                var lr225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Red);
                var lr270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Red);
                var lr315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Red);

                var lr1 = t180 && t270 ? lrtop : lrtop + lr180 + lr225 + lr270;
                var lr2 = t090 && t180 ? lrtop : lrtop + lr090 + lr135 + lr180;
                var lr3 = t000 && t090 ? lrtop : lrtop + lr000 + lr045 + lr090;
                var lr4 = t000 && t270 ? lrtop : lrtop + lr000 + lr270 + lr315;

                var lgtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Green);
                var lg000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Green);
                var lg045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Green);
                var lg090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Green);
                var lg135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Green);
                var lg180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Green);
                var lg225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Green);
                var lg270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Green);
                var lg315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Green);

                var lg1 = t180 && t270 ? lgtop : lgtop + lg180 + lg225 + lg270;
                var lg2 = t090 && t180 ? lgtop : lgtop + lg090 + lg135 + lg180;
                var lg3 = t000 && t090 ? lgtop : lgtop + lg000 + lg045 + lg090;
                var lg4 = t000 && t270 ? lgtop : lgtop + lg000 + lg270 + lg315;

                var lbtop = GetLight(x + 0, y + 0, z - 1, LightChanel.Blue);
                var lb000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Blue);
                var lb045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Blue);
                var lb090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Blue);
                var lb135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Blue);
                var lb180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Blue);
                var lb225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Blue);
                var lb270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Blue);
                var lb315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Blue);

                var lb1 = t180 && t270 ? lbtop : lbtop + lb180 + lb225 + lb270;
                var lb2 = t090 && t180 ? lbtop : lbtop + lb090 + lb135 + lb180;
                var lb3 = t000 && t090 ? lbtop : lbtop + lb000 + lb045 + lb090;
                var lb4 = t000 && t270 ? lbtop : lbtop + lb000 + lb270 + lb315;

                var lstop = GetLight(x + 0, y + 0, z - 1, LightChanel.Sun);
                var ls000 = GetLight(x + 1, y + 0, z - 1, LightChanel.Sun);
                var ls045 = GetLight(x + 1, y + 1, z - 1, LightChanel.Sun);
                var ls090 = GetLight(x + 0, y + 1, z - 1, LightChanel.Sun);
                var ls135 = GetLight(x - 1, y + 1, z - 1, LightChanel.Sun);
                var ls180 = GetLight(x - 1, y + 0, z - 1, LightChanel.Sun);
                var ls225 = GetLight(x - 1, y - 1, z - 1, LightChanel.Sun);
                var ls270 = GetLight(x + 0, y - 1, z - 1, LightChanel.Sun);
                var ls315 = GetLight(x + 1, y - 1, z - 1, LightChanel.Sun);

                var ls1 = t180 && t270 ? lstop : lstop + ls180 + ls225 + ls270;
                var ls2 = t090 && t180 ? lstop : lstop + ls090 + ls135 + ls180;
                var ls3 = t000 && t090 ? lstop : lstop + ls000 + ls045 + ls090;
                var ls4 = t000 && t270 ? lstop : lstop + ls000 + ls270 + ls315;

                var aof1 = lr1 + lg1 + lb1 + ls1;
                var aof2 = lr2 + lg2 + lb2 + ls2;
                var aof3 = lr3 + lg3 + lb3 + ls3;
                var aof4 = lr4 + lg4 + lb4 + ls4;

                AddFaceIndices(indices, aof1, aof2, aof3, aof4);
                Data.Vertices.Add(new Vertex(x + 0, y + 0, z + 0, GetUvIndex(block.Sprites.Back, 0, 0), Vertex.Normal.Back, lr1, lg1, lb1, ls1));
                Data.Vertices.Add(new Vertex(x + 0, y + 1, z + 0, GetUvIndex(block.Sprites.Back, 0, 1), Vertex.Normal.Back, lr2, lg2, lb2, ls2));
                Data.Vertices.Add(new Vertex(x + 1, y + 1, z + 0, GetUvIndex(block.Sprites.Back, 1, 1), Vertex.Normal.Back, lr3, lg3, lb3, ls3));
                Data.Vertices.Add(new Vertex(x + 1, y + 0, z + 0, GetUvIndex(block.Sprites.Back, 1, 0), Vertex.Normal.Back, lr4, lg4, lb4, ls4));
            }
        }

        private int GetUvIndex(ushort spriteId, int uOffset, int vOffset)
        {
            var uv = IndexUtility.IndexToCoordinate(spriteId, AtlasSize);
            uv.x += uOffset;
            uv.y += vOffset;
            return IndexUtility.CoordinateToIndex(uv, AtlasSize + 1);
        }

        private void AddFaceIndices(NativeList<ushort> indices, int aof1, int aof2, int aof3, int aof4)
        {
            if (aof1 + aof3 < aof2 + aof4)
            {
                // Fliped quad.
                indices.Add((ushort)(0 + vertexCount));
                indices.Add((ushort)(1 + vertexCount));
                indices.Add((ushort)(3 + vertexCount));
                indices.Add((ushort)(3 + vertexCount));
                indices.Add((ushort)(1 + vertexCount));
                indices.Add((ushort)(2 + vertexCount));
            }
            else
            {
                // Normal quad.
                indices.Add((ushort)(0 + vertexCount));
                indices.Add((ushort)(1 + vertexCount));
                indices.Add((ushort)(2 + vertexCount));
                indices.Add((ushort)(0 + vertexCount));
                indices.Add((ushort)(2 + vertexCount));
                indices.Add((ushort)(3 + vertexCount));
            }

            vertexCount += 4;
        }

        private unsafe Voxel GetVoxel(in int3 localVoxelCoordinate)
        {
            var voxelCoordinate = Coordinate * Chunk.Size + localVoxelCoordinate;
            var sideChunkCoordinate = CoordinateUtility.ToChunk(voxelCoordinate);

            var sideLocalVoxelCoordinate = voxelCoordinate - sideChunkCoordinate * Chunk.Size;

            var clasterChunkCoordinate = sideChunkCoordinate - Coordinate + new int3(1, 1, 1);
            var clasterIndex = IndexUtility.CoordinateToIndex(clasterChunkCoordinate, 3, 3);

            var voxels = (Voxel*)Claster[clasterIndex];

            if (voxels == null)
            {
                return default;
            }

            var sideLocalVoxelIndex = IndexUtility.CoordinateToIndex(sideLocalVoxelCoordinate, Chunk.Size, Chunk.Size);

            return voxels[sideLocalVoxelIndex];
        }

        private Voxel GetVoxel(int x, int y, int z)
        {
            return GetVoxel(new int3(x, y, z));
        }

        private byte GetLight(int x, int y, int z, LightChanel chanel)
        {
            return GetVoxel(x, y, z).Light.Get(chanel);
        }

        private bool IsTransparent(int x, int y, int z)
        {
            var voxel = GetVoxel(x, y, z);
            return Blocks.Items[voxel.Block].IsTransparent;
        }

        private bool HasFace(int x, int y, int z, ushort blockId)
        {
            var facingVoxel = GetVoxel(x, y, z);
            return Blocks.Items[facingVoxel.Block].IsTransparent && facingVoxel.Block != blockId;
        }

        public void Dispose()
        {
            Claster.Dispose();
            ClasterEntities.Dispose();
        }
    }
}
