using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class BattleGameManager : NetworkBehaviour
{
    public static BattleGameManager Instance;
    
    [Header("Settings")]
    public bool soloTestMode = true;
    public ChampionDatabase championDB;
    
    [Header("Game State")]
    [SyncVar] public GameState currentState = GameState.CharacterSelect;
    [SyncVar] public int currentTurn = 0; // 0-5 (총 6턴)
    [SyncVar] public float turnTimer = 3f;
    
    [Header("Player Data")]
    public PlayerGameData playerA;
    public PlayerGameData playerB;
    
    [Header("Debug")]
    public bool skipTimer = false;
    public KeyCode debugAutoSelectKey = KeyCode.F1;
    
    // Constants
    private const float TURN_TIME_LIMIT = 3f;
    private const int TOTAL_TURNS = 6;
    private const int CHAMPIONS_PER_PLAYER = 3;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializePlayerData();
    }
    
    void InitializePlayerData()
    {
        playerA = new PlayerGameData { playerIndex = 0 };
        playerB = new PlayerGameData { playerIndex = 1 };
    }
    
    void Start()
    {
        if (soloTestMode)
        {
            Debug.Log("🎮 Solo Test Mode: 1인 테스트 모드 활성화");
            Debug.Log("📌 F1 키: 자동 선택 완료");
        }
        
        if (isServer)
        {
            StartCharacterSelect();
        }
    }
    
    void Update()
    {
        // 디버그 키
        if (Input.GetKeyDown(debugAutoSelectKey))
        {
            DebugAutoSelectAll();
        }
        
        if (!isServer) return;
        
        if (currentState == GameState.CharacterSelect)
        {
            UpdateCharacterSelectTimer();
        }
    }
    
    #region Character Select Phase
    
    [Server]
    void StartCharacterSelect()
    {
        currentState = GameState.CharacterSelect;
        currentTurn = 0;
        turnTimer = TURN_TIME_LIMIT;
        
        Debug.Log("🎯 캐릭터 선택 시작!");
        RpcUpdateUI();
    }
    
    [Server]
    void UpdateCharacterSelectTimer()
    {
        if (skipTimer) return;
        
        turnTimer -= Time.deltaTime;
        
        if (turnTimer <= 0)
        {
            Debug.Log($"⏰ 시간 초과! 자동 선택 (Turn {currentTurn})");
            AutoSelectChampion();
            NextTurn();
        }
    }
    
    [Command(requiresAuthority = false)]
    public void CmdSelectChampion(int championIndex, NetworkConnectionToClient sender = null)
    {
        if (currentState != GameState.CharacterSelect)
        {
            Debug.LogWarning("현재 캐릭터 선택 단계가 아닙니다.");
            return;
        }
        
        if (IsChampionAlreadySelected(championIndex))
        {
            Debug.LogWarning($"이미 선택된 챔피언입니다: {championIndex}");
            return;
        }
        
        // 현재 턴의 플레이어 결정
        int currentPlayer = currentTurn % 2; // 0=A, 1=B
        
        // 챔피언 데이터 가져오기
        ChampionData champion = championDB.GetChampion(championIndex);
        if (champion == null)
        {
            Debug.LogError($"유효하지 않은 챔피언 인덱스: {championIndex}");
            return;
        }
        
        // 챔피언 추가
        if (currentPlayer == 0)
            playerA.selectedChampions.Add(champion);
        else
            playerB.selectedChampions.Add(champion);
        
        Debug.Log($"✅ Player {(currentPlayer == 0 ? "A" : "B")} 선택: {champion.championName} (Turn {currentTurn + 1}/6)");
        
        // UI 업데이트
        RpcUpdateChampionSelection(championIndex, currentPlayer);
        
        // 다음 턴
        NextTurn();
    }
    
    [Server]
    void AutoSelectChampion()
    {
        List<int> available = GetAvailableChampionIndices();
        if (available.Count == 0)
        {
            Debug.LogError("선택 가능한 챔피언이 없습니다!");
            return;
        }
        
        int randomIndex = available[Random.Range(0, available.Count)];
        int currentPlayer = currentTurn % 2;
        
        ChampionData champion = championDB.GetChampion(randomIndex);
        
        if (currentPlayer == 0)
            playerA.selectedChampions.Add(champion);
        else
            playerB.selectedChampions.Add(champion);
        
        Debug.Log($"🤖 자동 선택 - Player {(currentPlayer == 0 ? "A" : "B")}: {champion.championName}");
        
        RpcUpdateChampionSelection(randomIndex, currentPlayer);
    }
    
    [Server]
    void NextTurn()
    {
        currentTurn++;
        turnTimer = TURN_TIME_LIMIT;
        
        if (currentTurn >= TOTAL_TURNS)
        {
            // 선택 완료
            Debug.Log("✅ 캐릭터 선택 완료!");
            Debug.Log($"Player A: {string.Join(", ", playerA.selectedChampions.ConvertAll(c => c.championName))}");
            Debug.Log($"Player B: {string.Join(", ", playerB.selectedChampions.ConvertAll(c => c.championName))}");
            
            ChangeState(GameState.OrderSetup);
        }
        else
        {
            RpcUpdateUI();
        }
    }
    
    bool IsChampionAlreadySelected(int index)
    {
        foreach (var champ in playerA.selectedChampions)
            if (champ.id == index) return true;
        
        foreach (var champ in playerB.selectedChampions)
            if (champ.id == index) return true;
        
        return false;
    }
    
    List<int> GetAvailableChampionIndices()
    {
        List<int> available = new List<int>();
        int totalChampions = championDB.GetChampionCount();
        
        for (int i = 0; i < totalChampions; i++)
        {
            if (!IsChampionAlreadySelected(i))
                available.Add(i);
        }
        
        return available;
    }
    
    #endregion
    
    #region State Management
    
    [Server]
    void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"🎮 State Changed: {newState}");
        RpcOnStateChanged(newState);
    }
    
    [ClientRpc]
    void RpcOnStateChanged(GameState newState)
    {
        Debug.Log($"[Client] State: {newState}");
        
        switch (newState)
        {
            case GameState.OrderSetup:
                // TODO: OrderSetupPanel 표시
                Debug.Log("📋 전투 순서 지정 단계로 이동");
                break;
            
            case GameState.Battle_Round1:
                // TODO: Round 1 전투 시작
                Debug.Log("⚔️ Round 1 시작");
                break;
            
            case GameState.Result:
                // TODO: 결과 화면 표시
                Debug.Log("🏆 결과 화면");
                break;
        }
    }
    
    #endregion
    
    #region Network RPC
    
    [ClientRpc]
    void RpcUpdateUI()
    {
        if (CharacterSelectUI.Instance != null)
        {
            CharacterSelectUI.Instance.UpdateTurnDisplay(currentTurn);
            CharacterSelectUI.Instance.UpdateTimer(turnTimer);
        }
    }
    
    [ClientRpc]
    void RpcUpdateChampionSelection(int championIndex, int playerIndex)
    {
        if (CharacterSelectUI.Instance != null)
        {
            CharacterSelectUI.Instance.OnChampionSelected(championIndex, playerIndex);
        }
    }
    
    #endregion
    
    #region Debug Functions
    
    [ContextMenu("Debug: Auto Select All")]
    void DebugAutoSelectAll()
    {
        if (!isServer)
        {
            Debug.LogWarning("서버 전용 기능입니다.");
            return;
        }
        
        Debug.Log("🔧 디버그: 모든 챔피언 자동 선택");
        
        while (currentTurn < TOTAL_TURNS)
        {
            AutoSelectChampion();
            currentTurn++;
        }
        
        turnTimer = TURN_TIME_LIMIT;
        ChangeState(GameState.OrderSetup);
    }
    
    [ContextMenu("Debug: Reset Game")]
    void DebugResetGame()
    {
        if (!isServer) return;
        
        Debug.Log("🔄 게임 리셋");
        
        playerA.ClearData();
        playerB.ClearData();
        
        InitializePlayerData();
        StartCharacterSelect();
    }
    
    #endregion
    
    #region Public Methods
    
    public int GetCurrentPlayerIndex()
    {
        return currentTurn % 2;
    }
    
    public PlayerGameData GetCurrentPlayerData()
    {
        return GetCurrentPlayerIndex() == 0 ? playerA : playerB;
    }
    
    public PlayerGameData GetPlayerData(int playerIndex)
    {
        return playerIndex == 0 ? playerA : playerB;
    }
    
    #endregion
}

// GameState enum (파일 끝에 위치)
public enum GameState
{
    Lobby,
    CharacterSelect,
    OrderSetup,
    Battle_Round1,
    Battle_Round2,
    Battle_Round3,
    Battle_Final,
    Result
}
