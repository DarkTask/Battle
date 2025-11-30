using UnityEngine;
using UnityEngine.UI;

namespace QuantumUser
{
    /// <summary>
    /// 캐릭터 머리 위에 표시되는 HP 바
    /// World Space Canvas를 사용하여 캐릭터를 따라다님
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [Header("UI References")]
        public Image fillImage;
        public Image backgroundImage;
        public RectTransform fillRect;  // Fill의 RectTransform (스케일 조절용)

        [Header("Settings")]
        public Vector3 offset = new Vector3(0, 0.8f, 0);  // 캐릭터 위 오프셋
        public Color fullHealthColor = Color.green;
        public Color lowHealthColor = Color.red;
        public float lowHealthThreshold = 0.3f;

        [Header("Team Colors")]
        public Color teamAColor = new Color(1f, 0.3f, 0.3f);  // 빨간색
        public Color teamBColor = new Color(0.3f, 0.5f, 1f);  // 파란색

        private float _maxHealth = 100f;
        private float _currentHealth = 100f;
        private int _teamId = -1;

        /// <summary>
        /// HP 바 초기화
        /// </summary>
        public void Initialize(Transform target, float maxHealth, int teamId)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            _teamId = teamId;

            // 팀 색상 적용
            if (backgroundImage != null)
            {
                backgroundImage.color = (teamId == 0) ? teamAColor : teamBColor;
            }

            UpdateHealthBar();
        }

        /// <summary>
        /// HP 업데이트
        /// </summary>
        public void SetHealth(float currentHealth)
        {
            float oldHealth = _currentHealth;
            _currentHealth = Mathf.Clamp(currentHealth, 0, _maxHealth);

            // 체력 변화가 있을 때만 로그 출력
            if (Mathf.Abs(oldHealth - _currentHealth) > 0.1f)
            {
                float percent = _currentHealth / _maxHealth;
                Debug.Log($"💔 HP 변화: {oldHealth:F1} → {_currentHealth:F1} / {_maxHealth:F1} ({(percent * 100):F0}%) fillAmount={percent:F2}");
            }

            UpdateHealthBar();
        }

        /// <summary>
        /// 디버그용: 현재 HP 상태 강제 로그
        /// </summary>
        public void DebugLogHP(float incomingHP)
        {
            Debug.Log($"📊 HP Debug: incoming={incomingHP:F1}, current={_currentHealth:F1}, max={_maxHealth:F1}, fillAmount={fillImage?.fillAmount:F2}");
        }

        /// <summary>
        /// HP 바 시각적 업데이트
        /// </summary>
        void UpdateHealthBar()
        {
            if (_maxHealth <= 0)
            {
                Debug.LogWarning($"⚠️ Invalid maxHealth: {_maxHealth}");
                return;
            }

            float healthPercent = _currentHealth / _maxHealth;

            // RectTransform 스케일로 HP 바 조절 (anchorMax.x 변경)
            if (fillRect != null)
            {
                // anchorMax.x를 healthPercent로 설정 (0~1 범위)
                fillRect.anchorMax = new Vector2(0.02f + (0.96f * healthPercent), fillRect.anchorMax.y);
            }

            // 체력에 따른 색상 변화
            if (fillImage != null)
            {
                if (healthPercent <= lowHealthThreshold)
                {
                    fillImage.color = lowHealthColor;
                }
                else
                {
                    fillImage.color = fullHealthColor;
                }
            }
        }

        void LateUpdate()
        {
            // 자식으로 있으므로 localPosition만 설정 (offset 적용)
            transform.localPosition = offset;
        }

        /// <summary>
        /// HP 바 프리팹 동적 생성 (코드로 생성)
        /// </summary>
        public static HealthBarUI CreateHealthBar(Transform parent)
        {
            // Canvas 생성
            GameObject canvasObj = new GameObject("HealthBarCanvas");
            canvasObj.transform.SetParent(parent, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 1000;  // 높은 값으로 변경

            // Sorting Layer 설정 (Default 또는 UI)
            canvas.sortingLayerName = "Default";

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // RectTransform 설정 - 2D 게임에 맞게 스케일 조정
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(100f, 15f);  // 픽셀 단위로 크게
            canvasRect.localScale = Vector3.one * 0.005f;   // 더 작은 스케일로 조정 (0.5 유닛 너비)

            // Transform.localPosition 직접 설정 (RectTransform이 아닌 Transform으로)
            canvasObj.transform.localPosition = new Vector3(0, -0.15f, 0);  // 캐릭터 발밑

            // Background 생성
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(canvasObj.transform, false);

            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;

            // Fill 생성
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(canvasObj.transform, false);

            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = Color.green;
            fillImage.type = Image.Type.Simple;  // Simple 타입 사용 (Filled는 Sprite 필요)

            RectTransform fillRectTransform = fillObj.GetComponent<RectTransform>();
            fillRectTransform.anchorMin = new Vector2(0.02f, 0.15f);  // 약간의 패딩
            fillRectTransform.anchorMax = new Vector2(0.98f, 0.85f);  // 시작은 꽉 참
            fillRectTransform.sizeDelta = Vector2.zero;
            fillRectTransform.anchoredPosition = Vector2.zero;

            // HealthBarUI 컴포넌트 추가
            HealthBarUI healthBar = canvasObj.AddComponent<HealthBarUI>();
            healthBar.fillImage = fillImage;
            healthBar.backgroundImage = bgImage;
            healthBar.fillRect = fillRectTransform;  // RectTransform 할당
            healthBar.offset = new Vector3(0, -0.5f, 0);  // 발밑 오프셋 저장

            return healthBar;
        }
    }
}
