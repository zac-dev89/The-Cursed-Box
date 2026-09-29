using Mirror;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerRegistration : NetworkBehaviour
{
    private bool sentReady;
    public bool isReady;

    [SyncVar] public string playerName;

    public override void OnStartServer()
    {
        base.OnStartServer();
        PlayerManager.Instance.ServerRegister(netIdentity);
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        PlayerManager.Instance.ServerUnregister(netIdentity);
    }

    // Client once they are ready
    public override void OnStartLocalPlayer()
    {
        // Network ready, so move to ground loaded in check
        base.OnStartLocalPlayer();
        StartCoroutine(WaitForLoadIn());
    }

    IEnumerator WaitForLoadIn()
    {
        while (!Physics.Raycast(transform.position, Vector3.down, 10f))
        {
            yield return null;
        }

        // Ground has loaded in so player is ready
        ReportFullyReady();
    }


    private void ReportFullyReady()
    {
        if (sentReady) return;

        sentReady = true;

        CmdReportFullyReady();
    }

    [Command]
    private void CmdReportFullyReady()
    {
        isReady = true;
        PlayerManager.Instance.ServerCheckAllPlayersReady();
    }
}
