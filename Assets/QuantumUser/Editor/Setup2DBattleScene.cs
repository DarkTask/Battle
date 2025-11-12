using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

namespace QuantumUser.Editor
{
    public class Setup2DBattleScene
    {
        [MenuItem("Quantum/2D Battle/Setup BattleTestScene_2D")]
        public static void SetupScene()
        {
            // 씬 열기
            string scenePath = "Assets/QuantumUser/Scenes/BattleTest/BattleTestScene_2D.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            if (!scene.IsValid())
            {
                Debug.LogError($"❌ Failed to open scene: {scenePath}");
                return;
            }

            Debug.Log($"✅ Opened scene: {scenePath}");

            // BattleTest2DStarter 타입 찾기 (Reflection 사용)
            var starterType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "BattleTest2DStarter");

            if (starterType == null)
            {
                Debug.LogError("❌ BattleTest2DStarter type not found! Please wait for Unity to compile the script first.");
                return;
            }

            // BattleTest2DStarter GameObject가 이미 있는지 확인
            GameObject[] rootObjects = scene.GetRootGameObjects();
            GameObject starterObject = null;

            foreach (var obj in rootObjects)
            {
                if (obj.name == "BattleTest2DStarter")
                {
                    starterObject = obj;
                    Debug.Log("⏭️ BattleTest2DStarter already exists in scene");
                    break;
                }
            }

            // 없으면 생성
            if (starterObject == null)
            {
                starterObject = new GameObject("BattleTest2DStarter");
                var starter = starterObject.AddComponent(starterType);

                // Reflection을 사용한 필드 설정
                SetField(starter, "autoStart", true);
                SetField(starter, "startDelay", 1f);
                SetField(starter, "playerAChampion1", 0);
                SetField(starter, "playerAChampion2", 1);
                SetField(starter, "playerAChampion3", 2);
                SetField(starter, "playerBChampion1", 3);
                SetField(starter, "playerBChampion2", 4);
                SetField(starter, "playerBChampion3", 5);

                Debug.Log("✅ Created BattleTest2DStarter GameObject");
            }

            // 씬 저장
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("🎉 BattleTestScene_2D setup complete!");
            Debug.Log("   Press Play to start 2D battle test automatically");
            Debug.Log("   Or press Space to start manually");
            Debug.Log("   Press F2 to force next round during battle");
        }

        static void SetField(Component component, string fieldName, object value)
        {
            var field = component.GetType().GetField(fieldName);
            if (field != null)
            {
                field.SetValue(component, value);
            }
        }
    }
}
