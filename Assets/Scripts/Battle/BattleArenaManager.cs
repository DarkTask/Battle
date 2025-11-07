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
        Debug.Log($"🔧 BattleArenaManager.Awake() (isServer={isServer}, isClient={isClient}, GameObject={gameObject.name})");

        if (Instance == null)
        {
            Instance = this;
            Debug.Log("   ✅ BattleArenaManager.Instance 설정 완료 (동적 생성)");

            // 참고: 이제 Prefab으로 동적 생성되므로 sceneId는 0입니다.
            // MatchController가 전투 종료 시 파괴 담당
        }
        else
        {
            Debug.LogWarning($"   ⚠️ BattleArenaManager.Instance가 이미 존재합니다! (기존: {Instance.gameObject.name}, 새: {gameObject.name})");
            Debug.LogWarning($"   ⚠️ 이 오브젝트 파괴: {gameObject.name}");
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        Debug.Log($"🟢 BattleArenaManager.OnEnable() (isServer={isServer}, isClient={isClient})");
    }

    void OnDisable()
    {
        Debug.Log($"🔴 BattleArenaManager.OnDisable() (isServer={isServer}, isClient={isClient})");
        Debug.Log($"   호출 스택:\n{System.Environment.StackTrace}");
    }

    void Start()
    {
        Debug.Log($"🔧 BattleArenaManager.Start() (isServer={isServer}, isClient={isClient})");

        if (battleCanvas != null)
        {
            battleCanvas.gameObject.SetActive(false);
            Debug.Log("   ✅ BattleCanvas 비활성화");
        }
        else
        {
            Debug.LogWarning("   ⚠️ battleCanvas가 null입니다!");
        }

        // NetworkIdentity 상태 확인
        if (netIdentity != null)
        {
            Debug.Log($"   - NetworkIdentity: sceneId={netIdentity.sceneId}, netId={netIdentity.netId}");
        }
        else
        {
            Debug.LogError("   ❌ NetworkIdentity가 null입니다!");
        }
    }

    void Update()
    {
        // 첫 1초 동안만 로그 (스팸 방지)
        if (Time.time < 1f && Time.frameCount % 60 == 0)
        {
            Debug.Log($"⏱️ BattleArenaManager.Update() - isServer={isServer}, isClient={isClient}, Frame={Time.frameCount}");
        }

        // 캐릭터 위치 디버그 (2초마다, 10초 동안만)
        if (Time.frameCount % 120 == 0 && Time.time < 10f)
        {
            string side = isServer ? "Server" : "Client";
            if (currentPlayerACharacter != null)
            {
                Debug.Log($"📍 [{side}] Player A 위치: {currentPlayerACharacter.transform.position}");
            }
            if (currentPlayerBCharacter != null)
            {
                Debug.Log($"📍 [{side}] Player B 위치: {currentPlayerBCharacter.transform.position}");
            }
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"🌐 BattleArenaManager.OnStartServer() - netId={netIdentity.netId}, sceneId={netIdentity.sceneId}");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log($"🌐 BattleArenaManager.OnStartClient() - netId={netIdentity.netId}, sceneId={netIdentity.sceneId}");
        Debug.Log($"   - Instance == this? {Instance == this}");
        Debug.Log($"   - isServer={isServer}, isClient={isClient}, isLocalPlayer={isLocalPlayer}");
    }
    
    /// <summary>
    /// 전투 시작 (MatchController에서 호출)
    /// </summary>
    [Server]
    public void StartBattle(Mirror.Examples.MultipleMatch.MatchController controller)
    {
        matchController = controller;

        Debug.Log("🥊 [Server] BattleArenaManager: 전투 시작!");
        Debug.Log($"   - NetworkIdentity: {(netIdentity != null ? $"netId={netIdentity.netId}, sceneId={netIdentity.sceneId}" : "NULL")}");
        Debug.Log($"   - isServer: {isServer}, isClient: {isClient}");

        // Round 1 시작
        currentRound = 1;
        playerAWins = 0;
        playerBWins = 0;

        Debug.Log("   - RpcShowBattleArena() 호출 중...");
        RpcShowBattleArena();

        Debug.Log("   - StartRound(1) 시작...");
        StartCoroutine(StartRound(1));
    }
    
    /// <summary>
    /// 전투 아레나 UI 표시
    /// </summary>
    [ClientRpc]
    void RpcShowBattleArena()
    {
        Debug.Log($"🎬 [Client] RpcShowBattleArena 실행! (isServer={isServer}, isClient={isClient})");

        if (battleCanvas != null)
        {
            battleCanvas.gameObject.SetActive(true);
            Debug.Log("   ✅ BattleCanvas 활성화");
        }
        else
        {
            Debug.LogWarning("   ⚠️ battleCanvas가 null입니다!");
        }

        // 메인 카메라를 전투 위치로 이동
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // 카메라를 아레나 중앙 위에서 내려다보도록 설정
            mainCamera.transform.position = new Vector3(0, 10, -5);
            mainCamera.transform.rotation = Quaternion.Euler(60, 0, 0);
            Debug.Log($"   📷 메인 카메라 이동: {mainCamera.transform.position}");
        }
        else
        {
            Debug.LogWarning("   ⚠️ Main Camera를 찾을 수 없습니다!");
        }

        Debug.Log("🎬 전투 아레나 활성화 완료");
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
        // Player A 캐릭터 스폰 (TeamA = Layer 24)
        Vector3 spawnPosA = playerASpawnPoint != null ? playerASpawnPoint.position : new Vector3(-3, 0, 0);
        currentPlayerACharacter = Instantiate(defaultCharacterPrefab, spawnPosA, Quaternion.Euler(0, 90, 0));

        // 🔑 matchId 설정 (Prefab에 NetworkMatch가 미리 있어야 함)
        SetMatchId(currentPlayerACharacter, "Player A");

        NetworkServer.Spawn(currentPlayerACharacter);

        // 🔴 Player A: Layer 24 (TeamA)로 변경 (서버 + 클라이언트)
        SetLayerRecursively(currentPlayerACharacter, 24);
        var netIdentityA = currentPlayerACharacter.GetComponent<NetworkIdentity>();
        if (netIdentityA != null)
        {
            RpcSetLayerRecursively(netIdentityA.netId, 24);
        }
        Debug.Log($"🔴 Player A 레이어를 24 (TeamA)로 설정");

        // 모든 Renderer 활성화 (중요!)
        ActivateAllRenderers(currentPlayerACharacter, "Player A");

        playerAController = currentPlayerACharacter.GetComponent<BattleCharacterController>();
        if (playerAController != null)
        {
            playerAController.InitializeChampion(playerACard.name.text, 100, 0);
        }
        else
        {
            Debug.LogError("❌ Player A: BattleCharacterController를 찾을 수 없습니다!");
        }

        // AI Character 설정 (MoreMountains)
        var characterA = currentPlayerACharacter.GetComponent("Character");
        if (characterA != null)
        {
            Debug.Log("✅ Player A Character 컴포넌트 발견 (MoreMountains)");
        }

        // Player B 캐릭터 스폰 (TeamB = Layer 25)
        Vector3 spawnPosB = playerBSpawnPoint != null ? playerBSpawnPoint.position : new Vector3(3, 0, 0);
        currentPlayerBCharacter = Instantiate(defaultCharacterPrefab, spawnPosB, Quaternion.Euler(0, -90, 0));

        // 🔑 matchId 설정 (Prefab에 NetworkMatch가 미리 있어야 함)
        SetMatchId(currentPlayerBCharacter, "Player B");

        NetworkServer.Spawn(currentPlayerBCharacter);

        // 🔵 Player B: Layer 25 (TeamB) 유지 (이미 프리팹 기본값)
        SetLayerRecursively(currentPlayerBCharacter, 25);
        var netIdentityB = currentPlayerBCharacter.GetComponent<NetworkIdentity>();
        if (netIdentityB != null)
        {
            RpcSetLayerRecursively(netIdentityB.netId, 25);
        }
        Debug.Log($"🔵 Player B 레이어를 25 (TeamB)로 설정");

        // 모든 Renderer 활성화 (중요!)
        ActivateAllRenderers(currentPlayerBCharacter, "Player B");

        playerBController = currentPlayerBCharacter.GetComponent<BattleCharacterController>();
        if (playerBController != null)
        {
            playerBController.InitializeChampion(playerBCard.name.text, 100, 1);
        }
        else
        {
            Debug.LogError("❌ Player B: BattleCharacterController를 찾을 수 없습니다!");
        }

        // AI Character 설정 (MoreMountains)
        var characterB = currentPlayerBCharacter.GetComponent("Character");
        if (characterB != null)
        {
            Debug.Log("✅ Player B Character 컴포넌트 발견 (MoreMountains)");
        }

        // AIBrain 타겟 레이어 마스크 확인 및 설정
        CheckAIBrainSettings(currentPlayerACharacter, "Player A");
        CheckAIBrainSettings(currentPlayerBCharacter, "Player B");

        // 추가 디버그: GameObject 계층 구조 출력
        Debug.Log($"📦 Player A 계층 구조:");
        PrintHierarchy(currentPlayerACharacter.transform, 0, 2);
        Debug.Log($"📦 Player B 계층 구조:");
        PrintHierarchy(currentPlayerBCharacter.transform, 0, 2);

        Debug.Log($"✅ 캐릭터 스폰: {playerACard.name.text} vs {playerBCard.name.text}");
        Debug.Log($"📍 스폰된 오브젝트: A={currentPlayerACharacter.name} at {currentPlayerACharacter.transform.position} (Layer {currentPlayerACharacter.layer})");
        Debug.Log($"📍 스폰된 오브젝트: B={currentPlayerBCharacter.name} at {currentPlayerBCharacter.transform.position} (Layer {currentPlayerBCharacter.layer})");

        // 디버그: 스폰 위치에 Sphere 그리기 (Gizmo)
        RpcDrawDebugMarkers(spawnPosA, spawnPosB);

        // 🔑 중요: 클라이언트에서 AI 비활성화 (서버만 AI 제어)
        if (netIdentityA != null)
        {
            RpcDisableAIOnClient(netIdentityA.netId);
        }
        if (netIdentityB != null)
        {
            RpcDisableAIOnClient(netIdentityB.netId);
        }
    }

    /// <summary>
    /// GameObject와 모든 자식의 레이어를 재귀적으로 변경 (서버)
    /// </summary>
    [Server]
    void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// GameObject와 모든 자식의 레이어를 재귀적으로 변경 (클라이언트 RPC)
    /// </summary>
    [ClientRpc]
    void RpcSetLayerRecursively(uint netId, int layer)
    {
        // netId로 NetworkIdentity 찾기
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            GameObject obj = identity.gameObject;

            if (obj == null)
            {
                Debug.LogError($"[Client] netId={netId}의 GameObject가 null입니다!");
                return;
            }

            // 재귀적으로 레이어 변경
            SetLayerRecursivelyLocal(obj, layer);

            Debug.Log($"[Client] 레이어 변경: {obj.name} → Layer {layer}");
        }
        else
        {
            Debug.LogError($"[Client] netId={netId}를 가진 NetworkIdentity를 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// 재귀 헬퍼 (로컬, 서버/클라이언트 공용)
    /// </summary>
    void SetLayerRecursivelyLocal(GameObject obj, int layer)
    {
        if (obj == null) return;

        obj.layer = layer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursivelyLocal(child.gameObject, layer);
        }
    }

    /// <summary>
    /// AIBrain 설정 확인 및 수정
    /// </summary>
    [Server]
    void CheckAIBrainSettings(GameObject character, string playerName)
    {
        if (character == null) return;

        // AIBrain 찾기 (자식 오브젝트에 있을 수 있음)
        MonoBehaviour aiBrain = null;

        foreach (var brain in character.GetComponentsInChildren<MonoBehaviour>())
        {
            if (brain.GetType().Name == "AIBrain")
            {
                aiBrain = brain;
                break;
            }
        }

        if (aiBrain != null)
        {
            Debug.Log($"✅ {playerName}: AIBrain 발견");

            // 타겟 레이어 마스크 설정 (상대 팀을 탐지하도록)
            int targetLayer = character.layer == 24 ? 25 : 24; // Player A는 Layer 25 탐지, Player B는 Layer 24 탐지
            SetAIBrainTargetLayer(aiBrain, targetLayer, playerName);

            // Reflection으로 Target 속성 확인
            var targetProp = aiBrain.GetType().GetProperty("Target");
            if (targetProp != null)
            {
                var targetValue = targetProp.GetValue(aiBrain);
                Debug.Log($"   - Target: {(targetValue == null ? "null" : targetValue.ToString())}");
            }

            // BrainActive 확인
            var brainActiveProp = aiBrain.GetType().GetProperty("BrainActive");
            if (brainActiveProp != null)
            {
                var isActive = brainActiveProp.GetValue(aiBrain);
                Debug.Log($"   - BrainActive: {isActive}");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ {playerName}: AIBrain을 찾을 수 없습니다!");
        }
    }

    /// <summary>
    /// AIBrain의 모든 Decision에서 TargetLayerMask 설정
    /// </summary>
    [Server]
    void SetAIBrainTargetLayer(MonoBehaviour aiBrain, int targetLayer, string playerName)
    {
        try
        {
            int changedCount = 0;
            LayerMask newMask = 1 << targetLayer;

            Debug.Log($"   🔍 {playerName}: TargetLayer를 {targetLayer}로 변경 시도 (LayerMask={newMask.value})");

            // 방법 1: AIBrain GameObject의 모든 자식에서 Decision 컴포넌트 찾기
            GameObject aiBrainObj = aiBrain.gameObject;
            var allDecisions = aiBrainObj.GetComponentsInChildren<MonoBehaviour>();

            foreach (var component in allDecisions)
            {
                if (component == null) continue;

                string typeName = component.GetType().Name;

                // Decision으로 끝나는 모든 컴포넌트 확인
                if (typeName.Contains("Decision"))
                {
                    // TargetLayerMask 필드 찾기
                    var targetLayerMaskField = component.GetType().GetField("TargetLayerMask");
                    if (targetLayerMaskField != null)
                    {
                        var currentValue = targetLayerMaskField.GetValue(component);
                        targetLayerMaskField.SetValue(component, newMask);
                        changedCount++;

                        Debug.Log($"      ✅ {typeName}: TargetLayerMask 변경 ({currentValue} → {newMask.value})");
                    }
                }
            }

            // 방법 2: States를 통한 설정 (추가 보장)
            var statesField = aiBrain.GetType().GetField("States");
            if (statesField != null)
            {
                var states = statesField.GetValue(aiBrain);
                if (states != null)
                {
                    var statesList = states as System.Collections.IList;
                    if (statesList != null)
                    {
                        foreach (var state in statesList)
                        {
                            // Transitions 확인
                            var transitionsField = state.GetType().GetField("Transitions");
                            if (transitionsField != null)
                            {
                                var transitions = transitionsField.GetValue(state);
                                var transitionsList = transitions as System.Collections.IList;

                                if (transitionsList != null)
                                {
                                    foreach (var transition in transitionsList)
                                    {
                                        var decisionField = transition.GetType().GetField("Decision");
                                        if (decisionField != null)
                                        {
                                            var decision = decisionField.GetValue(transition);
                                            if (decision != null)
                                            {
                                                var targetLayerMaskField = decision.GetType().GetField("TargetLayerMask");
                                                if (targetLayerMaskField != null)
                                                {
                                                    targetLayerMaskField.SetValue(decision, newMask);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (changedCount > 0)
            {
                Debug.Log($"   🎯 {playerName}: TargetLayerMask를 Layer {targetLayer}로 설정 완료 ({changedCount}개 Decision 수정)");
            }
            else
            {
                Debug.LogWarning($"   ⚠️ {playerName}: TargetLayerMask를 가진 Decision을 찾지 못했습니다!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ {playerName}: AIBrain TargetLayerMask 설정 실패: {e.Message}\n{e.StackTrace}");
        }
    }
    
    /// <summary>
    /// 모든 Renderer 활성화
    /// </summary>
    [Server]
    void ActivateAllRenderers(GameObject character, string playerName)
    {
        if (character == null) return;

        // 모든 하위 GameObject 활성화
        foreach (Transform child in character.GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.SetActive(true);
        }

        // 모든 Renderer 활성화
        var renderers = character.GetComponentsInChildren<Renderer>(true);
        int activatedCount = 0;

        foreach (var renderer in renderers)
        {
            if (!renderer.enabled)
            {
                renderer.enabled = true;
                activatedCount++;
                Debug.Log($"🎨 {playerName}: Renderer 활성화 - {renderer.gameObject.name}");
            }
        }

        Debug.Log($"✅ {playerName}: 총 {activatedCount}개 Renderer 활성화 (전체 {renderers.Length}개)");

        // NetworkIdentity 가져오기
        var netIdentity = character.GetComponent<NetworkIdentity>();
        if (netIdentity != null)
        {
            // RPC로 클라이언트에도 전파 (netId 사용)
            RpcActivateRenderers(netIdentity.netId);
        }
        else
        {
            Debug.LogError($"❌ {playerName}: NetworkIdentity가 없습니다!");
        }
    }

    /// <summary>
    /// 클라이언트에서도 Renderer 활성화
    /// </summary>
    [ClientRpc]
    void RpcActivateRenderers(uint netId)
    {
        // netId로 NetworkIdentity 찾기
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            GameObject character = identity.gameObject;

            if (character == null)
            {
                Debug.LogError($"[Client] netId={netId}의 GameObject가 null입니다!");
                return;
            }

            // 모든 하위 GameObject 활성화
            foreach (Transform child in character.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.SetActive(true);
            }

            // 모든 Renderer 활성화
            var renderers = character.GetComponentsInChildren<Renderer>(true);
            int activatedCount = 0;

            foreach (var renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                    activatedCount++;
                }
            }

            Debug.Log($"[Client] 캐릭터 Renderer 활성화: {character.name} ({activatedCount}개 활성화)");
        }
        else
        {
            Debug.LogError($"[Client] netId={netId}를 가진 NetworkIdentity를 찾을 수 없습니다!");
        }
    }
    
    /// <summary>
    /// 디버그: 스폰 위치 시각화
    /// </summary>
    [ClientRpc]
    void RpcDrawDebugMarkers(Vector3 posA, Vector3 posB)
    {
        // 빨간 Sphere (Player A)
        var markerA = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        markerA.transform.position = posA + Vector3.up * 2;
        markerA.transform.localScale = Vector3.one * 0.5f;
        markerA.GetComponent<Renderer>().material.color = Color.red;
        Destroy(markerA, 5f); // 5초 후 삭제
        
        // 파란 Sphere (Player B)
        var markerB = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        markerB.transform.position = posB + Vector3.up * 2;
        markerB.transform.localScale = Vector3.one * 0.5f;
        markerB.GetComponent<Renderer>().material.color = Color.blue;
        Destroy(markerB, 5f); // 5초 후 삭제
        
        Debug.Log($"🎯 디버그 마커 생성: Red at {posA}, Blue at {posB}");
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

    /// <summary>
    /// GameObject 계층 구조 출력 (디버그용)
    /// </summary>
    void PrintHierarchy(Transform parent, int depth, int maxDepth)
    {
        if (depth > maxDepth) return;

        string indent = new string(' ', depth * 2);
        string layerInfo = $"(Layer {parent.gameObject.layer})";

        // AIBrain이 있는지 확인
        bool hasAIBrain = false;
        foreach (var comp in parent.GetComponents<MonoBehaviour>())
        {
            if (comp != null && comp.GetType().Name == "AIBrain")
            {
                hasAIBrain = true;
                break;
            }
        }

        string brainMarker = hasAIBrain ? " 🧠 AIBrain" : "";
        Debug.Log($"{indent}├─ {parent.name} {layerInfo}{brainMarker}");

        foreach (Transform child in parent)
        {
            PrintHierarchy(child, depth + 1, maxDepth);
        }
    }

    /// <summary>
    /// NetworkMatch matchId 설정 (Prefab에 NetworkMatch가 미리 있어야 함)
    /// </summary>
    [Server]
    void SetMatchId(GameObject obj, string playerName)
    {
        if (obj == null || matchController == null) return;

        // MatchController의 matchId 가져오기
        var controllerMatch = matchController.GetComponent<Mirror.NetworkMatch>();
        if (controllerMatch == null)
        {
            Debug.LogError($"❌ {playerName}: MatchController에 NetworkMatch가 없습니다!");
            return;
        }

        // 캐릭터의 NetworkMatch 가져오기
        var characterMatch = obj.GetComponent<Mirror.NetworkMatch>();
        if (characterMatch == null)
        {
            Debug.LogError($"❌ {playerName}: Character01 Prefab에 NetworkMatch 컴포넌트가 없습니다!");
            Debug.LogError($"   → Unity 에디터에서 Character01.prefab에 NetworkMatch 추가 필요");
            return;
        }

        // matchId 설정 (Prefab에 있으므로 Spawn 전에도 안전)
        characterMatch.matchId = controllerMatch.matchId;
        Debug.Log($"   🔑 {playerName} matchId 설정: {controllerMatch.matchId}");
    }

    /// <summary>
    /// 클라이언트에서 AI 비활성화 (서버만 AI 제어)
    /// </summary>
    [ClientRpc]
    void RpcDisableAIOnClient(uint netId)
    {
        // 서버에서는 AI가 작동해야 하므로 건너뜀
        if (isServer)
        {
            Debug.Log($"[Server] AI 유지 (서버는 AI 제어) - netId={netId}");
            return;
        }

        // netId로 NetworkIdentity 찾기
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            GameObject character = identity.gameObject;

            // 모든 AI 관련 컴포넌트 비활성화
            foreach (var component in character.GetComponentsInChildren<MonoBehaviour>())
            {
                if (component == null) continue;

                string componentName = component.GetType().Name;

                // AIBrain 비활성화
                if (componentName == "AIBrain")
                {
                    component.enabled = false;
                    Debug.Log($"[Client] AIBrain 비활성화: {character.name}");
                }
                // 무기 관련 AI Action 비활성화 (NullReferenceException 방지)
                else if (componentName == "AIActionShoot3D" || componentName == "AIActionAimWeaponAtMovement")
                {
                    component.enabled = false;
                    Debug.Log($"[Client] {componentName} 비활성화: {character.name}");
                }
            }

            // Character 컴포넌트 비활성화 (MoreMountains)
            var characterComponent = character.GetComponent("Character");
            if (characterComponent != null && characterComponent is MonoBehaviour charBehaviour)
            {
                charBehaviour.enabled = false;
                Debug.Log($"[Client] Character 컴포넌트 비활성화: {character.name}");
            }

            Debug.Log($"✅ [Client] AI 비활성화 완료: {character.name} (서버에서만 AI 제어)");
        }
        else
        {
            Debug.LogError($"❌ [Client] netId={netId}를 가진 캐릭터를 찾을 수 없습니다!");
        }
    }
}

