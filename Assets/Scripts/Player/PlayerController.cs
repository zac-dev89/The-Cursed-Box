using UnityEngine;
using Mirror;
using Unity.VisualScripting;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [Header("Main References")]
    public PlayerGameState playerGameState;
    public Transform FPSCamera;
    public Camera camera;
    public GameplayUIHandler gameplayUIHandler;

    [Header("Enabling Gameplay")]
    [SyncVar] public bool playerControllerEnabled;

 

    RaycastHit interactHit;



    public override void OnStartLocalPlayer()    
    {
        if (GameController.Instance.isMultiplayerGame && !GameManager.Instance.hasGameStarted)
        {
            gameplayUIHandler.ToggleWaitingPanel();
        }
        else if (GameController.Instance.isSingleplayerGame)
        {
            gameplayUIHandler.ToggleTabletopPanel();
        }
    }



    private void Update()
    {
        if (!isLocalPlayer) return;
        if (!playerControllerEnabled) return;
        if (!playerGameState.playerStartedInGame) return;


        if (playerGameState.playerState == PlayerState.Tabletop)
        {
            TabletopGameplay();
        }
        else if (playerGameState.playerState == PlayerState.DungeonHunter)
        {
            DungeonHunterGameplay();
        }
        else if (playerGameState.playerState == PlayerState.DungeonVictim)
        {
            DungeonVictimGameplay();
        }
    }

    public void OnPlayerControllerStarted()
    {
        gameplayUIHandler.ToggleTabletopPanel();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        camera.fieldOfView = tabletopFOV;
    }

    #region Table Top Gameplay

    [Header("Tabletop Gameplay")]
    public float tabletopFOV = 70f;
    public float tabletopFocusFOV = 50f;

    [Space]
    public float cameraTiltAngle = 3f;

    [Space]

    private Coroutine tabletopFocusingCorutine;

    private float tabletopLookX;
    private float tabletopLookY;


    private void TabletopGameplay()
    {
        if (playerGameState.playerInTurn)
        {
            if (playerGameState.playerTabletopState == PlayerTabletopState.Itembox)
            {
                TabletopItemBoxGameplay();
            }
            else if (playerGameState.playerTabletopState == PlayerTabletopState.Cursedbox)
            {

            }

        }
        else
        {
            TabletopLooking();
        }

    }

    private void TabletopItemBoxGameplay()
    {
        if (Input.GetKeyDown(Controls.finishedWithTabletopMode))
        {
            playerGameState.CmdFinishedWithItemBox();
        }
    }




    // Tabletop Looking
    float currentCameraTilt = 0f;
    public void TabletopLooking()
    {
        // Look Around
        tabletopLookX += Input.GetAxis("Mouse X") * Controls.sensX;
        tabletopLookY += Input.GetAxis("Mouse Y") * Controls.sensY;

        tabletopLookY = Mathf.Clamp(tabletopLookY, -60f, 60f);
        tabletopLookX = Mathf.Clamp(tabletopLookX, -80f, 80f);

        Vector3 lookRot = new Vector3(-tabletopLookY, tabletopLookX, 0f);

        // Focusing
        float currentCameraFOV = camera.fieldOfView;
        float targetCameraFOV = Input.GetKey(Controls.focus) ? tabletopFocusFOV : tabletopFOV;
        camera.fieldOfView = Mathf.Lerp(currentCameraFOV, targetCameraFOV, 15f * Time.deltaTime);

        // Camera Tilt
        float targetCamTilt = 0f;
        if (Input.GetAxis("Mouse X") > 0) targetCamTilt = cameraTiltAngle;
        else if (Input.GetAxis("Mouse X") < 0) targetCamTilt = -cameraTiltAngle;

        currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetCamTilt, 10f * Time.deltaTime);


        FPSCamera.transform.localRotation = Quaternion.Euler(lookRot + new Vector3(0,0,currentCameraTilt));
    }


    // Focusing on interactables
    public void FocusOnInteractable(Transform lookAtPT, float zoom, float duration)
    {
        Quaternion lookRot = Quaternion.LookRotation((lookAtPT.position - FPSCamera.transform.position).normalized);

        if (tabletopFocusingCorutine != null) StopCoroutine(tabletopFocusingCorutine);
        tabletopFocusingCorutine = StartCoroutine(playerTabletopFocusing(lookRot, zoom, duration));
    }
    public void ExitFocusOnInteractable()
    {
        if (tabletopFocusingCorutine != null) StopCoroutine(tabletopFocusingCorutine);
        tabletopFocusingCorutine = StartCoroutine(playerTabletopFocusing(transform.rotation, tabletopFOV, .2f));
    }

    IEnumerator playerTabletopFocusing(Quaternion targetRot, float targetFOV, float duration)
    {
        float elapsedTime = 0f;
        Quaternion initialRot = FPSCamera.rotation;
        float initialFOV = camera.fieldOfView;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            FPSCamera.rotation = Quaternion.Slerp(initialRot, targetRot, elapsedTime / duration);
            camera.fieldOfView = Mathf.Lerp(initialFOV, targetFOV, elapsedTime / duration);
            yield return null;
        }

    }
    #endregion



    #region Dungeon Gameplay
    private void DungeonHunterGameplay()
    {

    }
    private void DungeonVictimGameplay()
    {

    }
    #endregion
}
