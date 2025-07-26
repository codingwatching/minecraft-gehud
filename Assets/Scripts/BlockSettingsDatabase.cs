using System.Collections.Generic;
using Unity.Entities;

namespace Voxilarium
{
    public class BlockSettingsDatabase : IComponentData
    {
        public List<BlockSettings> Data;
    }
}
