﻿using OpenTK;
using System;
using System.Collections.Generic;
using WhittedRaytracer.Raytracing.AccelerationStructures;
using WhittedRaytracer.Raytracing.SceneObjects;
using WhittedRaytracer.Raytracing.SceneObjects.Primitives;

namespace WhittedRaytracer.Raytracing {
    /// <summary> The 3d scene in which the ray tracing takes place </summary>
    class Scene {
        /// <summary> The camera in the scene </summary>
        public readonly Camera Camera;
        /// <summary> The acceleration structure used to find intersections </summary>
        public readonly IAccelerationStructure AccelerationStructure;
        /// <summary> The primitives in the scene </summary>
        public readonly List<Primitive> Primitives = new List<Primitive>();
        /// <summary> The lightsources in the scene </summary>
        public readonly List<Lightsource> Lights = new List<Lightsource>();

        readonly Random r = new Random();

        /// <summary> Create a new scene with some default objects </summary>
        /// <param name="screen">The screen to draw the raytracing to</param>
        public Scene(IScreen screen) { 
            Camera = new Camera(screen);
            AddDefaultLights();
            AddDefaultPrimitives();
            AddRandomSpeheres(1000);
            AccelerationStructure = new BVHNode(Primitives);
        }

        void AddDefaultLights() {
            Lights.Add(new Lightsource(new Vector3(0, -8, 3), new Vector3(300, 300, 250)));
        }

        void AddDefaultPrimitives() {
            Primitives.Add(new Sphere(new Vector3(-3, -1, 5), 1, new Vector3(1f, 0.9f, 0.9f), 0, 1, 1.62f));
            Primitives.Add(new Sphere(new Vector3(0, -1, 5), 1, new Vector3(1f, 1f, 1f), 1f));
            Primitives.Add(new Sphere(new Vector3(3, -1, 5), 1, new Vector3(0, 0.5f, 0), 0, 0, 1, 0.5f, 10f));
            Primitives.Add(new Plane(new Vector3(0, -1, 0), -1, new Vector3(1, 1, 0.5f)));
            Primitives.Add(new Triangle(new Vector3(-5, 0, 0), new Vector3(5, 0, 0), new Vector3(5, 0, 10), new Vector3(1, 0.8f, 1), 0.4f, 0, 1, 0.7f, 50f));
            Primitives.Add(new Triangle(new Vector3(-5, 0, 0), new Vector3(5, 0, 10), new Vector3(-5, 0, 10), new Vector3(1, 1, 0.8f), 0, 0, 1, 0.7f, 50f));
        }

