using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 전투 아레나 관리자
/// 3라운드 전투를 순차적으로 진행하고 승패를 판정
/// </summary>
public class BattleArenaManager : NetworkBehaviour
{
    public static BattleArenaManager Instance;
    
    [Header("Battle Arena Settings")]
    public Transform playerASpawnPoint;     // Player A 캐릭터 스폰 위치
    public Transform playerBSpawnPoint;     // Player B 캐릭터 스폰 위치
    public GameObject defaultCharacterPrefab; // Character01.prefab (기본 모델)
    
    [Header("Battle State")]
    [SyncVar] public int currentRound = 0;  // 0=준비, 1~3=라운드
    [SyncVar] public int playerAWins = 0;
    [SyncVar] public int playerBWins = 0;
    
    [Header("Current Battle")]
    private GameObject currentPlayerACharacter;
    private GameObject currentPlayerBCharacter;
    private BattleCharacterController playerAController;
    private BattleCharacterController playerBController;
    
    [Header("References")]
    public Canvas battleCanvas;
    public TMPro.TextMeshProUGUI roundText;
    public TMPro.TextMeshProUGUI resultText;
    
    private Mirror.Examples.MultipleMatch.MatchController matchController;
    
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    void Start()
    {
        battleCanvas?.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 전투 시작 (MatchController에서 호출)
    /// </summary>
    [Server]
    public void StartBattle(Mirror.Examples.MultipleMatch.MatchController controller)
    {
        matchController = controller;
        
        Debug.Log("🥊 BattleArenaManager: 전투 시작!");
        
        // Round 1 시작
        currentRound = 1;
        playerAWins = 0;
        playerBWins = 0;
        
        RpcShowBattleArena();
        StartCoroutine(StartRound(1));
    }
    
    /// <summary>
    /// 전투 아레나 UI 표시
    /// </summary>
    [ClientRpc]
    void RpcShowBattleArena()
    {
        if (battleCanvas != null)
        {
            battleCanvas.gameObject.SetActive(true);
        }
        
        Debug.Log("🎬 전투 아레나 활성화");
    }
    
    /// <summary>
    /// 라운드 시작
    /// </summary>
    [Server]
    IEnumerator StartRound(int round)
    {
        currentRound = round;
        Debug.Log($"⚔️ Round {round} 시작!");
        
        RpcUpdateRoundText($"Round {round}");
        
        yield return new WaitForSeconds(2f);
        
        // 이전 라운드 캐릭터 제거
        CleanupPreviousRound();
        
        // 챔피언 정보 가져오기
        CardElement playerACard = GetChampionForRound(0, round - 1);
        CardElement playerBCard = GetChampionForRound(1, round - 1);
        
        if (playerACard == null || playerBCard == null)
        {
            Debug.LogError($"Round {round}: 챔피언 데이터를 찾을 수 없습니다!");
            yield break;
        }
        
        // 캐릭터 스폰
        SpawnCharacters(playerACard, playerBCard, round);
        
        yield return new WaitForSeconds(1f);
        
        // 전투 시작
        StartCoroutine(RunBattle());
    }
    
    /// <summary>
    /// 특정 라운드의 챔피언 가져오기
    /// </summary>
    CardElement GetChampionForRound(int playerIndex, int roundIndex)
    {
        if (matchController == null || !matchController.DicCardElement.ContainsKey(playerIndex))
        {
            Debug.LogError($"Player {playerIndex} 데이터가 없습니다!");
            return null;
        }
        
        var cards = matchController.DicCardElement[playerIndex];
        var setupCards = new List<CardElement>();
        
        foreach (var card in cards)
        {
            if (card.isSetup)
                setupCards.Add(card);
        }
        
        // battleOrder에 따라 챔피언 순서 결정
        int[] battleOrder = playerIndex == 0 
            ? matchController.GetPlayer1BattleOrder() 
            : matchController.GetPlayer2BattleOrder();
        
        if (battleOrder == null || roundIndex >= battleOrder.Length)
        {
            Debug.LogError($"battleOrder가 올바르지 않습니다!");
            return null;
        }
        
        int championIndex = battleOrder[roundIndex];
        
        if (championIndex < 0 || championIndex >= setupCards.Count)
        {
            Debug.LogError($"championIndex={championIndex}가 범위를 벗어났습니다!");
            return null;
        }
        
        return setupCards[championIndex];
    }
    
    /// <summary>
    /// 캐릭터 스폰
    /// </summary>
    [Server]
    void SpawnCharacters(CardElement playerACard, CardElement playerBCard, int round)
    {
        // Player A 캐릭터 스폰
        Vector3 spawnPosA = playerASpawnPoint != null ? playerASpawnPoint.position : new Vector3(-3, 0, 0);
        currentPlayerACharacter = Instantiate(defaultCharacterPrefab, spawnPosA, Quaternion.Euler(0, 90, 0));
        
        // 디버그: 스폰 위치 확인
        Debug.Log($"🔹 Player A 스폰 위치: {spawnPosA}");
        
        NetworkServer.Spawn(currentPlayerACharacter);
        
        playerAController = currentPlayerACharacter.GetComponent<BattleCharacterController>();
        if (playerAController != null)
        {
            playerAController.InitializeChampion(playerACard.name.text, 100, 0);
        }
        else
        {
            Debug.LogError("❌ Player A: BattleCharacterController를 찾을 수 없습니다!");
        }
        
        // Player B 캐릭터 스폰
        Vector3 spawnPosB = playerBSpawnPoint != null ? playerBSpawnPoint.position : new Vector3(3, 0, 0);
        currentPlayerBCharacter = Instantiate(defaultCharacterPrefab, spawnPosB, Quaternion.Euler(0, -90, 0));
        
        // 디버그: 스폰 위치 확인
        Debug.Log($"🔹 Player B 스폰 위치: {spawnPosB}");
        
        NetworkServer.Spawn(currentPlayerBCharacter);
        
        playerBController = currentPlayerBCharacter.GetComponent<BattleCharacterController>();
        if (playerBController != null)
        {
            playerBController.InitializeChampion(playerBCard.name.text, 100, 1);
        }
        else
        {
            Debug.LogError("❌ Player B: BattleCharacterController를 찾을 수 없습니다!");
        }
        
        Debug.Log($"✅ 캐릭터 스폰: {playerACard.name.text} vs {playerBCard.name.text}");
        Debug.Log($"📍 스폰된 오브젝트: A={currentPlayerACharacter.name}, B={currentPlayerBCharacter.name}");
    }
    
    /// <summary>
    /// 전투 진행
    /// Option 1: AI 자동 전투 (AiBrain 활용)
    /// Option 2: 간단한 턴제 (현재)
    /// </summary>
    [Server]
    IEnumerator RunBattle()
    {
        Debug.Log("⚔️ 전투 시작!");
        RpcUpdateResultText("Fight!");
        
        yield return new WaitForSeconds(2f);
        
        // AiBrain이 있는지 확인
        var aiBrainA = currentPlayerACharacter?.GetComponent<MonoBehaviour>();
        var aiBrainB = currentPlayerBCharacter?.GetComponent<MonoBehaviour>();
        
        bool useAI = (aiBrainA != null && aiBrainA.GetType().Name.Contains("Brain")) ||
                     (aiBrainB != null && aiBrainB.GetType().Name.Contains("Brain"));
        
        if (useAI)
        {
            Debug.Log("🤖 AI 모드로 전투 진행 (AiBrain 감지됨)");
            // AI가 알아서 전투하도록 둠
            // 30초 대기 또는 한쪽 사망까지
            float elapsed = 0f;
            while (elapsed < 30f)
            {
                if (playerAController == null || playerBController == null)
                    break;
                
                if (playerAController.currentHealth <= 0)
                {
                    OnRoundEnd(1);
                    yield break;
                }
                
                if (playerBController.currentHealth <= 0)
                {
                    OnRoundEnd(0);
                    yield break;
                }
                
                elapsed += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
            
            // 타임아웃
            if (playerAController.currentHealth > playerBController.currentHealth)
                OnRoundEnd(0);
            else
                OnRoundEnd(1);
        }
        else
        {
            Debug.Log("🎲 간단한 턴제 모드로 전투 진행");
            // 간단한 턴제 전투
            int turnCount = 0;
            int maxTurns = 20;
            
            while (turnCount < maxTurns)
            {
                // Player A 공격
                if (playerAController != null && playerBController != null)
                {
                    int damage = Random.Range(10, 20);
                    playerBController.TakeDamage(damage);
                    Debug.Log($"🔴 {playerAController.championName} attacks for {damage} damage!");
                    
                    yield return new WaitForSeconds(1f);
                    
                    if (playerBController.currentHealth <= 0)
                    {
                        OnRoundEnd(0);
                        yield break;
                    }
                }
                
                yield return new WaitForSeconds(0.5f);
                
                // Player B 공격
                if (playerAController != null && playerBController != null)
                {
                    int damage = Random.Range(10, 20);
                    playerAController.TakeDamage(damage);
                    Debug.Log($"🔵 {playerBController.championName} attacks for {damage} damage!");
                    
                    yield return new WaitForSeconds(1f);
                    
                    if (playerAController.currentHealth <= 0)
                    {
                        OnRoundEnd(1);
                        yield break;
                    }
                }
                
                turnCount++;
                yield return new WaitForSeconds(0.5f);
            }
            
            // 타임아웃
            if (playerAController.currentHealth > playerBController.currentHealth)
                OnRoundEnd(0);
            else
                OnRoundEnd(1);
        }
    }
    
    /// <summary>
    /// 라운드 종료
    /// </summary>
    [Server]
    void OnRoundEnd(int winner)
    {
        if (winner == 0)
        {
            playerAWins++;
            Debug.Log($"🔴 Round {currentRound}: Player A 승리!");
            RpcUpdateResultText($"Round {currentRound}: Player A Wins!");
        }
        else
        {
            playerBWins++;
            Debug.Log($"🔵 Round {currentRound}: Player B 승리!");
            RpcUpdateResultText($"Round {currentRound}: Player B Wins!");
        }
        
        StartCoroutine(PrepareNextRound());
    }
    
    /// <summary>
    /// 다음 라운드 준비
    /// </summary>
    [Server]
    IEnumerator PrepareNextRound()
    {
        yield return new WaitForSeconds(3f);
        
        // 3라운드 중 2승 확인
        if (playerAWins >= 2)
        {
            OnBattleEnd(0);
        }
        else if (playerBWins >= 2)
        {
            OnBattleEnd(1);
        }
        else if (currentRound < 3)
        {
            // 다음 라운드
            StartCoroutine(StartRound(currentRound + 1));
        }
        else
        {
            // 3라운드 완료 - 승수로 판정
            OnBattleEnd(playerAWins > playerBWins ? 0 : 1);
        }
    }
    
    /// <summary>
    /// 전투 종료
    /// </summary>
    [Server]
    void OnBattleEnd(int winner)
    {
        Debug.Log($"🏆 전투 종료! Winner: Player {(winner == 0 ? "A" : "B")}");
        Debug.Log($"📊 최종 스코어: Player A {playerAWins} - {playerBWins} Player B");
        
        RpcShowFinalResult(winner, playerAWins, playerBWins);
        
        StartCoroutine(ReturnToLobby());
    }
    
    /// <summary>
    /// 최종 결과 표시
    /// </summary>
    [ClientRpc]
    void RpcShowFinalResult(int winner, int winsA, int winsB)
    {
        string winnerName = winner == 0 ? "Player A" : "Player B";
        if (resultText != null)
        {
            resultText.text = $"{winnerName} Wins!\nScore: {winsA} - {winsB}";
        }
    }
    
    /// <summary>
    /// 이전 라운드 정리
    /// </summary>
    [Server]
    void CleanupPreviousRound()
    {
        if (currentPlayerACharacter != null)
        {
            NetworkServer.Destroy(currentPlayerACharacter);
            currentPlayerACharacter = null;
        }
        
        if (currentPlayerBCharacter != null)
        {
            NetworkServer.Destroy(currentPlayerBCharacter);
            currentPlayerBCharacter = null;
        }
    }
    
    /// <summary>
    /// 로비로 복귀
    /// </summary>
    [Server]
    IEnumerator ReturnToLobby()
    {
        yield return new WaitForSeconds(5f);
        
        RpcHideBattleArena();
        
        // TODO: 로비로 복귀 로직
        Debug.Log("🚪 로비로 복귀");
    }
    
    [ClientRpc]
    void RpcHideBattleArena()
    {
        if (battleCanvas != null)
        {
            battleCanvas.gameObject.SetActive(false);
        }
    }
    
    [ClientRpc]
    void RpcUpdateRoundText(string text)
    {
        if (roundText != null)
        {
            roundText.text = text;
        }
    }
    
    [ClientRpc]
    void RpcUpdateResultText(string text)
    {
        if (resultText != null)
        {
            resultText.text = text;
        }
    }
}

