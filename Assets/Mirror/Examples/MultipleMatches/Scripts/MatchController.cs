using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.MultipleMatch
{
    [RequireComponent(typeof(NetworkMatch))]
    public class MatchController : NetworkBehaviour
    {
        // ---------------------------------------------------------------- LFT ----------------------------------------------------------------

        internal readonly Dictionary<int, CharacterElement> DicCharacterElement = new Dictionary<int, CharacterElement>();

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

        [Header("Diagnostics")]
        [ReadOnly, SerializeField] internal CanvasController canvasController;
        [ReadOnly, SerializeField] internal NetworkIdentity player1;
        [ReadOnly, SerializeField] internal NetworkIdentity player2;
        [ReadOnly, SerializeField] internal NetworkIdentity startingPlayer;

        [SyncVar(hook = nameof(UpdateGameUI))]
        [ReadOnly, SerializeField] internal NetworkIdentity currentPlayer;

        void Awake()
        {
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

            matchPlayerData.Add(player1, new MatchPlayerData { playerIndex = CanvasController.playerInfos[player1.connectionToClient].playerIndex });
            matchPlayerData.Add(player2, new MatchPlayerData { playerIndex = CanvasController.playerInfos[player2.connectionToClient].playerIndex });
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
        public void RpcUpdateIndex(int index, NetworkIdentity player)
        {
            DicCharacterElement[index].SetPlayer(player);
        }

        [Command(requiresAuthority = false)]
        public void CmdCharacterClick(int index, NetworkConnectionToClient sender = null)
        {
            // 잘못된 플레이어이거나 셀이 이미 차지된 경우 무시
            if (sender.identity != currentPlayer && DicCharacterElement[index].playerIdentity != null)
                return;

            DicCharacterElement[index].playerIdentity = currentPlayer;

            currentPlayer = currentPlayer == player1 ? player2 : player1;

            RpcUpdateIndex(index, currentPlayer);

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