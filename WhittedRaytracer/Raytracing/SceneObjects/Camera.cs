﻿using OpenTK;
using WhittedRaytracer.Raytracing.SceneObjects.CameraParts;
using WhittedRaytracer.Utilities;

namespace WhittedRaytracer.Raytracing.SceneObjects {
    /// <summary> The camera object in the 3d scene </summary>
    class Camera : ISceneObject {
        /// <summary> The input handler of the camera </summary>
        public readonly InputHandler InputHandler = new InputHandler();
        /// <summary> The statitics of the camera </summary>
        public readonly Statistics Statistics = new Statistics();
        /// <summary> The configuration of the camera </summary>
        public readonly Config Config = new Config();

        /// <summary> The screen plane in front of the camera </summary>
        public readonly ScreenPlane ScreenPlane;
        /// <summary> The position of the camera </summary>
        public Vector3 Position { get => position; set => Move(value - position); }
        /// <summary> The direction the camera is facing </summary>
        public Vector3 ViewDirection { get => viewDirection; set => SetViewDirection(value); }
        /// <summary> The field of view of the camera. It determines the distance to the screen plane </summary>
        public float FOV { get => fov; set => SetFOV(value); } 
        /// <summary> The sensitivity of turning </summary>
        public readonly float Sensitivity = 0.05f;
        /// <summary> The speed at which the camera moves </summary>
        public readonly float MoveSpeed = 0.1f;

        /// <summary> Vector going up from the view direction of the camera </summary>
        public Vector3 Up => Vector3.Cross(ViewDirection, Left).Normalized();
        /// <summary> Vector going down from the view direction of the camera </summary>
        public Vector3 Down => -Up;
        /// <summary> Vector going left from the view direction of the camera </summary>
        public Vector3 Left => new Vector3(-ViewDirection.Z, 0, ViewDirection.X).Normalized();
        /// <summary> Vector going right from the view direction of the camera </summary>
        public Vector3 Right => -Left;
        /// <summary> Vector going along the view direction of the camera </summary>
        public Vector3 Front => ViewDirection;
        /// <summary> Vector going from the view direction of the camera </summary>
        public Vector3 Back => -ViewDirection;

        Vector3 position;
        Vector3 viewDirection;
        float fov = 90f;

        /// <summary> Create a new camera object </summary>
        /// <param name="screen">The screen to draw the raytracing to</param>
        /// <param name="position">The position of the camera</param>
        /// <param name="viewDirection">The direction the camera is facing</param>
        public Camera(IScreen screen, Vector3? position = null, Vector3? viewDirection = null) {
            this.position = position ?? new Vector3(0, -1, -1);
            this.viewDirection = viewDirection?.Normalized() ?? new Vector3(0, 0, 1);
            ScreenPlane = new ScreenPlane(this, screen);
        }

        /// <summary> Move the camera in a direction </summary>
        /// <param name="direction">The direction to move the camera in</param>
        public void Move(Vector3 direction) {
            position += direction * MoveSpeed;
            ScreenPlane.Update();
        }
        
        /// <summary> Turn the view direction of the camera </summary>
        /// <param name="direction">The direction to turn the camera in</param>
        public void Turn(Vector3 direction) {
            viewDirection += direction * Sensitivity;
            viewDirection.Normalize();
            ScreenPlane.Update();
        }

        /// <summary> Set the view direction of the camera </summary>
        /// <param name="newViewDirection">The new view direction of the camera</param>
        public void SetViewDirection(Vector3 newViewDirection) {
            viewDirection = newViewDirection.Normalized();
            ScreenPlane.Update();
        }

        /// <summary> Set the field of view of the camera </summary>
        /// <param name="newFOV">The new field of view</param>
        public void SetFOV(float newFOV) {
            fov = newFOV;
            ScreenPlane.Update();
        }

        /// <summary> Get a random amount of camera rays </summary>
        /// <param name="amount">The amount of random rays to get</param>
        /// <returns>An array with random camera rays</returns>
        public CameraRay[] GetRandomCameraRays(int amount) {
            CameraRay[] rays = new CameraRay[amount];
            for (int i = 0; i < amount; i++) {
                int x = Utils.Random.Next(0, ScreenPlane.Screen.Width);
                int y = Utils.Random.Next(0, ScreenPlane.Screen.Height);
                rays[i] = CreateCameraRay(x, y);
            }
            return rays;
        }

        /// <summary> Create a primary ray from the camera through a pixel on the screen plane </summary>
        /// <param name="x">The x position of the pixel</param>
        /// <param name="y">The y position of the pixel</param>
        /// <returns>The ray from the camera through the screen plane</returns>
        public CameraRay CreateCameraRay(int x, int y) {
            return new CameraRay(Position, ScreenPlane.GetPixelPosition(x, y) - Position, ScreenPlane.Accumulator.Cavities[x + y * ScreenPlane.Screen.Width]);
        }
    }
}