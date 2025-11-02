using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.MultipleMatch
{
    // 플레이어 GUI를 관리하는 클래스
    public class PlayerGUI : MonoBehaviour
    {
        public Text playerName;

        // 클라이언트에서 플레이어 정보를 설정하는 콜백 함수
        [ClientCallback]
        public void SetPlayerInfo(PlayerInfo info)
        {
            // 플레이어 이름 설정 (예: "Player 1")
            playerName.text = $"Player {info.playerIndex}";
            // 플레이어 준비 상태에 따라 이름 색상 변경 (준비 시 녹색, 아닐 시 빨간색)
            playerName.color = info.ready ? Color.green : Color.red;
        }
    }
}