using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Keeps the mouse cursor locked in the middle of the screen while playing and
/// frees it in the inventory, the journal, the pause menu and the settings.
/// While it is free the camera does not follow the mouse. Created on its own
/// when the game starts; it only acts in a scene that has a player.
/// </summary>
public class CursorState : MonoBehaviour
{
    private const float SceneCheckInterval = 0.5f;

    private float nextSceneCheck;
    private bool hasPlayer;
    private CinemachineInputAxisController lookInput;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        var holder = new GameObject("CursorState");
        DontDestroyOnLoad(holder);
        holder.AddComponent<CursorState>();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextSceneCheck)
        {
            nextSceneCheck = Time.unscaledTime + SceneCheckInterval;
            hasPlayer = FindFirstObjectByType<PlayerInteractor>() != null;
            lookInput = FindFirstObjectByType<CinemachineInputAxisController>();
        }

        // The pause menu stops time, so a stopped game also means a free cursor.
        // A shop book being read frees the cursor too (BookStation does that itself every frame, so
        // locking it here would make the two fight).
        bool menuOpen = PlayerInputHandler.fromInventory
            || MageTower.Shop.BookStation.IsAnyBookOpen
            || Time.timeScale <= 0f;
        bool locked = hasPlayer && !menuOpen;

        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;

        if (lookInput != null)
            lookInput.enabled = locked;
    }
}