        void AddRandomSpeheres(int amount) {
            for (int i = 0; i < amount; i++) {
                Vector3 pos = new Vector3((float)r.NextDouble() * 60 - 30, (float)r.NextDouble() * 30 - 40, (float)r.NextDouble() * 60 - 30);
                Vector3 color = new Vector3((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
                Primitives.Add(new Sphere(pos, (float)r.NextDouble(), color, (float)r.NextDouble(), (float)r.NextDouble(), 1, (float)r.NextDouble(), (float)r.NextDouble()));
            }
        }

        /// <summary> Intersect the scene with a primary ray and calculate the color found by the ray </summary>
        /// <param name="ray">The ray to intersect the scene with</param>
        /// <param name="recursionDepth">The recursion depth if this is a secondary ray</param>
        /// <param name="debugRay">Whether to draw this ray in debug</param>
        /// <returns>The color at the origin of the ray</returns>
        public Vector3 CastPrimaryRay(Ray ray, int recursionDepth = 0, bool debugRay = false) {
            // Intersect with Scene
            Intersection intersection = AccelerationStructure.Intersect(ray);
            if (intersection == null) return Vector3.Zero;
            Vector3 intersectionLight = intersection.Primitive.Specularity < 1 ? CastShadowRays(intersection, debugRay) : Vector3.Zero;
            Vector3 outgoingLight = intersectionLight;

            // Specularity
            if (intersection.Primitive.Specularity > 0 && recursionDepth < Ray.MaxRecursionDepth) {
                Vector3 outReflectedLight = CastPrimaryRay(intersection.GetReflectedRay(), recursionDepth + 1, debugRay) * intersection.Primitive.Color;
                outgoingLight = intersectionLight * (1 - intersection.Primitive.Specularity) + outReflectedLight * intersection.Primitive.Specularity;
            }

            // Dielectrics
            if (intersection.Primitive.Dielectric > 0 && recursionDepth < Ray.MaxRecursionDepth) {
                Ray refractedRay = intersection.GetRefractedRay();
                Vector3 incRefractedLight = refractedRay != null ? CastPrimaryRay(refractedRay, recursionDepth + 1, debugRay) : Vector3.Zero;
                Vector3 incReflectedLight = CastPrimaryRay(intersection.GetReflectedRay(), recursionDepth + 1, debugRay);
                float reflected = intersection.GetReflectivity();
                float refracted = 1 - reflected;
                outgoingLight = intersectionLight * (1f - intersection.Primitive.Dielectric) + (incRefractedLight * refracted + incReflectedLight * reflected) * intersection.Primitive.Dielectric * intersection.Primitive.Color;
            }

            // Debug: Primary Rays
            if (debugRay) {
                Camera.ScreenPlane.DrawRay(ray);
            }
            return outgoingLight;
        }

        /// <summary> Cast shadow rays from an intersection to every light and calculate the color </summary>
        /// <param name="intersection">The intersection to cast the shadow rays from</param>
        /// <param name="debugRay">Whether to draw this ray in debug</param>
        /// <returns>The color at the intersection</returns>
        public Vector3 CastShadowRays(Intersection intersection, bool debugRay = false) {
            Vector3 totalColor = Vector3.Zero;
            foreach (Lightsource light in Lights) {
                Ray shadowRay = intersection.GetShadowRay(light);
                if (AccelerationStructure.IntersectBool(shadowRay)) continue;

                // Light Absorption
                Vector3 color = intersection.Primitive.Color * light.Color;
                // N dot L (Glossyness)
                float NdotL = Vector3.Dot(intersection.Normal, shadowRay.Direction);
                if (intersection.Primitive.Glossyness == 0) {
                    color = color * NdotL;
                } else if (intersection.Primitive.Glossyness > 0) {
                    Vector3 glossyDirection = -shadowRay.Direction - 2 * Vector3.Dot(-shadowRay.Direction, intersection.Normal) * intersection.Normal;
                    float dot = Vector3.Dot(glossyDirection, -intersection.Ray.Direction);
                    if (dot > 0) {
                        float glossyness = (float)Math.Pow(dot, intersection.Primitive.GlossSpecularity);
                        // Phong-Shading (My Version)
                        color = color * (1 - intersection.Primitive.Glossyness) * NdotL + intersection.Primitive.Glossyness * glossyness * light.Color;
                        // Phong-Shading (Official)
                        //color = color * ((1 - intersection.primitive.glossyness) * NdotL + intersection.primitive.glossyness * glossyness);
                    } else {
                        color = color * (1 - intersection.Primitive.Glossyness) * NdotL;
                    }
                }
                // Distance Attenuation
                color = color * (1 / (shadowRay.Length * shadowRay.Length));

                // Add Color to Total
                totalColor += color;

                // Debug: Shadow Rays
                if (debugRay) {
                    Camera.ScreenPlane.DrawRay(shadowRay, light.Color.ToIntColor());
                }

            }
            // Triangle Texture
            if (intersection.Primitive is Triangle) {
                if (Math.Abs(intersection.Position.X % 2) < 1) totalColor = totalColor * 0.5f;
                if (Math.Abs(intersection.Position.Z % 2) > 1) totalColor = totalColor * 0.5f;
            }

            return totalColor;
        }
    }
}