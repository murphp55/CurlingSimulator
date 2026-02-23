using UnityEngine;

namespace CurlingSimulator.Simulation
{
    /// <summary>
    /// Analytic elastic collision between two equal-mass spheres on a 2D plane (XZ).
    /// Deterministic — no PhysX involved. Safe to use with kinematic Rigidbodies.
    /// </summary>
    public static class CollisionResolver
    {
        /// <summary>
        /// Returns true if two stones are overlapping based on their 2D centres and radius.
        /// </summary>
        public static bool AreColliding(Vector2 pos1, Vector2 pos2, float stoneRadius)
        {
            return (pos2 - pos1).sqrMagnitude < (stoneRadius * 2f) * (stoneRadius * 2f);
        }

        /// <summary>
        /// Computes new velocities after an elastic collision between two equal-mass stones.
        /// Positions are used only to determine the collision normal.
        /// </summary>
        /// <returns>Tuple of (velocity1After, velocity2After).</returns>
        public static (Vector2 v1Out, Vector2 v2Out) Resolve(
            Vector2 pos1, Vector2 vel1,
            Vector2 pos2, Vector2 vel2,
            float restitution)
        {
            Vector2 normal = (pos2 - pos1).normalized;

            // Project velocities onto the collision normal
            float v1n = Vector2.Dot(vel1, normal);
            float v2n = Vector2.Dot(vel2, normal);

            // Only resolve if stones are approaching each other
            if (v1n - v2n <= 0f)
                return (vel1, vel2);

            // Equal-mass elastic collision along normal
            float v1nAfter = v1n * (1f - restitution) / 2f + v2n * (1f + restitution) / 2f;
            float v2nAfter = v1n * (1f + restitution) / 2f + v2n * (1f - restitution) / 2f;

            // Tangential components are unchanged
            Vector2 tangent    = new Vector2(-normal.y, normal.x);
            float   v1t        = Vector2.Dot(vel1, tangent);
            float   v2t        = Vector2.Dot(vel2, tangent);

            Vector2 v1Out = normal * v1nAfter + tangent * v1t;
            Vector2 v2Out = normal * v2nAfter + tangent * v2t;

            return (v1Out, v2Out);
        }

        /// <summary>
        /// Pushes two overlapping stones apart along their separation axis so they no longer intersect.
        /// Call this after Resolve() to prevent tunnelling artifacts.
        /// </summary>
        public static (Vector2 pos1Out, Vector2 pos2Out) Separate(
            Vector2 pos1, Vector2 pos2, float stoneRadius)
        {
            Vector2 delta   = pos2 - pos1;
            float   dist    = delta.magnitude;
            float   overlap = stoneRadius * 2f - dist;

            if (overlap <= 0f)
                return (pos1, pos2);

            Vector2 push = delta.normalized * (overlap * 0.5f);
            return (pos1 - push, pos2 + push);
        }
    }
}
