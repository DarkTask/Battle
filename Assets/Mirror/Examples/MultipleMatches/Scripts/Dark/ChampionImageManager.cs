using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ChampionImageManager : MonoBehaviour
{
    public static ChampionImageManager instance;

    // 챔피언 Enum 정의 (_0 제외)
    public enum Champion
    {
        Aatrox,
        Ahri,
        Ashe,
        Caitlyn,
        Galio,
        Garen,
        Irelia,
        Jhin,
        Kassadin,
        KogMaw,
        Lucian,
        MasterYi,
        Mordekaiser,
        Orianna,
        Ornn,
        Shen,
        Vi,
        Xerath,
        Zed,
        Ziggs
    }

    [Header("UI Reference")]
    public Image championImage;

    // 스프라이트 캐시
    private Dictionary<Champion, Sprite> spriteCache = new Dictionary<Champion, Sprite>();

    void Start()
    {
        instance = this;

        // 모든 챔피언 스프라이트 로드
        LoadAllChampions();

        // 테스트: 첫 번째 챔피언 표시
        //ShowChampion(Champion.Aatrox);
    }

    // 모든 챔피언 스프라이트 로드
    void LoadAllChampions()
    {
        foreach (Champion champion in System.Enum.GetValues(typeof(Champion)))
        {
            // Enum 이름에 _0 추가해서 로드
            string path = $"Icons/Splash/{champion}_0 (1)";
            Sprite sprite = Resources.Load<Sprite>(path);

            if (sprite != null)
            {
                spriteCache[champion] = sprite;
                Debug.Log($"Loaded: {champion}");
            }
            else
            {
                Debug.LogWarning($"Failed to load: {path}");
            }
        }
    }

    public Sprite GetSprite(Champion champion)
    {
        if (spriteCache.ContainsKey(champion))
        {
            return spriteCache[champion];
        }
        else
        {
            Debug.LogError($"Champion sprite not loaded: {champion}");
            return null;
        }
    }

    // 챔피언 이미지 표시
    public void ShowChampion(Champion champion)
    {
        if (spriteCache.ContainsKey(champion))
        {
            championImage.sprite = spriteCache[champion];
            Debug.Log($"Showing: {champion}");
        }
        else
        {
            Debug.LogError($"Champion sprite not loaded: {champion}");
        }
    }

    // 랜덤 챔피언 표시
    public void ShowRandomChampion()
    {
        Champion[] champions = (Champion[])System.Enum.GetValues(typeof(Champion));
        Champion randomChampion = champions[Random.Range(0, champions.Length)];
        ShowChampion(randomChampion);
    }

    // 다음 챔피언 표시
    public void ShowNextChampion()
    {
        Champion currentChampion = GetCurrentChampion();
        int currentIndex = (int)currentChampion;
        int nextIndex = (currentIndex + 1) % System.Enum.GetValues(typeof(Champion)).Length;
        ShowChampion((Champion)nextIndex);
    }

    // 이전 챔피언 표시
    public void ShowPreviousChampion()
    {
        Champion currentChampion = GetCurrentChampion();
        int currentIndex = (int)currentChampion;
        int previousIndex = (currentIndex - 1 + System.Enum.GetValues(typeof(Champion)).Length)
                           % System.Enum.GetValues(typeof(Champion)).Length;
        ShowChampion((Champion)previousIndex);
    }

    // 현재 표시된 챔피언 가져오기
    private Champion GetCurrentChampion()
    {
        foreach (var kvp in spriteCache)
        {
            if (kvp.Value == championImage.sprite)
            {
                return kvp.Key;
            }
        }
        return Champion.Aatrox; // 기본값
    }

    // 챔피언 이름으로 표시
    public void ShowChampionByName(string championName)
    {
        if (System.Enum.TryParse(championName, out Champion champion))
        {
            ShowChampion(champion);
        }
        else
        {
            Debug.LogError($"Invalid champion name: {championName}");
        }
    }

    // 모든 챔피언 리스트 가져오기
    public List<Champion> GetAllChampions()
    {
        return new List<Champion>((Champion[])System.Enum.GetValues(typeof(Champion)));
    }
}