using Unity.Entities;
using UnityEngine;

namespace Minecraft
{
    public class DebugMeshAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Mesh mesh;

        private class Baker : Baker<DebugMeshAuthoring>
        {
            public override void Bake(DebugMeshAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new DebugMesh
                {
                    Mesh = authoring.mesh
                });
            }
        }
    }
}
