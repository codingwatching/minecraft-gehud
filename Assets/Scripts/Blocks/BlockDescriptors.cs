using System.Collections.Generic;
using Unity.Entities;

namespace Voxilarium
{
    public class BlockDescriptors : IComponentData
    {
        public List<BlockDescriptor> Items;
    }
}
