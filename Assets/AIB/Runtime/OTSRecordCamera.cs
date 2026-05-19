using UnityEngine;

public class OTSRecordCamera : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 2.5f, -4f);
    public Vector3 lookAtOffset = new Vector3(0f, 0.5f, 0f);
    public int width = 1280;
    public int height = 720;
    
    private Camera _cam;
    private RenderTexture _rt;
    private Transform _target;

    void Start()
    {
        var agent = GameObject.FindGameObjectWithTag("agent");
        if (agent != null)
            _target = agent.transform;

        _cam = gameObject.AddComponent<Camera>();
        _cam.enabled = false;
        _cam.fieldOfView = 60f;
        _cam.nearClipPlane = 0.1f;
        _cam.farClipPlane = 100f;

        _rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        _rt.Create();
        _cam.targetTexture = _rt;
    }

    void LateUpdate()
    {
        if (_target == null) return;
        transform.position = _target.position + _target.rotation * offset;
        transform.LookAt(_target.position + lookAtOffset);
    }

    public Texture2D CaptureFrame()
    {
        if (_cam == null || _rt == null) return null;
        _cam.Render();
        RenderTexture.active = _rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        return tex;
    }

    public byte[] CaptureJPG(int quality = 85)
    {
        var tex = CaptureFrame();
        if (tex == null) return null;
        var bytes = tex.EncodeToJPG(quality);
        Destroy(tex);
        return bytes;
    }
}
