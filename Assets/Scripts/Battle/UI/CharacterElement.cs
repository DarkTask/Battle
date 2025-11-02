using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterElement : MonoBehaviour
{
    [Header("References")]
    public int championIndex = -1;
    public Image cardBackground;          // Card_Red/Blue/Orange
    public Image championIcon;            // Icon
    public Image characterImage;          // Character_Mask > Character
    public GameObject starOn;             // Star_On
    public GameObject starOff;            // Star_Off
    public TextMeshProUGUI nameText;      // Text_Name
    public GameObject backGlow;           // BackGlow (선택 시 빛나는 효과)
    
    [Header("Stats Text")]
    public TextMeshProUGUI strText;       // STR 텍스트
    public TextMeshProUGUI dexText;       // DEX 텍스트
    public TextMeshProUGUI conText;       // CON 텍스트
    
    [Header("Card Sprites")]
    public Sprite cardOrange;             // 미선택 상태
    public Sprite cardRed;                // Player A 선택
    public Sprite cardBlue;               // Player B 선택
    
    private Button button;
    private bool isSelected = false;
    private ChampionData championData;
    
    void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
            Debug.LogWarning($"{gameObject.name}에 Button 컴포넌트가 없어서 추가했습니다.");
        }
        
        button.onClick.AddListener(OnClicked);
    }
    
    /// <summary>
    /// 챔피언 데이터로 초기화
    /// </summary>
    public void Initialize(ChampionData data, int index)
    {
        if (data == null)
        {
            Debug.LogError($"CharacterElement {index}: 챔피언 데이터가 null입니다!");
            return;
        }
        
        championIndex = index;
        championData = data;
        
        // 이름
        if (nameText != null)
            nameText.text = data.championName;
        
        // 아이콘
        if (championIcon != null && data.icon != null)
            championIcon.sprite = data.icon;
        
        // 전신 이미지
        if (characterImage != null && data.characterImage != null)
            characterImage.sprite = data.characterImage;
        
        // 스탯 표시
        if (strText != null) strText.text = $"STR:{data.strength}";
        if (dexText != null) dexText.text = $"DEX:{data.dexterity}";
        if (conText != null) conText.text = $"CON:{data.constitution}";
        
        // 초기 상태: 미선택
        SetSelected(false, -1);
        
        Debug.Log($"✅ CharacterElement 초기화: {data.championName} (Index: {index})");
    }
    
    /// <summary>
    /// 카드 클릭 이벤트
    /// </summary>
    void OnClicked()
    {
        if (isSelected)
        {
            Debug.Log($"❌ 이미 선택된 챔피언: {nameText.text}");
            return;
        }
        
        if (BattleGameManager.Instance == null)
        {
            Debug.LogError("BattleGameManager가 없습니다!");
            return;
        }
        
        Debug.Log($"🖱️ 챔피언 클릭: {nameText.text} (Index: {championIndex})");
        
        // BattleGameManager에 선택 알림
        BattleGameManager.Instance.CmdSelectChampion(championIndex);
    }
    
    /// <summary>
    /// 선택 상태 업데이트
    /// </summary>
    public void SetSelected(bool selected, int playerIndex)
    {
        isSelected = selected;
        
        // Star 표시/숨김
        if (starOn != null) starOn.SetActive(selected);
        if (starOff != null) starOff.SetActive(!selected);
        
        // 카드 배경 색상 변경
        if (cardBackground != null)
        {
            if (selected)
            {
                // Player에 따라 색상 변경
                if (playerIndex == 0 && cardRed != null)
                {
                    cardBackground.sprite = cardRed;
                    Debug.Log($"🔴 Player A 선택: {nameText.text}");
                }
                else if (playerIndex == 1 && cardBlue != null)
                {
                    cardBackground.sprite = cardBlue;
                    Debug.Log($"🔵 Player B 선택: {nameText.text}");
                }
                
                // Glow 효과 활성화
                if (backGlow != null)
                    backGlow.SetActive(true);
            }
            else
            {
                // 미선택 상태
                if (cardOrange != null)
                    cardBackground.sprite = cardOrange;
                
                // Glow 효과 비활성화
                if (backGlow != null)
                    backGlow.SetActive(false);
            }
        }
        
        // 버튼 비활성화 (선택된 카드는 다시 클릭 불가)
        if (button != null)
            button.interactable = !selected;
    }
    
    /// <summary>
    /// 에디터에서 참조 자동 연결 (선택)
    /// </summary>
#if UNITY_EDITOR
    [ContextMenu("Auto Connect References")]
    void AutoConnectReferences()
    {
        // Card Background
        if (cardBackground == null)
        {
            Transform card = transform.Find("Card_Orange");
            if (card == null) card = transform.Find("Card_Red");
            if (card == null) card = transform.Find("Card_Blue");
            if (card != null) cardBackground = card.GetComponent<Image>();
        }
        
        // Icon
        if (championIcon == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null) championIcon = icon.GetComponent<Image>();
        }
        
        // Character Image
        if (characterImage == null)
        {
            Transform mask = transform.Find("Character_Mask");
            if (mask != null)
            {
                Transform character = mask.Find("Character");
                if (character != null) characterImage = character.GetComponent<Image>();
            }
        }
        
        // Stars
        if (starOn == null)
        {
            Transform star = transform.Find("Star_On");
            if (star != null) starOn = star.gameObject;
        }
        
        if (starOff == null)
        {
            Transform star = transform.Find("Star_Off");
            if (star != null) starOff = star.gameObject;
        }
        
        // Name Text
        if (nameText == null)
        {
            Transform text = transform.Find("Text_Name");
            if (text != null) nameText = text.GetComponent<TextMeshProUGUI>();
        }
        
        // Back Glow
        if (backGlow == null)
        {
            Transform glow = transform.Find("BackGlow");
            if (glow != null) backGlow = glow.gameObject;
        }
        
        // Stats
        Transform statsGroup = transform.Find("Stats_Group");
        if (statsGroup != null)
        {
            int childCount = statsGroup.childCount;
            if (childCount >= 3)
            {
                if (strText == null) strText = statsGroup.GetChild(0).GetComponent<TextMeshProUGUI>();
                if (dexText == null) dexText = statsGroup.GetChild(1).GetComponent<TextMeshProUGUI>();
                if (conText == null) conText = statsGroup.GetChild(2).GetComponent<TextMeshProUGUI>();
            }
        }
        
        Debug.Log("✅ 참조 자동 연결 시도 완료");
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
#endif
}

