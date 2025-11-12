using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace QuantumUser.Editor
{
    public class Add2DViewComponents
    {
        [MenuItem("Quantum/2D Battle/Add 2D View Components to Prefabs")]
        public static void AddViewComponents()
        {
            string[] characterNames = new string[]
            {
                "Knight",
                "Paladin",
                "DeathKnight",
                "DarkLord",
                "Archer",
                "CamoArcher",
                "LongBow",
                "Mage",
                "Wizard"
            };

            string prefabPath = "Assets/QuantumUser/Resources/Prefabs/";

            // Quantum2DEntityView 타입 찾기
            var quantum2DViewType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "Quantum2DEntityView");

            if (quantum2DViewType == null)
            {
                Debug.LogError("❌ Quantum2DEntityView type not found! Please wait for Unity to compile the script first.");
                return;
            }

            int addedCount = 0;
            int skippedCount = 0;

            foreach (string characterName in characterNames)
            {
                string prefabFullPath = prefabPath + characterName + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFullPath);

                if (prefab == null)
                {
                    Debug.LogError($"❌ Prefab not found: {prefabFullPath}");
                    continue;
                }

                // 이미 Quantum2DEntityView 컴포넌트가 있는지 확인
                var existingView = prefab.GetComponent(quantum2DViewType);
                if (existingView != null)
                {
                    Debug.Log($"⏭️ Quantum2DEntityView already exists on: {characterName}");
                    skippedCount++;
                    continue;
                }

                // Prefab 수정 시작
                string prefabAssetPath = AssetDatabase.GetAssetPath(prefab);
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabAssetPath);

                // Quantum2DEntityView 컴포넌트 추가
                prefabContents.AddComponent(quantum2DViewType);

                // Prefab 저장
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabAssetPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                Debug.Log($"✅ Added Quantum2DEntityView to: {characterName}");
                addedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"🎉 Quantum2DEntityView 컴포넌트 추가 완료! Added: {addedCount}, Skipped: {skippedCount}");
        }
    }
}
