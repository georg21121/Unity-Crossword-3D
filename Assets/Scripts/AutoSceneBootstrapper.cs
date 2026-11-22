using UnityEngine;

namespace Crossword3D
{
    /// <summary>
    /// Guarantees that even if placed in a completely empty scene,
    /// the full 3D Crossword game, lighting, camera, audio and UI will initialize immediately!
    /// </summary>
    public class AutoSceneBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            SetupSceneLighting();
            SetupCamera();
            SetupGameManagers();
        }

        public static void EnsureSceneInitialized()
        {
            if (FindAnyObjectByType<CrosswordManager>() != null)
                return;

            GameObject bootstrapObj = new GameObject("[3D_Crossword_Bootstrapper]");
            bootstrapObj.AddComponent<AutoSceneBootstrapper>();
        }

        private void SetupSceneLighting()
        {
            Light mainLight = FindAnyObjectByType<Light>();
            if (mainLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                mainLight = lightObj.AddComponent<Light>();
                mainLight.type = LightType.Directional;
                mainLight.color = new Color(1.0f, 0.96f, 0.90f);
                mainLight.intensity = 1.15f;
                mainLight.shadows = LightShadows.Soft;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.35f);
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                cam.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f); // Deep cozy studio background

            if (cam.GetComponent<CameraOrbitController>() == null)
            {
                cam.gameObject.AddComponent<CameraOrbitController>();
            }
        }

        private void SetupGameManagers()
        {
            GameObject managersObj = new GameObject("GameController");

            if (FindAnyObjectByType<AudioManager>() == null)
                managersObj.AddComponent<AudioManager>();

            if (FindAnyObjectByType<BoardGenerator3D>() == null)
                managersObj.AddComponent<BoardGenerator3D>();

            if (FindAnyObjectByType<CrosswordUI>() == null)
                managersObj.AddComponent<CrosswordUI>();

            if (FindAnyObjectByType<CrosswordManager>() == null)
                managersObj.AddComponent<CrosswordManager>();
        }
    }
}
