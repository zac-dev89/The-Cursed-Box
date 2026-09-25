using UnityEngine;
using Mirror;

public class GameManager : NetworkBehaviour
{
    [SyncVar] public bool hasGameStarted = false;
}
