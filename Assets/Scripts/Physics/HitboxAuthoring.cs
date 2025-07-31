using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Voxilarium
{
    [DisallowMultipleComponent]
    public class HitboxAuthoring : MonoBehaviour
    {
        [SerializeField]
        private bool useGravity = true;
        [SerializeField]
        private AABB bounds;

        private class Baker : Baker<HitboxAuthoring>
        {
            public override void Bake(HitboxAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<HitboxPosition>(entity);
                AddComponent
                (
                    entity,
                    new Hitbox
                    {
                        UseGravity = authoring.useGravity,
                        Bounds = authoring.bounds
                    }
                );
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position + (Vector3)bounds.Center, bounds.Size);
        }
    }
}
