using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ChampionDatabase", menuName = "Battle/Champion Database")]
public class ChampionDatabase : ScriptableObject
{
    public List<ChampionData> champions = new List<ChampionData>();
    
    public ChampionData GetChampion(int index)
    {
        if (index >= 0 && index < champions.Count)
            return champions[index];
        
        Debug.LogWarning($"Champion index {index} out of range!");
        return null;
    }
    
    public string GetChampionName(int index)
    {
        ChampionData champion = GetChampion(index);
        return champion != null ? champion.championName : "Unknown";
    }
    
    public Sprite GetChampionIcon(int index)
    {
        ChampionData champion = GetChampion(index);
        return champion != null ? champion.icon : null;
    }
    
    public int GetChampionCount()
    {
        return champions.Count;
    }
    
    // 에디터에서 초기 데이터 생성용
#if UNITY_EDITOR
    [ContextMenu("Initialize 12 Champions")]
    void InitializeDefaultChampions()
    {
        Debug.Log("🔧 12개 챔피언 초기화 시작...");
        
        champions.Clear();
        
        string[] championNames = new string[]
        {
            "Aatrox", "Ahri", "Ashe", "Caitlyn", 
            "Galio", "Garen", "Irelia", "Jhin",
            "Kassadin", "KogMaw", "Lucian", "MasterYi"
        };
        
        for (int i = 0; i < championNames.Length; i++)
        {
            ChampionData newChampion = new ChampionData
            {
                id = i,
                championName = championNames[i],
                strength = Random.Range(8, 15),
                dexterity = Random.Range(8, 15),
                constitution = Random.Range(8, 15),
                maxHealth = 100,
                attackPower = Random.Range(8, 12),
                moveSpeed = 5f,
                isAlive = true
            };
            
            champions.Add(newChampion);
            Debug.Log($"  [{i}] {newChampion.championName} - STR:{newChampion.strength} DEX:{newChampion.dexterity} CON:{newChampion.constitution}");
        }
        
        Debug.Log($"✅ {championNames.Length}개 챔피언 데이터 생성 완료!");
        Debug.Log($"📊 현재 Champions 리스트 크기: {champions.Count}");
        
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
        
        Debug.Log("💾 저장 완료! Inspector를 확인하세요.");
    }
    
    [ContextMenu("Check Champion Count")]
    void CheckChampionCount()
    {
        Debug.Log($"📊 현재 ChampionDatabase 챔피언 수: {champions.Count}개");
        
        if (champions.Count == 0)
        {
            Debug.LogWarning("⚠️ 챔피언이 없습니다! 'Initialize 12 Champions'를 실행하세요.");
        }
        else
        {
            for (int i = 0; i < champions.Count; i++)
            {
                Debug.Log($"  [{i}] {champions[i].championName}");
            }
        }
    }
#endif
}

