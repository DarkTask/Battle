using UnityEngine;

[System.Serializable]
public class ChampionData
{
    public int id;
    public string championName;
    public Sprite icon;
    public Sprite characterImage;
    public GameObject championPrefab;
    
    [Header("Stats")]
    public int strength = 10;
    public int dexterity = 10;
    public int constitution = 10;
    
    [Header("Battle Stats")]
    public int maxHealth = 100;
    public int attackPower = 10;
    public float attackRange = 0.6f;  // 근접: 0.6, 원거리: 2.0~3.0
    public float attackSpeed = 1f;    // 초당 공격 횟수
    public float moveSpeed = 5f;
    public bool isAlive = true;

    /// <summary>
    /// 원거리 챔피언인지 확인 (attackRange > 1.0)
    /// </summary>
    public bool IsRanged => attackRange > 1.0f;
}

