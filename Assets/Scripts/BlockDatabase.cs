using System.Collections.Generic;
using Unity.Entities;

namespace Minecraft
{
    public class BlockDatabase : IComponentData
    {
        public List<BlockSettings> Data;
    }
}
