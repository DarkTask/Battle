using UnityEngine;
using UnityEditor;
using Quantum;

namespace QuantumUser.Editor
{
    public class Add2DEntityViewComponents
    {
        [MenuItem("Quantum/2D Battle/Add EntityView Components to Prefabs")]
        public static void AddEntityViewComponents()
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

                // 이미 QuantumEntityView 컴포넌트가 있는지 확인
                QuantumEntityView existingView = prefab.GetComponent<QuantumEntityView>();
                if (existingView != null)
                {
                    Debug.Log($"⏭️ QuantumEntityView already exists on: {characterName}");
                    skippedCount++;
                    continue;
                }

                // EntityView Asset 로드
                string entityViewAssetPath = entityViewPath + "EntityView_" + characterName + ".asset";
                EntityView entityViewAsset = AssetDatabase.LoadAssetAtPath<EntityView>(entityViewAssetPath);

                if (entityViewAsset == null)
                {
                    Debug.LogError($"❌ EntityView asset not found: {entityViewAssetPath}");
                    continue;
                }

                // Prefab 수정 시작
                string prefabAssetPath = AssetDatabase.GetAssetPath(prefab);
                GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabAssetPath);

                // QuantumEntityView 컴포넌트 추가
                QuantumEntityView entityView = prefabContents.AddComponent<QuantumEntityView>();

                // 설정
                entityView.BindBehaviour = QuantumEntityViewBindBehaviour.NonVerified;
                entityView.ManualDisposal = false;
                entityView.ViewFlags = 0; // 기본 플래그 (모든 기능 활성화)

                // Prefab 저장
                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabAssetPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                Debug.Log($"✅ Added QuantumEntityView to: {characterName}");
                addedCount++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"🎉 QuantumEntityView 컴포넌트 추가 완료! Added: {addedCount}, Skipped: {skippedCount}");
        }
    }
}
