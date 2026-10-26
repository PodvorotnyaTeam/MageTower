using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Dot in the middle of the screen. It marks the point the interaction ray goes
/// through (PlayerInteractor casts through the centre of the camera view).
/// It builds its own overlay canvas when a scene with a player loads, so
/// nothing has to be set up in the scene. Hidden while a menu is open or the
/// game is paused.
/// </summary>
public class Crosshair : MonoBehaviour
{
    private const int TextureSize = 32;
    private const float DotSize = 12f;

    private Image image;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded += (scene, mode) => Spawn();
    }

    private static void Spawn()
    {
        if (FindFirstObjectByType<PlayerInteractor>() == null || FindFirstObjectByType<Crosshair>() != null)
            return;

        var canvasObject = new GameObject("Crosshair");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Anchored to the middle of the canvas, which covers the whole screen,
        // so the dot is in the exact centre at any resolution.
        var dot = new GameObject("Dot");
        dot.transform.SetParent(canvasObject.transform, false);

        var rect = dot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(DotSize, DotSize);

        var dotImage = dot.AddComponent<Image>();
        dotImage.sprite = MakeDotSprite();
        dotImage.raycastTarget = false;

        canvasObject.AddComponent<Crosshair>().image = dotImage;
    }

    // A white dot with a soft dark rim so it can be seen on light and dark backgrounds.
    private static Sprite MakeDotSprite()
    {
        var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        float centre = (TextureSize - 1) / 2f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                float core = Mathf.Clamp01(6.5f - distance);
                float rim = 0.6f * Mathf.Clamp01(9.5f - distance);
                Color color = Color.Lerp(Color.black, Color.white, core);
                color.a = Mathf.Max(core, rim);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), 100f);
    }

    private void Update()
    {
        if (image != null)
            image.enabled = !PlayerInputHandler.fromInventory && Time.timeScale > 0f;
    }
}
