using UnityEngine;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using MoonProject.Editor.Builders;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Builds RoverCameraRig.prefab, the game's main camera:
    /// <code>
    /// RoverCameraRig   RoverCameraRig (IGameSystem; initialise after the rover)
    ///   FollowTarget   moved above 07 every frame
    ///   MainCamera     Camera (tag MainCamera, URP post-processing on) + CinemachineBrain + AudioListener
    ///   DroneCamera    CinemachineCamera + OrbitalFollow + RotationComposer + RoverCameraBump
    ///                  + Decollider + Deoccluder
    /// </code>
    /// The bump extension is added before the terrain extensions so they still correct its offset.
    /// </summary>
    public static class RoverCameraRigBuilder
    {
        private const string MainCameraTag = "MainCamera";

        /// <summary>Slow, eased blends for future cinematic cameras (relic surfacing, upgrades): no cuts.</summary>
        private const float DefaultBlendSeconds = 2f;

        [MoonBuilder("Rover/CameraRig", 320)]
        public static void Build()
        {
            var tuning = BuildWiring.Require<RoverCameraTuning>(RoverAssetPaths.CameraTuning, "Rover/Tuning");
            using (var scratch = new BuilderScratchScene())
            {
                GameObject root = scratch.Create("RoverCameraRig");
                var rig = root.AddComponent<RoverCameraRig>();

                GameObject target = scratch.Create("FollowTarget", root.transform);

                GameObject cameraHost = scratch.Create("MainCamera", root.transform);
                cameraHost.tag = MainCameraTag;
                var camera = cameraHost.AddComponent<Camera>();
                camera.fieldOfView = tuning.BaseFov;
                camera.nearClipPlane = tuning.NearClip;
                camera.farClipPlane = tuning.FarClip;
                var cameraData = cameraHost.AddComponent<UniversalAdditionalCameraData>();
                cameraData.renderPostProcessing = true;
                var brain = cameraHost.AddComponent<CinemachineBrain>();
                brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.EaseInOut,
                    DefaultBlendSeconds);
                cameraHost.AddComponent<AudioListener>();

                GameObject drone = scratch.Create("DroneCamera", root.transform);
                var virtualCamera = drone.AddComponent<CinemachineCamera>();
                virtualCamera.Follow = target.transform;
                virtualCamera.LookAt = target.transform;
                virtualCamera.Lens.FieldOfView = tuning.BaseFov;
                virtualCamera.Lens.NearClipPlane = tuning.NearClip;
                virtualCamera.Lens.FarClipPlane = tuning.FarClip;
                var orbit = drone.AddComponent<CinemachineOrbitalFollow>();
                orbit.OrbitStyle = CinemachineOrbitalFollow.OrbitStyles.Sphere;
                orbit.Radius = tuning.Distance;
                var composer = drone.AddComponent<CinemachineRotationComposer>();
                var bump = drone.AddComponent<RoverCameraBump>();
                var decollider = drone.AddComponent<CinemachineDecollider>();
                var deoccluder = drone.AddComponent<CinemachineDeoccluder>();

                BuildWiring.Assign(rig,
                    ("_tuning", tuning),
                    ("_target", target.transform),
                    ("_viewCamera", camera),
                    ("_camera", virtualCamera),
                    ("_orbit", orbit),
                    ("_composer", composer),
                    ("_decollider", decollider),
                    ("_deoccluder", deoccluder),
                    ("_bump", bump));

                GeneratedAssets.SavePrefab(root, RoverAssetPaths.CameraRigPrefab);
            }
        }
    }
}
