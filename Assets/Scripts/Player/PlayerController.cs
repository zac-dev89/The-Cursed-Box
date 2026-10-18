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


    [Header("Interaction")]
    public float interactionDistance = 2.5f;
    bool hasInteractHit;
    RaycastHit interactHit;

    [Space]

    bool hasCursorInteractHit;
    Ray cursorInteractRay;
    RaycastHit cursorInteractHit;


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

        InteractionChecking();
        CursorInteractionChecking();

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
        gameplayUIHandler.ToggleNonTabletopTurnPanel();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        camera.fieldOfView = tabletopFOV;
    }

    #region Interaction
    private void InteractionChecking()
    {
        hasInteractHit = Physics.Raycast(FPSCamera.position, FPSCamera.forward, out interactHit, interactionDistance);
    }
    private void CursorInteractionChecking()
    {
        cursorInteractRay = camera.ScreenPointToRay(Input.mousePosition);
        hasCursorInteractHit = Physics.Raycast(cursorInteractRay, out cursorInteractHit);
    }

    private void TryInteract(RaycastHit interactHit, Vector3 origin, Vector3 direction)
    {
        Interactable tempInteractable = interactHit.collider.GetComponent<Interactable>();
        InteractablePassOver tempInteractablePassOver = interactHit.collider.GetComponent<InteractablePassOver>();

        if (tempInteractable != null && tempInteractable.CanInteract(interactHit.collider.gameObject, this.netIdentity).canInteract)
        {
            // CMD Interact
            CmdTryInteract(origin, direction);
        }
        else if (tempInteractablePassOver != null && tempInteractablePassOver.GetInteractableTarget().CanInteract(interactHit.collider.gameObject, this.netIdentity).canInteract)
        {
            // CMD Interact Passover
            CmdTryInteract(origin, direction);
        }
    }

    [Command]
    private void CmdTryInteract(Vector3 origin, Vector3 direction)
    {
        RaycastHit serverInteractHit;
        bool hasServerInteractHit = Physics.Raycast(origin, direction, out serverInteractHit, 100f);

        if (!hasServerInteractHit) return;

        Interactable tempInteractable = serverInteractHit.collider.GetComponent<Interactable>();
        InteractablePassOver tempInteractablePassOver = serverInteractHit.collider.GetComponent<InteractablePassOver>();

        if (tempInteractable != null && tempInteractable.CanInteract(serverInteractHit.collider.gameObject, this.netIdentity).canInteract)
        {
            // Interact
            tempInteractable.ServerInteract(serverInteractHit.collider.gameObject, this.netIdentity);
        }
        else if (tempInteractablePassOver != null && tempInteractablePassOver.GetInteractableTarget().CanInteract(serverInteractHit.collider.gameObject, this.netIdentity).canInteract)
        {
            // Interact Passover
            tempInteractablePassOver.GetInteractableTarget().ServerInteract(serverInteractHit.collider.gameObject, this.netIdentity);
        }
    }
    #endregion



    #region Table Top Gameplay

    [Header("Tabletop Gameplay")]
    public float tabletopFOV = 70f;
    public float tabletopFocusFOV = 50f;

    [Space]
    public float cameraTiltAngle = 3f;

    [Space]

    private Coroutine tabletopFocusingCorutine;
    private bool inTabletopFocusingTransition;

    private float tabletopLookX;
    private float tabletopLookY;

    public void OnPlayerTabletopTurn(PlayerTabletopTurn tabletopTurn)
    {
        PlayerTabletopState tabletopState = tabletopTurn.playerTabletopState;

        if (tabletopState == PlayerTabletopState.Itembox)
        {
            if (tabletopTurn.inBloodPackMode)
            {
                gameplayUIHandler.ToggleAddBloodPackPanel();
                FocusOnInteractable(GameManager.Instance.cursedBoxStation.addBloodPackTrigger.transform, 30f, .5f);
            }
            else
            {
                gameplayUIHandler.HideAllPanels(gameplayUIHandler.itemboxUIPanels);
                FocusOnInteractable(GameManager.Instance.cursedBoxStation.itemBox.transform, 40f, .5f);
            }

            gameplayUIHandler.ToggleItemBoxPanel();

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else if (tabletopState == PlayerTabletopState.Cursedbox)
        {
            if (tabletopTurn.inItemCardMode)
            {
                gameplayUIHandler.ToggleAddItemsPanel();
                FocusOnInteractable(GameManager.Instance.cursedBoxStation.insertItemCardTrigger.transform, 30f, .5f);
            }
            else
            {
                gameplayUIHandler.HideAllPanels(gameplayUIHandler.cursedboxUIPanels);
                FocusOnInteractable(GameManager.Instance.cursedBoxStation.cursedBox.transform, 40f, .5f);
            }

            gameplayUIHandler.ToggleCursedBoxPanel();

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else
        {
            ExitFocusOnInteractable(.5f);
            gameplayUIHandler.ToggleNonTabletopTurnPanel();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }


    private void TabletopGameplay()
    {
        if (playerGameState.playerTabletopTurn.inTurn)
        {
            if (playerGameState.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Itembox)
            {
                TabletopItemBoxGameplay();
            }
            else if (playerGameState.playerTabletopTurn.playerTabletopState == PlayerTabletopState.Cursedbox)
            {
                CursedBoxGameplay();
            }

        }
        else if (!inTabletopFocusingTransition)
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

        if (Input.GetKeyDown(Controls.exitOutOfItemMode))
        {
            TryExitBloodPackMode();
        }

        if (Input.GetKeyDown(Controls.cursorInteraction) && hasCursorInteractHit)
        {
            TryInteract(cursorInteractHit, cursorInteractRay.origin, cursorInteractRay.direction);
        }


    }
    private void TryExitBloodPackMode()
    {
        PlayerTabletopTurn playerTabletopTurn = playerGameState.playerTabletopTurn;

        if (playerTabletopTurn.inTurn && playerTabletopTurn.inBloodPackMode && !playerTabletopTurn.hasUsedBloodPack)
        {
            CmdTryExitBloodPackMode();
        }
    }

    [Command]
    private void CmdTryExitBloodPackMode()
    {
        PlayerTabletopTurn playerTabletopTurn = playerGameState.playerTabletopTurn;

        if (playerTabletopTurn.inTurn && playerTabletopTurn.inBloodPackMode && !playerTabletopTurn.hasUsedBloodPack)
        {
            GameManager.Instance.cursedBoxStation.ServerExitBloodPackMode(this.netIdentity);
        }
    }




    private void CursedBoxGameplay()
    {
        if (Input.GetKeyDown(Controls.cursorInteraction) && hasCursorInteractHit)
        {
            TryInteract(cursorInteractHit, cursorInteractRay.origin, cursorInteractRay.direction);
        }

        if (Input.GetKeyDown(Controls.exitOutOfItemMode))
        {
            TryExitItemCardMode();
        }
    }

    private void TryExitItemCardMode()
    {
        PlayerTabletopTurn playerTabletopTurn = playerGameState.playerTabletopTurn;

        if (playerTabletopTurn.inTurn && playerTabletopTurn.inItemCardMode && playerTabletopTurn.insertedItems < GameManager.Instance.cursedBoxStation.specialItemsCap)
        {
            CmdTryExitItemCardMode();
        }
    }

    [Command]
    private void CmdTryExitItemCardMode()
    {
        PlayerTabletopTurn playerTabletopTurn = playerGameState.playerTabletopTurn;

        if (playerTabletopTurn.inTurn && playerTabletopTurn.inItemCardMode && playerTabletopTurn.insertedItems < GameManager.Instance.cursedBoxStation.specialItemsCap)
        {
            GameManager.Instance.cursedBoxStation.ServerExitItemCardMode(this.netIdentity);
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
        tabletopFocusingCorutine = StartCoroutine(PlayerTabletopFocusing(lookRot, zoom, duration));
    }
    public void ExitFocusOnInteractable(float duration)
    {
        tabletopLookX = 0f;
        tabletopLookY = 0f;

        if (tabletopFocusingCorutine != null) StopCoroutine(tabletopFocusingCorutine);
        tabletopFocusingCorutine = StartCoroutine(PlayerTabletopFocusing(transform.rotation, tabletopFOV, duration));
    }

    IEnumerator PlayerTabletopFocusing(Quaternion targetRot, float targetFOV, float duration)
    {
        float elapsedTime = 0f;
        Quaternion initialRot = FPSCamera.rotation;
        float initialFOV = camera.fieldOfView;

        inTabletopFocusingTransition = true;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            // Exponential Ease Out
            float smoothT = t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);

            FPSCamera.rotation = Quaternion.Slerp(initialRot, targetRot, smoothT);
            camera.fieldOfView = Mathf.Lerp(initialFOV, targetFOV, smoothT);

            yield return null;
        }

        inTabletopFocusingTransition = false;

        // Ensure exact final values
        FPSCamera.rotation = targetRot;
        camera.fieldOfView = targetFOV;
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
