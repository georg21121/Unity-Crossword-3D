#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Crossword3D.Editor
{
    public class CrosswordSceneBuilder : EditorWindow
    {
        [MenuItem("Tools/3D Crossword/Generate Complete Game Scene", false, 1)]
        public static void GenerateScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Внимание", 
                    "Вы находитесь в режиме воспроизведения (Play Mode).\n\nНажмите кнопку Play (▶) вверху по центру, чтобы остановить игру, и затем снова запустите этот пункт меню!", "Понятно");
                return;
            }

            // Create a fresh scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Setup Camera
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camObj.AddComponent<AudioListener>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f);
            cam.transform.position = new Vector3(0, 10, -10);
            cam.transform.rotation = Quaternion.Euler(45, 0, 0);
            CameraOrbitController orbit = camObj.AddComponent<CameraOrbitController>();

            // 2. Setup Directional Light
            GameObject lightObj = new GameObject("Directional Light");
            Light dirLight = lightObj.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.color = new Color(1.0f, 0.96f, 0.90f);
            dirLight.intensity = 1.15f;
            dirLight.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.30f, 0.35f);

            // 3. Setup Game Controller & Managers
            GameObject controllerObj = new GameObject("CrosswordGameController");
            AudioManager audioMgr = controllerObj.AddComponent<AudioManager>();
            BoardGenerator3D boardGen = controllerObj.AddComponent<BoardGenerator3D>();
            CrosswordUI uiMgr = controllerObj.AddComponent<CrosswordUI>();
            CrosswordManager gameMgr = controllerObj.AddComponent<CrosswordManager>();

            // 4. Ensure Directory & Save Scene
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            string scenePath = "Assets/Scenes/CrosswordGameScene.unity";
            EditorSceneManager.SaveScene(newScene, scenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("3D Crossword", 
                "Сцена успешно создана и сохранена по пути:\n" + scenePath + "\n\nНажмите 'Play' в Unity Editor для запуска игры!", "OK");
        }
    }
}
#endif
