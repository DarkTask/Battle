using UnityEngine;
using UnityEditor;
using Quantum;
using Photon.Deterministic;

[CustomEditor(typeof(BattleGameConfig))]
public class BattleGameConfigEditor : Editor
{
    private ChampionDatabase championDatabase;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var config = (BattleGameConfig)target;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Champion Stats Sync", EditorStyles.boldLabel);

        // ChampionDatabase 참조 필드
        championDatabase = (ChampionDatabase)EditorGUILayout.ObjectField(
            "Champion Database",
            championDatabase,
            typeof(ChampionDatabase),
            false
        );

        // 자동으로 찾기 버튼
        if (GUILayout.Button("Find ChampionDatabase"))
        {
            string[] guids = AssetDatabase.FindAssets("t:ChampionDatabase");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                championDatabase = AssetDatabase.LoadAssetAtPath<ChampionDatabase>(path);
                Debug.Log($"✅ ChampionDatabase 찾음: {path}");
            }
            else
            {
                Debug.LogError("❌ ChampionDatabase를 찾을 수 없습니다!");
            }
        }

        EditorGUILayout.Space(10);

        // 동기화 버튼
        GUI.enabled = championDatabase != null;
        if (GUILayout.Button("Sync from ChampionDatabase", GUILayout.Height(30)))
        {
            SyncFromChampionDatabase(config);
        }
        GUI.enabled = true;

        if (championDatabase == null)
        {
            EditorGUILayout.HelpBox("ChampionDatabase를 할당하거나 'Find ChampionDatabase' 버튼을 클릭하세요.", MessageType.Info);
        }
    }

    void SyncFromChampionDatabase(BattleGameConfig config)
    {
        if (championDatabase == null)
        {
            Debug.LogError("ChampionDatabase가 없습니다!");
            return;
        }

        int count = championDatabase.GetChampionCount();
        config.ChampionStats = new ChampionBattleStats[count];

        for (int i = 0; i < count; i++)
        {
            var champion = championDatabase.GetChampion(i);
            if (champion != null)
            {
                config.ChampionStats[i] = new ChampionBattleStats
                {
                    MaxHealth = FP.FromFloat_UNSAFE(champion.maxHealth),
                    AttackPower = FP.FromFloat_UNSAFE(champion.attackPower),
                    AttackRange = FP.FromFloat_UNSAFE(champion.attackRange),
                    AttackSpeed = FP.FromFloat_UNSAFE(champion.attackSpeed),
                    MoveSpeed = FP.FromFloat_UNSAFE(champion.moveSpeed)
                };

                Debug.Log($"✅ [{i}] {champion.championName}: HP={champion.maxHealth}, ATK={champion.attackPower}, Range={champion.attackRange}, Speed={champion.moveSpeed}");
            }
        }

        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ {count}개 챔피언 스탯 동기화 완료! (ChampionDatabase → BattleGameConfig)");
    }
}
