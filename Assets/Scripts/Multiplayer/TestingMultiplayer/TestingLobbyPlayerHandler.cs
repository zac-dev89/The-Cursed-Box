using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;

public class TestingLobbyPlayerHandler : NetworkBehaviour
{
    public override void OnStartServer()
    {
        base.OnStartServer();
        TestingLobbyManager.Instance.ServerRegisterPlayer();
    }
}
