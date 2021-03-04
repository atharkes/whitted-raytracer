﻿using OpenTK.Mathematics;
using System;

namespace PathTracer.Pathtracing.SceneObjects.Primitives {
    /// <summary> A triangle primitive for the 3d scene </summary>
    public class Triangle : Primitive {
        /// <summary> Epsilon used for the Möller–Trumbore triangle intersection </summary>
        public const float IntersectionEpsilon = 0.0000001f;

        /// <summary> The first point of the triangle </summary>
        public Vector3 P1 { get; }
        /// <summary> The second point of the triangle </summary>
        public Vector3 P2 { get; }
        /// <summary> The third point of the triangle </summary>
        public Vector3 P3 { get; }
        /// <summary> The normal of the triangle </summary>
        public Vector3 Normal { get; }
        /// <summary> Get the AABB bounds of the triangle </summary>
        public override Vector3[] Bounds {
            get {
                Vector3 min = Vector3.ComponentMin(P1, Vector3.ComponentMin(P2, P3));
                Vector3 max = Vector3.ComponentMax(P1, Vector3.ComponentMax(P2, P3));
                return new Vector3[] { min, max };
            }
        }

        /// <summary> Create a new triangle object for the 3d scene </summary>
        /// <param name="p1">The first point of the triangle</param>
        /// <param name="p2">The second point of the triangle</param>
        /// <param name="p3">The third point of the triangle</param>
        /// <param name="normal">The optional normal value; default is clockwise</param>
        /// <param name="material">The material of the triangle</param>
        public Triangle(Vector3 p1, Vector3 p2, Vector3 p3, Vector3? normal = null, Material? material = null) : base((p1 + p2 + p3) / 3f, material) {
            P1 = p1;
            P2 = p2;
            P3 = p3;
            Normal = normal ?? Vector3.Cross(p2 - p1, p3 - p1).Normalized();
        }

        /// <summary> Create a <paramref name="random"/> point on the surface of the <see cref="Triangle"/> </summary>
        /// <param name="random">The <see cref="Random"/> to determine the location of the point</param>
        /// <returns>A <paramref name="random"/> point on the surface of the <see cref="Triangle"/></returns>
        public override Vector3 GetSurfacePoint(Random random) {
            Vector3 P1toP2 = P2 - P1;
            Vector3 P1toP3 = P3 - P1;
            float r1 = (float)random.NextDouble();
            float r2 = (float)random.NextDouble();
            if (r1 + r2 > 1) {
                r1 = 1 - r1;
                r2 = 1 - r2;
            }
            return P1 + P1toP2 * r1 + P1toP3 * r2;
        }

        /// <summary> Get the normal of the triangle </summary>
        /// <param name="surfacePoint">The intersection point to get the normal at</param>
        /// <returns>The normal of the triangle at the intersection point</returns>
        public override Vector3 GetNormal(Vector3 surfacePoint) {
            return Normal;
        }

        /// <summary> Intersect the <see cref="Triangle"/> with a <paramref name="ray"/> </summary>
        /// <param name="ray">The <see cref="Ray"/> to intersect the <see cref="Triangle"/> with</param>
        /// <returns>Whether the <paramref name="ray"/> intersects the <see cref="Triangle"/></returns>
        public override bool IntersectBool(Ray ray) {
            return Intersect(ray) != null;
        }

        /// <summary> Intersect the triangle with a ray (Möller–Trumbore triangle intersection) </summary>
        /// <param name="ray">The ray to intersect the triangle with</param>
        /// <returns>The intersection with the triangle if there is any</returns>
        public override Intersection? Intersect(Ray ray) {
            // Get vectors for two edges sharing V1
            Vector3 P1toP2 = P2 - P1;
            Vector3 P1toP3 = P3 - P1;

            // Begin calculating determinant - also used to calculate u parameter
            Vector3 P = Vector3.Cross(ray.Direction, P1toP3);
            // If determinant is near zero, ray lies in plane of triangle
            float determinant = Vector3.Dot(P1toP2, P);
            if (determinant > -IntersectionEpsilon && determinant < IntersectionEpsilon) return null;
            float determinantInverted = 1f / determinant;

            // Calculate distance from V1 to ray origin
            Vector3 T = ray.Origin - P1;

            // Calculate u parameter and test bound
            float u = Vector3.Dot(T, P) * determinantInverted;
            if (u < 0f || u > 1f)  return null;

            // Calculate v and test bound
            Vector3 Q = Vector3.Cross(T, P1toP2);
            float v = Vector3.Dot(ray.Direction, Q) * determinantInverted;
            if (v < 0f || u + v > 1f)  return null;

            float t = Vector3.Dot(P1toP3, Q) * determinantInverted;
            if (t < IntersectionEpsilon || t > ray.Length) {
                return null;
            } else {
                return new Intersection(ray, t, this);
            }
        }

        /// <summary> Clip this triangle by a plane </summary>
        /// <param name="plane">The clipping plane to clip the triangle with</param>
        /// <returns>The points that are left after clipping the triangle</returns>
        public Vector3[] GetClippedPoints(AxisAlignedPlane plane) {
            Vector3 v0 = P1 - plane.Position, v1 = P2 - plane.Position, v2 = P3 - plane.Position, v3;
            const float clipEpsilon = 0.00001f, clipEpsilon2 = 0.01f;
            // Distances to the plane (this is an array parallel to v[], stored as a vec3)
            Vector3 dist = new Vector3(Vector3.Dot(v0, plane.Normal), Vector3.Dot(v1, plane.Normal), Vector3.Dot(v2, plane.Normal));
            if (dist.X < clipEpsilon2 && dist.Y < clipEpsilon2 && dist.Z < clipEpsilon2) {
                // Case 1 (all clipped)
                return Array.Empty<Vector3>();
            }
            if (dist.X > -clipEpsilon && dist.Y > -clipEpsilon && dist.Z > -clipEpsilon) {
                // Case 2 (none clipped)
                return new Vector3[] { v0, v1, v2 };
            }
            // There are either 1 or 2 vertices above the clipping plane
            bool above0 = dist.X >= 0;
            bool above1 = dist.Y >= 0;
            bool above2 = dist.Z >= 0;
            bool nextIsAbove;
            // Find the CCW - most vertex above the plane
            if (above1 && !above0) {
                // Cycle once CCW. Use v3 as a temp
                nextIsAbove = above2;
                v3 = v0; v0 = v1; v1 = v2; v2 = v3;
                dist = new Vector3(dist.Y, dist.Z, dist.X);
            } else if (above2 && !above1) {
                // Cycle once CW. Use v3 as a temp
                nextIsAbove = above0;
                v3 = v2; v2 = v1; v1 = v0; v0 = v3;
                dist = new Vector3(dist.Z, dist.X, dist.Y);
            } else nextIsAbove = above1;
            // We always need to clip v2 - v0
            v3 = Vector3.Lerp(v0, v2, dist[0] / (dist[0] - dist[2]));
            if (nextIsAbove) {
                // Case 3 (quadrilateral)
                v2 = Vector3.Lerp(v1, v2, dist[1] / (dist[1] - dist[2]));
                return new Vector3[] { v0, v1, v2, v3 };
            } else {
                // Case 4 (triangle)
                v1 = Vector3.Lerp(v0, v1, dist[0] / (dist[0] - dist[1]));
                v2 = v3;
                return new Vector3[] { v0, v1, v2 };
            }
        }

        
    }
}