using Mirror;
using Mirror.Examples.MultipleMatch;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

public class CharacterElement : MonoBehaviour
{
    public MatchController matchController;

    public Button button;

    public Image image;

    public TextMeshProUGUI name;

    public int index;

    [Header("Diagnostics - 진단 정보")]
    [ReadOnly, SerializeField] internal NetworkIdentity playerIdentity;

    /// <summary>
    /// Unity Start 메서드 - 초기화 작업 수행
    /// - MatchController의 딕셔너리에 자신을 등록
    /// - 버튼 클릭 이벤트 리스너 등록
    /// - 스프라이트 이름에서 캐릭터 이름 추출
    /// </summary>
    void Start()
    {
        matchController.DicCharacterElement.Add(index, this);

        button = GetComponent<Button>();

        button.onClick.AddListener(OnClick);

        string result = image.sprite.name.Substring(0, image.sprite.name.IndexOf('_'));

        name.text = result;
    }

    // Update is called once per frame
    void Update()
    {
        // 현재 이 메서드는 사용되지 않습니다.
        // 필요시 캐릭터 상태 업데이트 로직을 여기에 추가할 수 있습니다.
    }

    [ClientCallback]
    private void OnClick()
    {
        if (matchController.currentPlayer.isLocalPlayer)
            matchController.CmdCharacterClick(index);

        Debug.Log(gameObject.name + " Character Element Clicked");
    }

    [ClientCallback]
    public void SetPlayer(NetworkIdentity playerIdentity, int playerIndex)
    {
        if (playerIdentity != null)
        {
            this.playerIdentity = playerIdentity;

            if (playerIndex == 0)
            {
                image.color = Color.red;
            }
            else
            {
                image.color = Color.blue;
            }

            //image.color = this.playerIdentity.isLocalPlayer ? Color.blue : Color.red;
            button.interactable = false;
        }
        else
        {
            this.playerIdentity = null;
            image.color = Color.white;
            button.interactable = true;
        }
    }


    /// <summary>
    /// CharacterSelectUI에서 호출: ChampionData로 초기화
    /// </summary>
    public void InitializeWithChampionData(ChampionData championData, int championIndex)
    {
        if (championData == null)
        {
            Debug.LogWarning($"CharacterElement[{championIndex}]: ChampionData가 null입니다!");
            return;
        }

        index = championIndex;

        // 아이콘 설정
        if (image != null && championData.icon != null)
        {
            image.sprite = championData.icon;
            image.color = Color.white;
        }

        // 이름 설정
        if (name != null)
        {
            name.text = championData.championName;
        }

        // 버튼 활성화
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.interactable = true;
        }

        Debug.Log($"✅ CharacterElement[{championIndex}] 초기화: {championData.championName}");
    }

    /// <summary>
    /// CharacterSelectUI에서 호출: 선택 상태 설정
    /// </summary>
    public void SetSelectedState(bool selected, int playerIndex)
    {
        if (selected)
        {
            // 선택됨 - 플레이어 색상으로 변경
            if (image != null)
            {
                image.color = playerIndex == 0 ? Color.red : Color.blue;
            }

            // 버튼 비활성화
            if (button != null)
            {
                button.interactable = false;
            }

            string playerName = playerIndex == 0 ? "Player A" : "Player B";
            Debug.Log($"🎯 CharacterElement[{index}] 선택됨 ({playerName})");
        }
        else
        {
            // 선택 해제 - 원래 색상으로 복원
            if (image != null)
            {
                image.color = Color.white;
            }

            // 버튼 활성화
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }
}
