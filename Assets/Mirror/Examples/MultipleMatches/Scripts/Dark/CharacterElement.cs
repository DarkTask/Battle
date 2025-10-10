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

    [Header("Diagnostics")]
    [ReadOnly, SerializeField] internal NetworkIdentity playerIdentity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
        
    }

    [ClientCallback]
    private void OnClick()
    {
        if (matchController.currentPlayer.isLocalPlayer)
            matchController.CmdCharacterClick(index);

        Debug.Log(gameObject.name + " Character Element Clicked");
    }

    [ClientCallback]
    public void SetPlayer(NetworkIdentity playerIdentity)
    {
        if (playerIdentity != null)
        {
            this.playerIdentity = playerIdentity;
            image.color = this.playerIdentity.isLocalPlayer ? Color.blue : Color.red;
            button.interactable = false;
        }
        else
        {
            this.playerIdentity = null;
            image.color = Color.white;
            button.interactable = true;
        }
    }
}
