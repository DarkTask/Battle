using Mirror;
using Mirror.Examples.MultipleMatch;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Image;

public class CardElement : MonoBehaviour
{
    public MatchController matchController;

    //public Button button;

    public Image image;

    public TextMeshProUGUI name;

    public int player;

    public int index;

    public bool isSetup = false;

    [Header("Diagnostics")]
    [ReadOnly, SerializeField] internal NetworkIdentity playerIdentity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (matchController.DicCardElement.ContainsKey(player) == false)
        {
            matchController.DicCardElement.Add(player, new System.Collections.Generic.List<CardElement>());
            matchController.DicCardElement[player].Add(this);
        }
        else
        {
            matchController.DicCardElement[player].Add(this);
        }

        Debug.Log("matchController.DicCardElement : " + index);

        //button = GetComponent<Button>();

        //button.onClick.AddListener(OnClick);

        //string result = image.sprite.name.Substring(0, image.sprite.name.IndexOf('_'));

        //name.text = result;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ClientCallback]
    public void SetCard(NetworkIdentity playerIdentity, string championName)
    {
        if (playerIdentity != null)
        {
            this.playerIdentity = playerIdentity;
            name.text = championName;
        }
        else
        {
            this.playerIdentity = null;
            image.color = Color.white;
        }

        ChampionImageManager.Champion champion = (ChampionImageManager.Champion)Enum.Parse(typeof(ChampionImageManager.Champion), championName);

        var sprite = ChampionImageManager.instance.GetSprite(champion);
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }

        isSetup = true;
    }

    //[ClientCallback]
    //private void OnClick()
    //{
    //    if (matchController.currentPlayer.isLocalPlayer)
    //        matchController.CmdCharacterClick(index);

    //    Debug.Log(gameObject.name + " Character Element Clicked");
    //}

    //[ClientCallback]
    //public void SetPlayer(NetworkIdentity playerIdentity)
    //{
    //    if (playerIdentity != null)
    //    {
    //        this.playerIdentity = playerIdentity;
    //        image.color = this.playerIdentity.isLocalPlayer ? Color.blue : Color.red;
    //        button.interactable = false;
    //    }
    //    else
    //    {
    //        this.playerIdentity = null;
    //        image.color = Color.white;
    //        button.interactable = true;
    //    }
    //}
}
