using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using Steamworks;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;
    public Transform playerListParent;
    public List<TextMeshProUGUI> playerNameTexts = new List<TextMeshProUGUI>();
    public List<PlayerLobbyHandler> playerLobbyHandlers = new List<PlayerLobbyHandler>();
    public TextMeshProUGUI lobbyNameText;

    public Button playGameButton;
    public Button readyButton;
    public Button leaveLobbyButton;
    public Button inviteFriendsButton;

    protected Callback<AvatarImageLoaded_t> avatarLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        playGameButton.interactable = false;
        avatarLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarLoaded);

        SetUpListeners();
    }

    private void SetUpListeners()
    {
        try
        {
            leaveLobbyButton.onClick.AddListener(GameController.Instance.ExitMatch);
            inviteFriendsButton.onClick.AddListener(SteamLobby.Instance.InviteFriends);
        }
        catch
        {
            Debug.Log("Couldn't set up listeners");
        }
    }

    public void UpdatePlayerLobbyUI()
    {
        lobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(SteamLobby.Instance.lobbyID), "LobbyName");

        playerNameTexts.Clear();
        playerLobbyHandlers.Clear();

        var lobby = new CSteamID(SteamLobby.Instance.lobbyID);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobby);

        CSteamID hostID = new CSteamID(ulong.Parse(SteamMatchmaking.GetLobbyData(lobby, "HostAddress")));
        List<CSteamID> orderedMembers = new List<CSteamID>();

        if (memberCount == 0)
        {
            Debug.Log("Lobby has no members... retrying...");
            StartCoroutine(RetryUpdate());
            return;
        }

        orderedMembers.Add(hostID);

        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberID = SteamMatchmaking.GetLobbyMemberByIndex(lobby, i);
            if (memberID != hostID)
            {
                orderedMembers.Add(memberID);
            }
        }

        int j = 0;
        foreach(var member in orderedMembers)
        {
            TextMeshProUGUI playerNameText = playerListParent.GetChild(j).GetComponent<PlayerLobbyHandler>().nameText;
            PlayerLobbyHandler playerLobbyHandler = playerListParent.GetChild(j).GetComponent<PlayerLobbyHandler>();

            playerLobbyHandlers.Add(playerLobbyHandler);
            playerNameTexts.Add(playerNameText);

            string playerName = SteamFriends.GetFriendPersonaName(member);
            playerNameText.text = playerName;

            playerLobbyHandler.steamID = member;
            playerLobbyHandler.steamPFP.texture = null;

            int imageID = SteamFriends.GetLargeFriendAvatar(member);

            if (imageID != -1)
            {
                Texture2D avatar = GetSteamAvatar(member);
                if (avatar != null)
                    playerLobbyHandler.steamPFP.texture = avatar;
            }


            j++;
        }
    }

    // Steam Avatar

    Texture2D GetSteamAvatar(CSteamID steamID)
    {
        int imageID = SteamFriends.GetLargeFriendAvatar(steamID);

        if (imageID == -1)
            return null;

        uint width, height;

        if (!SteamUtils.GetImageSize(imageID, out width, out height))
            return null;

        byte[] image = new byte[width * height * 4];

        if (!SteamUtils.GetImageRGBA(imageID, image, (int)(width * height * 4)))
            return null;

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(image);
        texture.Apply();

        return texture;
    }

    void OnAvatarLoaded(AvatarImageLoaded_t callback)
    {
        CSteamID steamID = callback.m_steamID;

        Texture2D avatar = GetSteamAvatar(steamID);

        if (avatar == null)
            return;

        foreach (var player in playerLobbyHandlers)
        {
            if (player.steamID == steamID)
            {
                player.steamPFP.texture = avatar;
                break;
            }
        }
    }


    public void OnPlayButtonClicked()
    {
        if (NetworkServer.active)
        {
            SteamMatchmaking.SetLobbyJoinable(new CSteamID(SteamLobby.Instance.lobbyID), false);
            NetworkManager.singleton.ServerChangeScene("Gameplay");
        }
    }

    public void RegisterPlayer(PlayerLobbyHandler player)
    {
        player.transform.SetParent(playerListParent, false);
        UpdatePlayerLobbyUI();
    }

    [Server]
    public void CheckAllPlayersReady()
    {
        foreach (var player in playerLobbyHandlers)
        {
            if (!player.isReady)
            {
                RpcSetPlayButtonInteractable(false);
                return;
            }
        }

        RpcSetPlayButtonInteractable(true);
    }
    [ClientRpc]
    void RpcSetPlayButtonInteractable(bool truthStatus)
    {
        playGameButton.interactable = truthStatus;
    }
    IEnumerator RetryUpdate()
    {
        yield return new WaitForSeconds(1f);
        UpdatePlayerLobbyUI();
    }
}
