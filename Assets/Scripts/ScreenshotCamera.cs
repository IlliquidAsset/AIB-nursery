using System.IO;
using UnityEngine;

/// <summary>
/// Captures screenshots from the main camera and saves them to the device's storage.
/// </summary>
[RequireComponent(typeof(Camera))]
public class ScreenshotCamera : MonoBehaviour
{
    [Header("Screenshot Settings")]
    public int fileCounter = 0;
    public RenderTexture renderTexture;
    // AIB-screenshot-route-072 2026-05-07: canonical AIB SmokeRenders folder.
    public string filePath = "Assets/AIB/SmokeRenders";
    public string fileName = "capture";
    private Camera screenshotCam;
    public bool testMode = false;

    private void Awake()
    {
        screenshotCam = GetComponent<Camera>();
        if (screenshotCam == null)
        {
            Debug.LogError("Camera component is missing from the GameObject.");
            return;
        }
        InitializeRenderTexture();
    }

    private void InitializeRenderTexture()
    {
        if (!testMode && renderTexture != null)
        {
            screenshotCam.targetTexture = new RenderTexture(
                renderTexture.width,
                renderTexture.height,
                renderTexture.depth,
                renderTexture.format,
                RenderTextureReadWrite.sRGB
            );
        }
        else
        {
            Debug.LogWarning("RenderTexture is not assigned or in Test Mode.");
        }
    }

    public void Activate(bool enable = true)
    {
        if (screenshotCam == null)
        {
            Debug.LogError("Screenshot Camera is not assigned.");
            return;
        }
        screenshotCam.enabled = enable;
    }

    private void LateUpdate()
    {
        if (screenshotCam.enabled && !testMode)
        {
            CaptureScreenshot();
            Activate(false);
        }
    }

    private void CaptureScreenshot()
    {
        if (screenshotCam == null)
        {
            Debug.LogError("Screenshot Camera is not assigned.");
            return;
        }

        if (screenshotCam.targetTexture == null)
        {
            Debug.LogError("No RenderTexture assigned to the Camera.");
            return;
        }

        screenshotCam.Render();
        RenderTexture.active = screenshotCam.targetTexture;

        Texture2D image = new Texture2D(
            screenshotCam.targetTexture.width,
            screenshotCam.targetTexture.height,
            TextureFormat.RGB24,
            false
        );
        image.ReadPixels(
            new Rect(0, 0, screenshotCam.targetTexture.width, screenshotCam.targetTexture.height),
            0,
            0
        );
        image.Apply();
        byte[] bytes = image.EncodeToPNG();
        Destroy(image);

        // AIB-screenshot-route-072 2026-05-07: route screenshots to the
        // canonical AIB SmokeRenders folder. Absolute filePath used as-is;
        // relative filePath resolved under the project root (or the build's
        // dataPath parent at runtime). Never Application.persistentDataPath.
        string directoryPath;
        if (Path.IsPathRooted(filePath))
        {
            directoryPath = filePath;
        }
        else
        {
#if UNITY_EDITOR
            // Project root = parent of Application.dataPath ("Assets")
            directoryPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), filePath);
#else
            // Runtime in built player: hardcoded canonical location matches
            // /Volumes/Video/AIB-nursery/Assets/AIB/SmokeRenders, which is
            // the same location used by the editor smoke. Honors inbox 072.
            directoryPath = "/Volumes/Video/AIB-nursery/" + filePath;
#endif
        }
        Directory.CreateDirectory(directoryPath);

        string formattedFileName =
            $"{fileName}{fileCounter}_{System.DateTime.Now:dd-MM_HH-mm-ss}.png";
        string fullPath = Path.Combine(directoryPath, formattedFileName);

        File.WriteAllBytes(fullPath, bytes);
        fileCounter++;
    }
}
