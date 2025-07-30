using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Voxilarium
{
    public struct AtlasSize : IComponentData
    {
        public int Value;
    }

    public partial struct BlockSystem : ISystem
    {
        public const int TextureResolution = 16;
        public const int MaxAtlasSize = 254;

        void ISystem.OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BlockDescriptors>();
            state.RequireForUpdate<ChunkMaterials>();
        }

        void ISystem.OnUpdate(ref SystemState state)
        {
            var descriptors = SystemAPI.ManagedAPI.GetSingleton<BlockDescriptors>();

            var sprites = descriptors.Items
                .SelectMany(item => item.Sprites.All())
                .Where(item => item != null)
                .Distinct()
                .ToList();

            var length = Mathf.Max(4, Mathf.NextPowerOfTwo(sprites.Count + 1));

            var size = length / 2;

            if (size > MaxAtlasSize)
            {
                throw new Exception("Sprite count is out of atlas bounds.");
            }

            var atlas = new Texture2D
            (
                size * TextureResolution,
                size * TextureResolution,
                TextureFormat.RGBA32,
                3,
                false,
                true
            )
            {
                filterMode = FilterMode.Point,
            };

            Color32[] colors = new Color32[atlas.width * atlas.height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color32(0, 0, 0, 0);
            }

            atlas.SetPixels32(colors);

            var spriteToId = new Dictionary<Sprite, ushort>();

            var id = 1;

            for (var i = 1; i < length; i++)
            {
                if (sprites.Count == 0)
                {
                    break;
                }

                var coordinate = IndexUtility.IndexToCoordinate(i, size);
                var x = coordinate.x;
                var y = coordinate.y;

                var sprite = sprites[^1];
                sprites.RemoveAt(sprites.Count - 1);

                if (sprite.rect.width != TextureResolution || sprite.rect.height != TextureResolution)
                {
                    throw new Exception("Wrong sprite resolution.");
                }

                atlas.CopyPixels
                (
                    sprite.texture,
                    0,
                    0,
                    (int)sprite.rect.xMin,
                    (int)sprite.rect.yMin,
                    (int)sprite.rect.width,
                    (int)sprite.rect.height,
                    0,
                    x * TextureResolution,
                    y * TextureResolution
                );

                spriteToId.Add(sprite, (ushort)id++);
            }

            atlas.Apply();

            var chunkMaterials = SystemAPI.ManagedAPI.GetSingleton<ChunkMaterials>();
            chunkMaterials.OpaqueMaterial.SetTexture("_Atlas", atlas);
            chunkMaterials.OpaqueMaterial.SetFloat("_AtlasSize", size);
            chunkMaterials.TransparentMaterial.SetTexture("_Atlas", atlas);
            chunkMaterials.TransparentMaterial.SetFloat("_AtlasSize", size);

            state.EntityManager.CreateSingleton
            (
                new AtlasSize
                {
                    Value = size
                }
            );

            BlockSprites GetTextures(BlockSpriteDescriptor descriptor)
            {
                return new BlockSprites
                {
                    Back = descriptor.Back != null ? spriteToId[descriptor.Back] : (ushort)0,
                    Bottom = descriptor.Bottom != null ? spriteToId[descriptor.Bottom] : (ushort)0,
                    Front = descriptor.Front != null ? spriteToId[descriptor.Front] : (ushort)0,
                    Left = descriptor.Left != null ? spriteToId[descriptor.Left] : (ushort)0,
                    Right = descriptor.Right != null ? spriteToId[descriptor.Right] : (ushort)0,
                    Top = descriptor.Top != null ? spriteToId[descriptor.Top] : (ushort)0,
                };
            }

            state.EntityManager.CreateSingleton
            (
                new Blocks
                {
                    Items = new(descriptors.Items
                        .Select(item => new Block(item, GetTextures(item.Sprites))).ToArray(), Allocator.Persistent)
                }
            );

            state.EntityManager.DestroyEntity(SystemAPI.ManagedAPI.GetSingletonEntity<BlockDescriptors>());
        }

        [BurstCompile]
        void ISystem.OnDestroy(ref SystemState state)
        {
            if (SystemAPI.TryGetSingleton(out Blocks blocks))
            {
                blocks.Dispose();
            }
        }
    }
}
