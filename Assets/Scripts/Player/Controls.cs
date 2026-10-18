using UnityEngine;

public static class Controls
{
    // Camera
    public static float sensX = 1f;
    public static float sensY = 1f;
    public static KeyCode focus = KeyCode.Mouse1;

    // Movement
    public static KeyCode moveForward = KeyCode.W;
    public static KeyCode moveBackward = KeyCode.S;
    public static KeyCode moveLeft = KeyCode.A;
    public static KeyCode moveRight = KeyCode.D;

    // Interaction
    public static KeyCode primaryInteraction = KeyCode.E;
    public static KeyCode cursorInteraction = KeyCode.Mouse0;

    // Tabletop

    public static KeyCode finishedWithTabletopMode = KeyCode.Space;
    public static KeyCode exitOutOfItemMode = KeyCode.Tab;
}
