using UnityEngine;
using Mirror;

/// <summary>
/// 전투 중인 캐릭터 개별 제어
/// HP, 애니메이션, 데미지 처리 등
/// </summary>
public class BattleCharacterController : NetworkBehaviour
{
    [Header("Champion Info")]
    [SyncVar] public string championName = "Champion";
    [SyncVar] public int maxHealth = 100;
    [SyncVar(hook = nameof(OnHealthChanged))] 
    public int currentHealth = 100;
    [SyncVar] public int playerIndex = 0; // 0=A, 1=B
    
    [Header("UI")]
    public Canvas healthCanvas;
    public TMPro.TextMeshProUGUI healthText;
    public UnityEngine.UI.Image healthBar;
    
    [Header("Visual")]
    public Animator animator;
    public Renderer characterRenderer;
    
    void Start()
    {
        UpdateHealthUI();
    }
    
    /// <summary>
    /// 챔피언 초기화
    /// </summary>
    [Server]
    public void InitializeChampion(string name, int hp, int index)
    {
        championName = name;
        maxHealth = hp;
        currentHealth = hp;
        playerIndex = index;
        
        Debug.Log($"✅ {championName} 초기화: HP={hp}, Player={index}");
        
        // 색상 구분 (Player A=빨강, Player B=파랑)
        RpcSetColor(index == 0 ? Color.red : Color.blue);
    }
    
    /// <summary>
    /// 데미지 받기
    /// </summary>
    [Server]
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        
        Debug.Log($"💥 {championName} takes {damage} damage! HP: {currentHealth}/{maxHealth}");
        
        // 공격받는 애니메이션
        RpcPlayHitAnimation();
    }
    
    /// <summary>
    /// 체력 변경 Hook
    /// </summary>
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        UpdateHealthUI();
        
        if (newHealth <= 0)
        {
            OnDeath();
        }
    }
    
    /// <summary>
    /// 체력 UI 업데이트
    /// </summary>
    void UpdateHealthUI()
    {
        if (healthText != null)
        {
            healthText.text = $"{championName}\n{currentHealth}/{maxHealth}";
        }
        
        if (healthBar != null && maxHealth > 0)
        {
            healthBar.fillAmount = (float)currentHealth / maxHealth;
        }
    }
    
    /// <summary>
    /// 사망 처리
    /// </summary>
    void OnDeath()
    {
        Debug.Log($"💀 {championName} defeated!");
        
        // 사망 애니메이션
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger("Death");
        }
        
        // 색상 어둡게
        if (characterRenderer != null)
        {
            characterRenderer.material.color = Color.gray;
        }
    }
    
    /// <summary>
    /// 색상 설정
    /// </summary>
    [ClientRpc]
    void RpcSetColor(Color color)
    {
        if (characterRenderer != null)
        {
            characterRenderer.material.color = color;
        }
    }
    
    /// <summary>
    /// 피격 애니메이션
    /// </summary>
    [ClientRpc]
    void RpcPlayHitAnimation()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetTrigger("Hit");
        }
        
        // 간단한 흔들림 효과
        StartCoroutine(ShakeEffect());
    }
    
    /// <summary>
    /// 흔들림 효과
    /// </summary>
    System.Collections.IEnumerator ShakeEffect()
    {
        Vector3 originalPos = transform.position;
        float duration = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-0.1f, 0.1f);
            float z = Random.Range(-0.1f, 0.1f);
            transform.position = originalPos + new Vector3(x, 0, z);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPos;
    }
}

