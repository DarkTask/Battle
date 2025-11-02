using System.Collections.Generic;

public class PlayerGameData
{
    public int playerIndex;                                      // 0 = A, 1 = B
    public List<ChampionData> selectedChampions = new List<ChampionData>();
    public int[] battleOrder = new int[3] { -1, -1, -1 };       // 전투 순서 (-1 = 미지정)
    public int totalScore = 0;
    
    public ChampionData GetChampionAtSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return null;
        if (battleOrder[slotIndex] == -1) return null;
        
        int championIndex = battleOrder[slotIndex];
        if (championIndex >= 0 && championIndex < selectedChampions.Count)
            return selectedChampions[championIndex];
        
        return null;
    }
    
    public bool IsBattleOrderComplete()
    {
        return battleOrder[0] != -1 && battleOrder[1] != -1 && battleOrder[2] != -1;
    }
    
    public void ClearData()
    {
        selectedChampions.Clear();
        battleOrder = new int[3] { -1, -1, -1 };
        totalScore = 0;
    }
}

