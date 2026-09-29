using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using TMPro;
using Steamworks;

public class PlayerLobbyHandler : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnReadyStatusChanged))] public bool isReady = false;
    private Button readyButton;
    private TextMeshProUGUI readyButtonText;
    public TextMeshProUGUI readyText;
    public TextMeshProUGUI nameText;

    public CSteamID steamID;
    public RawImage steamPFP;


    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        readyButton = LobbyManager.Instance.readyButton;
        readyButton.onClick.AddListener(OnReadyButtonClicked);
        readyButtonText = readyButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        isReady = false;
    }
    private void Start()
    {
        LobbyManager.Instance.RegisterPlayer(this);
    }

    public void OnReadyButtonClicked()
    {
        CmdSetReady();
    }
    [Command]
    public void CmdSetReady()
    {
        isReady = !isReady;
    }

    void OnReadyStatusChanged(bool oldValue, bool newValue)
    {
        if (NetworkServer.active)
        {
            LobbyManager.Instance.CheckAllPlayersReady();
        }

        if (isReady)
        {
            readyButtonText.text = "Unready";
            readyText.text = "READY";
        }
        else
        {
            readyButtonText.text = "Ready";
            readyText.text = "NOT READY";
        }
    }


}
