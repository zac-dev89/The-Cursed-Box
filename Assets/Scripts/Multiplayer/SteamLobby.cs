using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    public NetworkManager networkManager;

    [Header("Steam Specific")]
    public ulong lobbyID = 0;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyChatUpdate_t> lobbyChatUpdate;

    private const string HostAddressKey = "HostAddress";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        networkManager = GetComponent<NetworkManager>();
        if (!SteamManager.Initialized)
        {
            Debug.Log("STEAM IS NOT INITIALIZED");
            return;
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

    }

    public void HostLobby()
    {
        NetworkManager.singleton.maxConnections = 4;
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }
    void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.Log("Failed to create lobby: " + callback.m_eResult);
            return;
        }

        Debug.Log("Lobby successfully created. Lobby ID: " + callback.m_ulSteamIDLobby);

        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());

        string lobbyName = SteamFriends.GetPersonaName().ToString() + "'s Lobby";
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "LobbyName", lobbyName);

        lobbyID = callback.m_ulSteamIDLobby;

        networkManager.ServerChangeScene("Gameplay");
    }
    void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Join request recieved for lobby: " + callback.m_steamIDLobby);

        if (NetworkClient.isConnected || NetworkClient.active)
        {
            Debug.Log("NetworkClient is active or connected. Disconnecting before joining new lobby");
            NetworkManager.singleton.StopClient();
            NetworkClient.Shutdown();
        }

        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }
    void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active)
        {
            Debug.Log("Already in a lobby as a host. Ignoring join request");
            return;
        }

        GameTypeManager.Instance.InitiateMultiplayerGame();

        lobbyID = callback.m_ulSteamIDLobby;
        string _hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
        networkManager.networkAddress = _hostAddress;
        Debug.Log("Entered lobby: " + callback.m_ulSteamIDLobby);
        networkManager.StartClient();
    }
    void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if (callback.m_ulSteamIDLobby != lobbyID) return;

        EChatMemberStateChange stateChange = (EChatMemberStateChange)callback.m_rgfChatMemberStateChange;
        Debug.Log($"LobbyChatUpdate: {stateChange}");

        bool shouldUpdate = stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeEntered) ||
                            stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeLeft) ||
                            stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeDisconnected) ||
                            stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeKicked) ||
                            stateChange.HasFlag(EChatMemberStateChange.k_EChatMemberStateChangeBanned);

        if (shouldUpdate)
        {
            StartCoroutine(DelayedNameUpdate(0.5f));
        }

    }

    private IEnumerator DelayedNameUpdate(float delay)
    {
        yield return new WaitForSeconds(delay);
    }

    public void InviteFriends()
    {
        if (lobbyID == 0)
        {
            return;
        }
        SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(lobbyID));
    }

    public void LeaveSteamLobby()
    {
        if (lobbyID != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(lobbyID));
            lobbyID = 0;
        }
    }
 
}
