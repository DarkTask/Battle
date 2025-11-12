using UnityEngine;
using UnityEditor;
using Quantum;
using System.IO;

namespace QuantumUser.Editor
{
    public class Create2DBattleGameConfig
    {
        [MenuItem("Quantum/2D Battle/Create BattleGameConfig Asset")]
        public static void CreateBattleGameConfig()
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

            string configPath = "Assets/QuantumUser/Resources/BattleGameConfig_2D.asset";
            string prototypePath = "Assets/QuantumUser/Resources/DB/Prototypes/";

            // 이미 존재하면 스킵
            if (File.Exists(configPath))
            {
                Debug.Log($"⏭️ BattleGameConfig already exists: {configPath}");
                return;
            }

            // BattleGameConfig 생성
            BattleGameConfig config = ScriptableObject.CreateInstance<BattleGameConfig>();
            config.name = "BattleGameConfig_2D";

            // ChampionPrototypes 배열 초기화 (9개)
            config.ChampionPrototypes = new Quantum.AssetRef<Quantum.EntityPrototype>[9];

            for (int i = 0; i < characterNames.Length; i++)
            {
                string characterName = characterNames[i];
                string prototypeAssetPath = prototypePath + "ChampionPrototype_" + characterName + ".asset";

                // EntityPrototype 로드
                EntityPrototype prototype = AssetDatabase.LoadAssetAtPath<EntityPrototype>(prototypeAssetPath);

                if (prototype == null)
                {
                    Debug.LogError($"❌ ChampionPrototype asset not found: {prototypeAssetPath}");
                    continue;
                }

                // AssetRef 설정
                config.ChampionPrototypes[i] = new Quantum.AssetRef<Quantum.EntityPrototype>
                {
                    Id = prototype.Identifier.Guid
                };

                Debug.Log($"✅ Added Champion {i}: {characterName} (GUID: {prototype.Identifier.Guid.Value})");
            }

            // 스폰 위치 설정 (2D: X, Z 사용, Y=0)
            // TeamA (teamId=0): 왼쪽 (-2.5, 0, 0), TeamB (teamId=1): 오른쪽 (2.5, 0, 0)
            // 거리: 5.0 유닛 (AttackRange=2보다 멀어서 서로에게 이동한 후 공격)
            // 중앙(X=0) 부근에서 만나게 됨
            config.SpawnPositions = new Photon.Deterministic.FPVector3[6];

            // Team A (왼쪽) - 모두 Z=0 (Unity Y=0, 일직선)
            config.SpawnPositions[0] = new Photon.Deterministic.FPVector3(
                Photon.Deterministic.FP.FromFloat_UNSAFE(-2.5f),
                Photon.Deterministic.FP._0,
                Photon.Deterministic.FP._0
            );
            config.SpawnPositions[1] = new Photon.Deterministic.FPVector3(
                Photon.Deterministic.FP.FromFloat_UNSAFE(-2.5f),
                Photon.Deterministic.FP._0,
                Photon.Deterministic.FP._0
            );
            config.SpawnPositions[2] = new Photon.Deterministic.FPVector3(
                Photon.Deterministic.FP.FromFloat_UNSAFE(-2.5f),
                Photon.Deterministic.FP._0,
                Photon.Deterministic.FP._0
            );

            // Team B (오른쪽) - 모두 Z=0 (Unity Y=0, 일직선)
            config.SpawnPositions[3] = new Photon.Deterministic.FPVector3(
                Photon.Deterministic.FP.FromFloat_UNSAFE(2.5f),
                Photon.Deterministic.FP._0,
                Photon.Deterministic.FP._0
            );
            config.SpawnPositions[4] = new Photon.Deterministic.FPVector3(
                Photon.Deterministic.FP.FromFloat_UNSAFE(2.5f),
                Photon.Deterministic.FP._0,
                Photon.Deterministic.FP._0
            );
            config.SpawnPositions[5] = new Photon.Deterministic.FPVector3(
                Photon.Deterministic.FP.FromFloat_UNSAFE(2.5f),
                Photon.Deterministic.FP._0,
                Photon.Deterministic.FP._0
            );

            // 타이머 설정
            config.SelectTimeLimit = Photon.Deterministic.FP.FromFloat_UNSAFE(0.3f);  // 빠른 테스트: 0.3초
            config.BattleTimeLimit = Photon.Deterministic.FP.FromFloat_UNSAFE(999f);  // 999초 (자동 종료 안됨)

            // 에셋으로 저장
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"🎉 BattleGameConfig 생성 완료! Path: {configPath}");
        }
    }
}
