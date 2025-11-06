using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.MultipleMatch
{
    [RequireComponent(typeof(NetworkMatch))]
    public class MatchController : NetworkBehaviour
    {
        public static MatchController Instance;

        // ---------------------------------------------------------------- LFT ----------------------------------------------------------------

        internal readonly Dictionary<int, CharacterElement> DicCharacterElement = new Dictionary<int, CharacterElement>();

        /// <summary>
        /// Key : (player 0~1, index)
        /// </summary>
        internal readonly Dictionary<int, List<CardElement>> DicCardElement = new Dictionary<int, List<CardElement>>();

        // 전투 순서 제출 상태
        private bool player1OrderSubmitted = false;
        private bool player2OrderSubmitted = false;
        
        // 전투 순서 저장 (player 0, 1)
        private int[] player1BattleOrder = new int[3] { -1, -1, -1 };
        private int[] player2BattleOrder = new int[3] { -1, -1, -1 };
        
        // Public 접근자
        public int[] GetPlayer1BattleOrder() => player1BattleOrder;
        public int[] GetPlayer2BattleOrder() => player2BattleOrder;

        // ---------------------------------------------------------------- LFT ----------------------------------------------------------------

        internal readonly SyncDictionary<NetworkIdentity, MatchPlayerData> matchPlayerData = new SyncDictionary<NetworkIdentity, MatchPlayerData>();
        internal readonly Dictionary<CellValue, CellGUI> MatchCells = new Dictionary<CellValue, CellGUI>();

        CellValue boardScore = CellValue.None;
        bool playAgain = false;

        [Header("GUI References")]
        public CanvasGroup canvasGroup;
        public Text gameText;
        public Button exitButton;
        public Button playAgainButton;
        public Text winCountLocal;
        public Text winCountOpponent;
        public GameObject Panels;

        [Header("Diagnostics")]
        [ReadOnly, SerializeField] internal CanvasController canvasController;
        [ReadOnly, SerializeField] internal NetworkIdentity player1;
        [ReadOnly, SerializeField] internal NetworkIdentity player2;
        [ReadOnly, SerializeField] internal NetworkIdentity startingPlayer;

        [SyncVar(hook = nameof(UpdateGameUI))]
        [ReadOnly, SerializeField] internal NetworkIdentity currentPlayer;

        void Awake()
        {
            if (Instance == null)
                Instance = this;

#if UNITY_2022_2_OR_NEWER
            canvasController = GameObject.FindAnyObjectByType<CanvasController>();
#else
            // Unity 2023.1에서 사용되지 않음
            canvasController = GameObject.FindObjectOfType<CanvasController>();
#endif
        }

        public override void OnStartServer()
        {
            StartCoroutine(AddPlayersToMatchController());
        }

        // SyncDictionary가 업데이트 콜백을 제대로 실행하려면
        // 이미 스폰된 MatchController에 플레이어를 추가하기 전에 프레임을 기다려야 합니다.
        IEnumerator AddPlayersToMatchController()
        {
            yield return null;

            // playerIndex를 명확하게 0과 1로 설정 (전투 순서 시스템을 위해)
            matchPlayerData.Add(player1, new MatchPlayerData { playerIndex = 0 });
            matchPlayerData.Add(player2, new MatchPlayerData { playerIndex = 1 });
            
            Debug.Log($"✅ MatchController: player1 설정 (playerIndex=0), player2 설정 (playerIndex=1)");
        }

        public override void OnStartClient()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            exitButton.gameObject.SetActive(false);
            playAgainButton.gameObject.SetActive(false);

            // SyncDictionary 변경에 대한 핸들러 할당
            matchPlayerData.OnChange = UpdateWins;
        }

        [ClientCallback]
        public void UpdateGameUI(NetworkIdentity _, NetworkIdentity newPlayerTurn)
        {
            if (!newPlayerTurn) return;

            if (newPlayerTurn.gameObject.GetComponent<NetworkIdentity>().isLocalPlayer)
            {
                gameText.text = "Your Turn";
                gameText.color = Color.blue;
            }
            else
            {
                gameText.text = "Their Turn";
                gameText.color = Color.red;
            }
        }

        [ClientCallback]
        public void UpdateWins(SyncDictionary<NetworkIdentity, MatchPlayerData>.Operation op, NetworkIdentity key, MatchPlayerData matchPlayerData)
        {
            if (key.gameObject.GetComponent<NetworkIdentity>().isLocalPlayer)
                winCountLocal.text = $"Player {matchPlayerData.playerIndex}\n{matchPlayerData.wins}";
            else
                winCountOpponent.text = $"Player {matchPlayerData.playerIndex}\n{matchPlayerData.wins}";
        }

        [Command(requiresAuthority = false)]
        public void CmdMakePlay(CellValue cellValue, NetworkConnectionToClient sender = null)
        {
            // 잘못된 플레이어이거나 셀이 이미 차지된 경우 무시
            if (sender.identity != currentPlayer || MatchCells[cellValue].playerIdentity != null)
                return;

            MatchCells[cellValue].playerIdentity = currentPlayer;
            RpcUpdateCell(cellValue, currentPlayer);

            MatchPlayerData mpd = matchPlayerData[currentPlayer];
            mpd.currentScore = mpd.currentScore | cellValue;
            matchPlayerData[currentPlayer] = mpd;

            boardScore |= cellValue;

            if (CheckWinner(mpd.currentScore))
            {
                mpd.wins += 1;
                matchPlayerData[currentPlayer] = mpd;
                RpcShowWinner(currentPlayer);
                currentPlayer = null;
            }
            else if (boardScore == CellValue.Full)
            {
                RpcShowWinner(null);
                currentPlayer = null;
            }
            else
            {
                // 클라이언트가 누구의 턴인지 알 수 있도록 currentPlayer SyncVar 설정
                currentPlayer = currentPlayer == player1 ? player2 : player1;
            }

        }

        [ServerCallback]
        bool CheckWinner(CellValue currentScore)
        {
            if ((currentScore & CellValue.TopRow) == CellValue.TopRow)
                return true;
            if ((currentScore & CellValue.MidRow) == CellValue.MidRow)
                return true;
            if ((currentScore & CellValue.BotRow) == CellValue.BotRow)
                return true;
            if ((currentScore & CellValue.LeftCol) == CellValue.LeftCol)
                return true;
            if ((currentScore & CellValue.MidCol) == CellValue.MidCol)
                return true;
            if ((currentScore & CellValue.RightCol) == CellValue.RightCol)
                return true;
            if ((currentScore & CellValue.Diag1) == CellValue.Diag1)
                return true;
            if ((currentScore & CellValue.Diag2) == CellValue.Diag2)
                return true;

            return false;
        }

        [ClientRpc]
        public void RpcUpdateCell(CellValue cellValue, NetworkIdentity player)
        {
            MatchCells[cellValue].SetPlayer(player);
        }

        [ClientRpc]
        public void RpcShowWinner(NetworkIdentity winner)
        {
            foreach (CellGUI cellGUI in MatchCells.Values)
                cellGUI.GetComponent<Button>().interactable = false;

            if (winner == null)
            {
                gameText.text = "Draw!";
                gameText.color = Color.yellow;
            }
            else if (winner.gameObject.GetComponent<NetworkIdentity>().isLocalPlayer)
            {
                gameText.text = "Winner!";
                gameText.color = Color.blue;
            }
            else
            {
                gameText.text = "Loser!";
                gameText.color = Color.red;
            }

            exitButton.gameObject.SetActive(true);
            playAgainButton.gameObject.SetActive(true);
        }

        // 인스펙터에서 ReplayButton::OnClick에 할당됨
        [ClientCallback]
        public void RequestPlayAgain()
        {
            playAgainButton.gameObject.SetActive(false);
            CmdPlayAgain();
        }

        [Command(requiresAuthority = false)]
        public void CmdPlayAgain(NetworkConnectionToClient sender = null)
        {
            if (!playAgain)
                playAgain = true;
            else
            {
                playAgain = false;
                RestartGame();
            }
        }

        [ServerCallback]
        public void RestartGame()
        {
            boardScore = CellValue.None;

            NetworkIdentity[] keys = new NetworkIdentity[matchPlayerData.Keys.Count];
            matchPlayerData.Keys.CopyTo(keys, 0);

            foreach (NetworkIdentity identity in keys)
            {
                MatchPlayerData mpd = matchPlayerData[identity];
                mpd.currentScore = CellValue.None;
                matchPlayerData[identity] = mpd;
            }

            RpcRestartGame();

            startingPlayer = startingPlayer == player1 ? player2 : player1;
            currentPlayer = startingPlayer;
        }

        [ClientRpc]
        public void RpcRestartGame()
        {
            foreach (CellGUI cellGUI in MatchCells.Values)
                cellGUI.SetPlayer(null);

            exitButton.gameObject.SetActive(false);
            playAgainButton.gameObject.SetActive(false);
        }

        // 인스펙터에서 BackButton::OnClick에 할당됨
        [Client]
        public void RequestExitGame()
        {
            exitButton.gameObject.SetActive(false);
            playAgainButton.gameObject.SetActive(false);
            CmdRequestExitGame();
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestExitGame(NetworkConnectionToClient sender = null)
        {
            StartCoroutine(ServerEndMatch(sender, false));
        }

        [ServerCallback]
        public void OnPlayerDisconnect(NetworkConnectionToClient conn)
        {
            // 연결이 끊긴 클라이언트가 이 매치의 플레이어인지 확인
            if (player1 == conn.identity || player2 == conn.identity)
                StartCoroutine(ServerEndMatch(conn, true));
        }

        [ServerCallback]
        public IEnumerator ServerEndMatch(NetworkConnectionToClient conn, bool disconnected)
        {
            RpcExitGame();

            canvasController.OnPlayerDisconnect -= OnPlayerDisconnect;

            // ClientRpc가 객체 파괴보다 먼저 나가도록 기다립니다.
            yield return new WaitForSeconds(0.1f);

            // Mirror는 연결이 끊긴 클라이언트를 정리하므로 나머지 클라이언트만 정리하면 됩니다.
            // 두 플레이어 모두 로비로 돌아가는 경우 두 연결 플레이어를 모두 제거해야 합니다.

            if (!disconnected)
            {
                NetworkServer.RemovePlayerForConnection(player1.connectionToClient, RemovePlayerOptions.Destroy);
                CanvasController.waitingConnections.Add(player1.connectionToClient);

                NetworkServer.RemovePlayerForConnection(player2.connectionToClient, RemovePlayerOptions.Destroy);
                CanvasController.waitingConnections.Add(player2.connectionToClient);
            }
            else if (conn == player1.connectionToClient)
            {
                // player1 연결 끊김 - player2를 로비로 돌려보냄
                NetworkServer.RemovePlayerForConnection(player2.connectionToClient, RemovePlayerOptions.Destroy);
                CanvasController.waitingConnections.Add(player2.connectionToClient);
            }
            else if (conn == player2.connectionToClient)
            {
                // player2 연결 끊김 - player1을 로비로 돌려보냄
                NetworkServer.RemovePlayerForConnection(player1.connectionToClient, RemovePlayerOptions.Destroy);
                CanvasController.waitingConnections.Add(player1.connectionToClient);
            }

            // 제거가 완료될 때까지 프레임 건너뛰기
            yield return null;

            // 최신 매치 목록 보내기
            canvasController.SendMatchList();

            NetworkServer.Destroy(gameObject);
        }

        [ClientRpc]
        public void RpcExitGame()
        {
            canvasController.OnMatchEnded();
        }

        //---------------------------------------------------------------- LFT ----------------------------------------------------------------

        [ClientRpc]
        public void RpcUpdateIndex(int index, NetworkIdentity player, int playerIndex)
        {
            DicCharacterElement[index].SetPlayer(player, playerIndex);

            var championName = DicCharacterElement[index].name.text.ToString();

            var cardIndex = DicCardElement[playerIndex].Where(x => x.isSetup == true).Count();

            DicCardElement[playerIndex][cardIndex].SetCard(player, championName);
        }

        [ClientRpc]
        public void RpcDisablePanel()
        {
            Panels.SetActive(false);
            
            // 전투 순서 지정 UI 표시
            StartBattleOrderSetup();
        }

        /// <summary>
        /// 전투 순서 지정 시작 (클라이언트)
        /// </summary>
        [ClientCallback]
        void StartBattleOrderSetup()
        {
            // 로컬 플레이어의 인덱스 결정
            int localPlayerIndex = -1;
            
            // matchPlayerData를 사용하여 로컬 플레이어 찾기
            foreach (var kvp in matchPlayerData)
            {
                NetworkIdentity playerIdentity = kvp.Key;
                MatchPlayerData playerData = kvp.Value;
                
                if (playerIdentity != null && playerIdentity.isLocalPlayer)
                {
                    localPlayerIndex = playerData.playerIndex;
                    Debug.Log($"✅ matchPlayerData에서 로컬 플레이어 찾음: playerIndex={localPlayerIndex}");
                    break;
                }
            }
            
            // 대체 방법 1: player1, player2 직접 체크
            if (localPlayerIndex == -1)
            {
                if (player1 != null && player1.isLocalPlayer)
                {
                    localPlayerIndex = matchPlayerData.ContainsKey(player1) ? matchPlayerData[player1].playerIndex : 0;
                    Debug.Log($"✅ player1에서 로컬 플레이어 찾음: playerIndex={localPlayerIndex}");
                }
                else if (player2 != null && player2.isLocalPlayer)
                {
                    localPlayerIndex = matchPlayerData.ContainsKey(player2) ? matchPlayerData[player2].playerIndex : 1;
                    Debug.Log($"✅ player2에서 로컬 플레이어 찾음: playerIndex={localPlayerIndex}");
                }
            }
            
            // 대체 방법 2: NetworkClient.connection을 통한 확인
            if (localPlayerIndex == -1 && NetworkClient.connection != null && NetworkClient.connection.identity != null)
            {
                NetworkIdentity localIdentity = NetworkClient.connection.identity;
                if (localIdentity == player1 && matchPlayerData.ContainsKey(player1))
                {
                    localPlayerIndex = matchPlayerData[player1].playerIndex;
                    Debug.Log($"✅ NetworkClient.connection에서 player1 확인: playerIndex={localPlayerIndex}");
                }
                else if (localIdentity == player2 && matchPlayerData.ContainsKey(player2))
                {
                    localPlayerIndex = matchPlayerData[player2].playerIndex;
                    Debug.Log($"✅ NetworkClient.connection에서 player2 확인: playerIndex={localPlayerIndex}");
                }
            }
            
            if (localPlayerIndex == -1)
            {
                Debug.LogWarning($"⚠️ 로컬 플레이어를 찾을 수 없습니다! " +
                    $"player1={player1?.gameObject.name} (isLocal={player1?.isLocalPlayer}), " +
                    $"player2={player2?.gameObject.name} (isLocal={player2?.isLocalPlayer}), " +
                    $"matchPlayerData.Count={matchPlayerData.Count}");
                
                // 타이밍 이슈일 수 있으므로 다음 프레임에 재시도
                StartCoroutine(RetryStartBattleOrderSetup());
                return;
            }
            
            Debug.Log($"🎯 전투 순서 지정 시작 (Player {(localPlayerIndex == 0 ? "A" : "B")}, Index={localPlayerIndex})");
            
            // BattleOrderUI 표시
            if (BattleOrderUI.Instance != null)
            {
                BattleOrderUI.Instance.ShowOrderSetupUI(localPlayerIndex);
            }
            else
            {
                Debug.LogError("BattleOrderUI.Instance를 찾을 수 없습니다!");
            }
        }
        
        /// <summary>
        /// 타이밍 이슈 대응: 재시도
        /// </summary>
        IEnumerator RetryStartBattleOrderSetup()
        {
            yield return new WaitForSeconds(0.2f);
            Debug.Log("🔄 전투 순서 지정 재시도...");
            StartBattleOrderSetup();
        }

        /// <summary>
        /// 전투 순서 제출 (Command)
        /// </summary>
        [Command(requiresAuthority = false)]
        public void CmdSubmitBattleOrder(int slot1, int slot2, int slot3, NetworkConnectionToClient sender = null)
        {
            if (sender == null || sender.identity == null)
            {
                Debug.LogError("올바르지 않은 sender입니다!");
                return;
            }

            // 플레이어 인덱스 결정
            int playerIndex = -1;
            if (sender.identity == player1)
                playerIndex = 0;
            else if (sender.identity == player2)
                playerIndex = 1;
            
            if (playerIndex == -1)
            {
                Debug.LogError("플레이어를 찾을 수 없습니다!");
                return;
            }

            // 유효성 검사
            if (slot1 < 0 || slot1 > 2 || slot2 < 0 || slot2 > 2 || slot3 < 0 || slot3 > 2)
            {
                Debug.LogError($"유효하지 않은 슬롯 값: [{slot1}, {slot2}, {slot3}]");
                return;
            }

            if (slot1 == slot2 || slot2 == slot3 || slot1 == slot3)
            {
                Debug.LogError($"중복된 슬롯 값: [{slot1}, {slot2}, {slot3}]");
                return;
            }

            // MatchController에 순서 저장
            if (playerIndex == 0)
            {
                player1BattleOrder[0] = slot1;
                player1BattleOrder[1] = slot2;
                player1BattleOrder[2] = slot3;
            }
            else
            {
                player2BattleOrder[0] = slot1;
                player2BattleOrder[1] = slot2;
                player2BattleOrder[2] = slot3;
            }
            
            Debug.Log($"✅ Player {(playerIndex == 0 ? "A" : "B")} 전투 순서 제출: [{slot1}, {slot2}, {slot3}]");

            // 제출 상태 기록
            if (playerIndex == 0)
                player1OrderSubmitted = true;
            else
                player2OrderSubmitted = true;

            // 해당 클라이언트에게만 확인 메시지 전송
            TargetBattleOrderConfirmed(sender, slot1, slot2, slot3);

            // 양쪽 모두 제출했는지 확인
            if (player1OrderSubmitted && player2OrderSubmitted)
            {
                Debug.Log("✅ 양쪽 플레이어 모두 전투 순서 제출 완료!");

                // 서버에서 전투 시작 (캐릭터 스폰)
                ServerStartBattle();

                // 클라이언트들에게 전투 시작 알림 (UI 변경)
                RpcStartBattle();
            }
        }

        /// <summary>
        /// 특정 클라이언트에게만 순서 확인 메시지 전송 (비공개)
        /// </summary>
        [TargetRpc]
        void TargetBattleOrderConfirmed(NetworkConnection target, int slot1, int slot2, int slot3)
        {
            Debug.Log($"📨 전투 순서 제출 완료: [{slot1}, {slot2}, {slot3}]");
            
            // UI 업데이트 (대기 상태)
            if (BattleOrderUI.Instance != null)
            {
                // UI는 이미 "상대방 기다리는 중" 상태를 표시하고 있음
            }
        }

        /// <summary>
        /// 서버에서 실제 전투 시작 (캐릭터 스폰)
        /// </summary>
        [Server]
        void ServerStartBattle()
        {
            Debug.Log("🎮 [Server] ServerStartBattle() 시작!");
            Debug.Log($"   - Player A 전투 순서: [{player1BattleOrder[0]}, {player1BattleOrder[1]}, {player1BattleOrder[2]}]");
            Debug.Log($"   - Player B 전투 순서: [{player2BattleOrder[0]}, {player2BattleOrder[1]}, {player2BattleOrder[2]}]");

            // BattleArenaManager 찾기
            Debug.Log($"   - BattleArenaManager.Instance: {(BattleArenaManager.Instance != null ? "존재" : "NULL")}");

            if (BattleArenaManager.Instance != null)
            {
                Debug.Log("   ✅ BattleArenaManager.Instance.StartBattle() 호출 중...");
                BattleArenaManager.Instance.StartBattle(this);
            }
            else
            {
                Debug.LogError("❌ BattleArenaManager.Instance를 찾을 수 없습니다!");
                Debug.LogError("   Scene에서 BattleArenaManager GameObject를 찾아봅니다...");

                // 직접 찾기 시도
                var manager = FindObjectOfType<BattleArenaManager>();
                if (manager != null)
                {
                    Debug.LogWarning($"⚠️ FindObjectOfType으로 발견! (Instance가 설정되지 않았음)");
                    BattleArenaManager.Instance = manager;
                    manager.StartBattle(this);
                }
                else
                {
                    Debug.LogError("❌ Scene에 BattleArenaManager GameObject가 없습니다! Hierarchy를 확인하세요.");
                }
            }
        }

        /// <summary>
        /// 클라이언트에게 전투 시작 알림 (UI 변경)
        /// </summary>
        [ClientRpc]
        void RpcStartBattle()
        {
            Debug.Log("⚔️ 전투 시작! (Client)");

            // 전투 순서 UI 숨기기 (모든 클라이언트)
            if (BattleOrderUI.Instance != null)
            {
                BattleOrderUI.Instance.HideOrderSetupUI();
            }
        }

        int cnt = 0;

        [Command(requiresAuthority = false)]
        public void CmdCharacterClick(int index, NetworkConnectionToClient sender = null)
        {
            // 잘못된 플레이어이거나 셀이 이미 차지된 경우 무시
            if (sender.identity != currentPlayer && DicCharacterElement[index].playerIdentity != null)
                return;

            DicCharacterElement[index].playerIdentity = currentPlayer;

            int playerIndex = 0;

            if (currentPlayer == player1)
                playerIndex = 0;
            else
                playerIndex = 1;

            currentPlayer = currentPlayer == player1 ? player2 : player1;

            RpcUpdateIndex(index, currentPlayer, playerIndex);

            cnt++;

            if (cnt == 6)
            {
                RpcDisablePanel();
            }

            return;


            MatchPlayerData mpd = matchPlayerData[currentPlayer];
            //mpd.currentScore = mpd.currentScore | cellValue;
            matchPlayerData[currentPlayer] = mpd;

            //boardScore |= cellValue;

            if (CheckWinner(mpd.currentScore))
            {
                mpd.wins += 1;
                matchPlayerData[currentPlayer] = mpd;
                RpcShowWinner(currentPlayer);
                currentPlayer = null;
            }
            else if (boardScore == CellValue.Full)
            {
                RpcShowWinner(null);
                currentPlayer = null;
            }
            else
            {
                // 클라이언트가 누구의 턴인지 알 수 있도록 currentPlayer SyncVar 설정
                currentPlayer = currentPlayer == player1 ? player2 : player1;
            }

            
        }
    }
}