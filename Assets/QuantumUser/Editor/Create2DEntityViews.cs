using UnityEngine;
using UnityEditor;
using Quantum;
using System.IO;

namespace QuantumUser.Editor
{
    public class Create2DEntityViews
    {
        [MenuItem("Quantum/2D Battle/Create EntityView Assets")]
        public static void CreateEntityViewAssets()
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
            string entityViewPath = "Assets/QuantumUser/Resources/DB/EntityViews/";

            // EntityViews 디렉토리가 없으면 생성
            if (!Directory.Exists(entityViewPath))
            {
                Directory.CreateDirectory(entityViewPath);
            }

            int createdCount = 0;
            int skippedCount = 0;

            foreach (string characterName in characterNames)
            {
                string assetPath = entityViewPath + "EntityView_" + characterName + ".asset";

                // 이미 존재하면 스킵
                if (File.Exists(assetPath))
                {
                    Debug.Log($"⏭️ EntityView already exists: {characterName}");
                    skippedCount++;
                    continue;
                }

                // 프리팹 로드
                string prefabFullPath = prefabPath + characterName + ".prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabFullPath);

                if (prefab == null)
                {
                    Debug.LogError($"❌ Prefab not found: {prefabFullPath}");
                    continue;
                }

                // EntityView 에셋 생성
                EntityView entityView = ScriptableObject.CreateInstance<EntityView>();
                entityView.name = "EntityView_" + characterName;

                // Prefab 참조 설정
                entityView.Prefab = prefab;

                // 에셋으로 저장
                AssetDatabase.CreateAsset(entityView, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"✅ Created EntityView: {characterName} at {assetPath}");
                createdCount++;
            }

            Debug.Log($"🎉 EntityView 생성 완료! Created: {createdCount}, Skipped: {skippedCount}");
        }
    }
}
