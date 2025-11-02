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
    public float moveSpeed = 5f;
    public bool isAlive = true;
}

