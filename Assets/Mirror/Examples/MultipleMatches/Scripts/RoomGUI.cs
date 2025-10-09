using UnityEngine;
using UnityEngine.UI;

namespace Mirror.Examples.MultipleMatch
{
    // 룸 GUI를 관리하는 클래스
    public class RoomGUI : MonoBehaviour
    {
        public GameObject playerList; // 플레이어 목록을 표시할 게임 오브젝트
        public GameObject playerPrefab; // 플레이어 UI 프리팹
        public GameObject cancelButton; // 취소 버튼
        public GameObject leaveButton; // 나가기 버튼
        public Button startButton; // 시작 버튼
        public bool owner; // 방장 여부

        // 클라이언트에서 룸의 플레이어 목록을 새로고침하는 콜백 함수
        [ClientCallback]
        public void RefreshRoomPlayers(PlayerInfo[] playerInfos)
        {
            // 기존 플레이어 목록 삭제
            foreach (Transform child in playerList.transform)
                Destroy(child.gameObject);

            startButton.interactable = false; // 시작 버튼 비활성화
            bool everyoneReady = true; // 모든 플레이어가 준비되었는지 여부

            // 모든 플레이어 정보에 대해 반복
            foreach (PlayerInfo playerInfo in playerInfos)
            {
                // 플레이어 UI 프리팹 인스턴스화
                GameObject newPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                newPlayer.transform.SetParent(playerList.transform, false);
                newPlayer.GetComponent<PlayerGUI>().SetPlayerInfo(playerInfo);

                // 한 명이라도 준비되지 않았으면 everyoneReady를 false로 설정
                if (!playerInfo.ready)
                    everyoneReady = false;
            }

            // 모든 플레이어가 준비되었고, 방장이며, 플레이어가 1명 초과일 때 시작 버튼 활성화
            startButton.interactable = everyoneReady && owner && (playerInfos.Length > 1);
        }

        // 클라이언트에서 방장 여부를 설정하는 콜백 함수
        [ClientCallback]
        public void SetOwner(bool owner)
        {
            this.owner = owner;
            cancelButton.SetActive(owner); // 방장이면 취소 버튼 활성화
            leaveButton.SetActive(!owner); // 방장이 아니면 나가기 버튼 활성화
        }
    }
}
