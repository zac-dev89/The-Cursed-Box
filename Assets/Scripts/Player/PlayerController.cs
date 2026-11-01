using Mirror;
using System.Collections;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Main References")]
    public PlayerGameState playerGameState;
    public Transform FPSCamera;
    public Camera camera;
    public GameplayUIHandler gameplayUIHandler;
    public CharacterController characterController;

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
    public bool isCursorOnInteractable;


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
        else if (playerGameState.playerState == PlayerState.DungeonHunter && GameManager.Instance.allDungeonPlayersReady)
        {
            DungeonHunterGameplay();
        }
        else if (playerGameState.playerState == PlayerState.DungeonVictim && GameManager.Instance.allDungeonPlayersReady)
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
        characterController.enabled = false;
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

        if (!hasCursorInteractHit)
        {
            isCursorOnInteractable = false;
            return;
        }

        Interactable tempInteractable = cursorInteractHit.collider.GetComponent<Interactable>();
        InteractablePassOver tempInteractablePassOver = cursorInteractHit.collider.GetComponent<InteractablePassOver>();

        if ((tempInteractable != null && tempInteractable.CanInteract(cursorInteractHit.collider.gameObject, this.netIdentity).canInteract) ||
            (tempInteractablePassOver != null && tempInteractablePassOver.GetInteractableTarget().CanInteract(cursorInteractHit.collider.gameObject, this.netIdentity).canInteract))
        {
            isCursorOnInteractable = true;
        }
        else
        {
            isCursorOnInteractable = false;
        }
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


    #region Camera Focusing
    private Coroutine focusingCorutine;
    private bool inFocusingTransition;

    public void FocusAtPoint(Transform lookAtPT, float zoom, float duration)
    {
        Quaternion lookRot = Quaternion.LookRotation((lookAtPT.position - FPSCamera.transform.position).normalized);

        if (focusingCorutine != null) StopCoroutine(focusingCorutine);
        focusingCorutine = StartCoroutine(PlayerCameraFocusing(lookRot, zoom, duration, false));
    }
    public void LerpCameraRotation(Quaternion lookAtRot, float zoom, float duration)
    {
        if (focusingCorutine != null) StopCoroutine(focusingCorutine);
        focusingCorutine = StartCoroutine(PlayerCameraFocusing(lookAtRot, zoom, duration, true));
    }
    public void ExitFocus(float duration)
    {
        if (focusingCorutine != null) StopCoroutine(focusingCorutine);
        focusingCorutine = StartCoroutine(PlayerCameraFocusing(transform.rotation, tabletopFOV, duration, false));
    }

    IEnumerator PlayerCameraFocusing(Quaternion targetRot, float targetFOV, float duration, bool isLocalRot)
    {
        float elapsedTime = 0f;
        Quaternion initialRot = FPSCamera.rotation;
        float initialFOV = camera.fieldOfView;

        inFocusingTransition = true;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t = Mathf.Clamp01(elapsedTime / duration);

            // Exponential Ease Out
            float smoothT = t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);

            if (!isLocalRot) FPSCamera.rotation = Quaternion.Slerp(initialRot, targetRot, smoothT);
            else FPSCamera.localRotation = Quaternion.Slerp(initialRot, targetRot, smoothT);

            camera.fieldOfView = Mathf.Lerp(initialFOV, targetFOV, smoothT);

            yield return null;
        }

        inFocusingTransition = false;

        // Ensure exact final values
        FPSCamera.rotation = targetRot;
        camera.fieldOfView = targetFOV;
    }

    #endregion

    #region Table Top Gameplay

    [Header("Tabletop Gameplay")]
    public float tabletopFOV = 70f;
    public float tabletopFocusFOV = 50f;

    [Space]
    public float tabletopCameraTiltAngle = 3f;


    public void OnPlayerTabletopTurn(PlayerTabletopTurn tabletopTurn)
    {
        if (playerGameState.playerState == PlayerState.DungeonVictim || playerGameState.playerState == PlayerState.DungeonHunter) return;

        PlayerTabletopState tabletopState = tabletopTurn.playerTabletopState;

        if (tabletopState == PlayerTabletopState.Itembox)
        {
            if (tabletopTurn.inItemBoxInsertMode)
            {
                gameplayUIHandler.ToggleAddBloodPackPanel();
                gameplayUIHandler.UpdateItemBoxItemsUI();
                FocusAtPoint(GameManager.Instance.cursedBoxStation.insertItemBoxItemTrigger.transform, 30f, .5f);
            }
            else
            {
                gameplayUIHandler.HideAllPanels(gameplayUIHandler.itemboxUIPanels);
                FocusAtPoint(GameManager.Instance.cursedBoxStation.itemBox.transform, 40f, .5f);
            }

            gameplayUIHandler.ToggleItemBoxPanel();

            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        else if (tabletopState == PlayerTabletopState.Cursedbox)
        {
            if (tabletopTurn.inCursedBoxInsertMode && !tabletopTurn.peekingAtBox)
            {
                gameplayUIHandler.ToggleAddItemsPanel();
                gameplayUIHandler.UpdateCursedBoxItemsUI();
                FocusAtPoint(GameManager.Instance.cursedBoxStation.insertCursedBoxItemTrigger.transform, 30f, .5f);

                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
            else if (tabletopTurn.peekingAtBox)
            {
                gameplayUIHandler.HideAllPanels(gameplayUIHandler.cursedboxUIPanels);
                FocusAtPoint(GameManager.Instance.cursedBoxStation.peekInsideCursedBox.transform, 25f, .5f);

                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
            else
            {
                gameplayUIHandler.HideAllPanels(gameplayUIHandler.cursedboxUIPanels);
                FocusAtPoint(GameManager.Instance.cursedBoxStation.cursedBox.transform, 40f, .5f);

                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }

            gameplayUIHandler.ToggleCursedBoxPanel();         
        }
        else
        {
            ExitFocus(.5f);
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
        else if (!inFocusingTransition)
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

        if (playerTabletopTurn.inTurn && playerTabletopTurn.inItemBoxInsertMode && !playerTabletopTurn.hasUsedItemBoxItem && !playerTabletopTurn.inInteractionCooldown)
        {
            CmdTryExitBloodPackMode();
        }
    }

    [Command]
    private void CmdTryExitBloodPackMode()
    {
        PlayerTabletopTurn playerTabletopTurn = playerGameState.playerTabletopTurn;

        if (playerTabletopTurn.inTurn && playerTabletopTurn.inItemBoxInsertMode && !playerTabletopTurn.hasUsedItemBoxItem && !playerTabletopTurn.inInteractionCooldown)
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

        if (playerTabletopTurn.inTurn && playerTabletopTurn.inCursedBoxInsertMode && playerTabletopTurn.insertedItems < GameManager.Instance.cursedBoxStation.cursedBoxItemsCap && !playerTabletopTurn.inInteractionCooldown)
        {
            CmdTryExitItemCardMode();
        }
    }

    [Command]
    private void CmdTryExitItemCardMode()
    {
        PlayerTabletopTurn playerTabletopTurn = playerGameState.playerTabletopTurn;

        if (playerTabletopTurn.inTurn && playerTabletopTurn.inCursedBoxInsertMode && playerTabletopTurn.insertedItems < GameManager.Instance.cursedBoxStation.cursedBoxItemsCap && !playerTabletopTurn.inInteractionCooldown)
        {
            GameManager.Instance.cursedBoxStation.ServerExitItemCardMode(this.netIdentity);
        }
    }



    // Tabletop Looking
    float currentCameraTilt = 0f;
    public void TabletopLooking()
    {
        // Get the actual camera rotation
        Vector3 lookRot = FPSCamera.localEulerAngles;

        // Convert Unity's 0-360 angles into -180 to 180
        lookRot.x = Mathf.DeltaAngle(0f, lookRot.x);
        lookRot.y = Mathf.DeltaAngle(0f, lookRot.y);

        // Apply mouse input to the current camera rotation
        lookRot.x -= Input.GetAxis("Mouse Y") * Controls.sensY;
        lookRot.y += Input.GetAxis("Mouse X") * Controls.sensX;

        // Clamp pitch and yaw
        lookRot.x = Mathf.Clamp(lookRot.x, -60f, 60f);
        lookRot.y = Mathf.Clamp(lookRot.y, -80f, 80f);


        // Focusing
        float currentCameraFOV = camera.fieldOfView;
        float targetCameraFOV = Input.GetKey(Controls.focus) ? tabletopFocusFOV : tabletopFOV;
        camera.fieldOfView = Mathf.Lerp(currentCameraFOV, targetCameraFOV, 15f * Time.deltaTime);

        // Camera Tilt
        float targetCamTilt = 0f;
        if (Input.GetAxis("Mouse X") > 0) targetCamTilt = tabletopCameraTiltAngle;
        else if (Input.GetAxis("Mouse X") < 0) targetCamTilt = -tabletopCameraTiltAngle;

        currentCameraTilt = Mathf.Lerp(currentCameraTilt, targetCamTilt, 10f * Time.deltaTime);

        // Preserve your camera tilt
        FPSCamera.localRotation = Quaternion.Euler(
            lookRot.x,
            lookRot.y,
            currentCameraTilt
        );
    }

    #endregion



    #region Dungeon Victim Gameplay
    [Header("Dungeon Victim Movement")]
    public float dungeonVictimFOV = 70f;
    public float dungeonVictimFocusFOV = 50f;
    public float dungeonVictimCameraTiltAngle = 4f;

    [Space]

    public float dungeonVictimWalkSpeed = 5f;
    public float dungeonVictimRunSpeed = 7.5f;
    private float dungeonVictimMoveSpeed = 0f;


    [Space]

    public float dungeonVictimAcceleration = 12f;
    public float dungeonVictimDeceleration = 16f;
    public float dungeonVictimGravity = -20f;
    private Vector3 inputDirection;
    public Vector3 dungeonVictimVelocity;


    public void OnDungeonVictimStarted()
    {
        dungeonVictimMoveSpeed = dungeonVictimWalkSpeed;
        characterController.enabled = true;

        if (focusingCorutine != null) StopCoroutine(focusingCorutine);
        LerpCameraRotation(Quaternion.Euler(0,0,0), dungeonVictimFOV, .5f);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void DungeonVictimGameplay()
    {
        if (IsDungeonVictimFrozen()) return;

        DungeonVictimLooking();
        DungeonVictimMovementSpeed();
        DungeonVictimMovement();
        DungeonVictimInteraction();



    }
    private void DungeonVictimLooking()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Get current camera rotation
        Vector3 lookRot = FPSCamera.localEulerAngles;

        lookRot.x = Mathf.DeltaAngle(0f, lookRot.x);
        lookRot.y = Mathf.DeltaAngle(0f, lookRot.y);

        // Apply mouse input
        lookRot.x -= mouseY * Controls.sensY;
        lookRot.y += mouseX * Controls.sensX;

        // Clamp pitch
        lookRot.x = Mathf.Clamp(lookRot.x, -80f, 80f);

        // Transfer camera yaw to player
        float playerYaw = transform.eulerAngles.y + lookRot.y;

        transform.rotation = Quaternion.Euler(0f, playerYaw, 0f);

        // Focus
        float targetFOV = Input.GetKey(Controls.focus)
            ? dungeonVictimFocusFOV
            : dungeonVictimFOV;

        camera.fieldOfView = Mathf.Lerp(
            camera.fieldOfView,
            targetFOV,
            15f * Time.deltaTime
        );

        // Camera tilt
        float targetCamTilt = 0f;

        if (mouseX > 0f)
            targetCamTilt = dungeonVictimCameraTiltAngle;
        else if (mouseX < 0f)
            targetCamTilt = -dungeonVictimCameraTiltAngle;

        currentCameraTilt = Mathf.Lerp(
            currentCameraTilt,
            targetCamTilt,
            10f * Time.deltaTime
        );

        // Reset local yaw after transferring it to the player
        FPSCamera.localRotation = Quaternion.Euler(
            lookRot.x,
            0f,
            0f // currentCameraTilt
        );
    }
    private void DungeonVictimMovementSpeed()
    {
        bool movingStraight = Vector3.Angle(inputDirection.normalized, transform.forward) < 90f;

        //Debug.Log($"Run Key Down: {Input.GetKey(Controls.run)} || Velocity > 0: {dungeonVictimVelocity.magnitude > 2f} || Is Moving Straight {movingStraight}");

        if (Input.GetKey(Controls.run) && dungeonVictimVelocity.magnitude > 2f && movingStraight)
        {
            dungeonVictimMoveSpeed = dungeonVictimRunSpeed;
        }
        else
        {
            dungeonVictimMoveSpeed = dungeonVictimWalkSpeed;
        }
    }
    private void DungeonVictimMovement()
    {
        float inputX = Input.GetAxisRaw("Horizontal");
        float inputZ = Input.GetAxisRaw("Vertical");

        // Get movement direction relative to player facing.
        inputDirection = transform.right * inputX
                               + transform.forward * inputZ;

        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        // Current horizontal velocity.
        Vector3 currentHorizontalVelocity = new Vector3(
            dungeonVictimVelocity.x,
            0f,
            dungeonVictimVelocity.z
        );

        // Desired horizontal velocity.
        Vector3 targetHorizontalVelocity =
            inputDirection * dungeonVictimMoveSpeed;

        // Use separate acceleration and stopping speeds.
        float acceleration = inputDirection.sqrMagnitude > 0.01f
            ? dungeonVictimAcceleration
            : dungeonVictimDeceleration;

        // Smooth velocity directly toward the target.
        Vector3 newHorizontalVelocity = Vector3.MoveTowards(
            currentHorizontalVelocity,
            targetHorizontalVelocity,
            acceleration * Time.deltaTime
        );

        dungeonVictimVelocity.x = newHorizontalVelocity.x;
        dungeonVictimVelocity.z = newHorizontalVelocity.z;

        // Gravity.
        if (characterController.isGrounded && dungeonVictimVelocity.y < 0f)
        {
            dungeonVictimVelocity.y = -2f;
        }
        else
        {
            dungeonVictimVelocity.y += dungeonVictimGravity * Time.deltaTime;
        }

        // Apply movement.
        characterController.Move(
            dungeonVictimVelocity * Time.deltaTime
        );
    }
    private void DungeonVictimInteraction()
    {
        if (Input.GetKeyDown(Controls.primaryInteraction) && hasInteractHit)
        {
            TryInteract(interactHit, FPSCamera.position, FPSCamera.forward);
        }
    }

    private bool IsDungeonVictimFrozen()
    {
        return playerGameState.isMovingThroughGap || !playerGameState.isAlive;
    }
    #endregion



    #region Dungeon Hunter Gameplay
    private void DungeonHunterGameplay()
    {

    }
    #endregion
}
