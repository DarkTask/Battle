using UnityEngine;
using UnityEditor;
using Quantum;
using System.IO;

namespace QuantumUser.Editor
{
    public class Create2DChampionPrototypes
    {
        [MenuItem("Quantum/2D Battle/Create ChampionPrototype Assets")]
        public static void CreateChampionPrototypes()
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

            string prototypePath = "Assets/QuantumUser/Resources/DB/Prototypes/";
            string entityViewPath = "Assets/QuantumUser/Resources/DB/EntityViews/";

            // Prototypes 디렉토리가 없으면 생성
            if (!Directory.Exists(prototypePath))
            {
                Directory.CreateDirectory(prototypePath);
            }

            int createdCount = 0;
            int skippedCount = 0;

            foreach (string characterName in characterNames)
            {
                string assetPath = prototypePath + "ChampionPrototype_" + characterName + ".asset";

                // 이미 존재하면 스킵
                if (File.Exists(assetPath))
                {
                    Debug.Log($"⏭️ ChampionPrototype already exists: {characterName}");
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

                // EntityPrototype 생성
                EntityPrototype prototype = ScriptableObject.CreateInstance<EntityPrototype>();
                prototype.name = "ChampionPrototype_" + characterName;

                // 컴포넌트 프로토타입 리스트 생성
                var components = new System.Collections.Generic.List<Quantum.ComponentPrototype>();

                // Transform3D 컴포넌트 추가 (2D이므로 Y=0 고정)
                var transform3D = new Quantum.Prototypes.Transform3DPrototype();
                transform3D.Position = new Photon.Deterministic.FPVector3(
                    Photon.Deterministic.FP._0,
                    Photon.Deterministic.FP._0,
                    Photon.Deterministic.FP._0
                );
                components.Add(transform3D);

                // NavMeshPathfinder 추가 (AI 이동용)
                var pathfinder = new Quantum.Prototypes.NavMeshPathfinderPrototype();
                components.Add(pathfinder);

                // NavMeshSteeringAgent 추가
                var steering = new Quantum.Prototypes.NavMeshSteeringAgentPrototype();
                components.Add(steering);

                // View 컴포넌트 추가 (EntityView 연결)
                var viewPrototype = new Quantum.Prototypes.ViewPrototype();
                viewPrototype.Current = entityViewAsset.Identifier.Guid;
                components.Add(viewPrototype);

                // Container에 컴포넌트 설정
                prototype.Container = Quantum.ComponentPrototypeSet.FromArray(components.ToArray());

                // 에셋으로 저장
                AssetDatabase.CreateAsset(prototype, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"✅ Created ChampionPrototype: {characterName} at {assetPath}");
                createdCount++;
            }

            Debug.Log($"🎉 ChampionPrototype 생성 완료! Created: {createdCount}, Skipped: {skippedCount}");
        }
    }
}
