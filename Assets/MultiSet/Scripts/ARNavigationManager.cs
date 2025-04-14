using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using System.Collections;
using Unity.Collections;
using Unity.XR.CoreUtils;
using System;
using UnityEngine.UI;
using System.Collections.Generic;

public class ARNavigationManager : MonoBehaviour
{
    [Header("AR Setup")]
    public XROrigin sessionOrigin;
    public ARCameraManager arCameraManager;
    public ARSession arSession;

    [Header("Location Targets")]
    public List<Target> targetList = new List<Target>();

    [Header("UI Controls")]
    public Button scanButton;
    public ToastManager toastManager;

    private Texture2D cameraTexture;
    private BarcodeReader barcodeReader;
    private bool isScanning = false;
    private bool scanningEnabled = false;

    // Webcam for editor simulation
    private WebCamTexture webCamTexture;
    private float scanInterval = 0.5f; // Scan every 0.5 seconds in editor
    private float lastScanTime;

    public static ARNavigationManager Instance;

    [System.Serializable]
    public class Target
    {
        public string locationName;
        public Transform targetTransform;
    }

    void Awake()
    {
        Instance = this;
        InitializeBarcodeReader();
    }

    void Start()
    {
        arCameraManager.frameReceived += OnCameraFrameReceived;
        scanButton.onClick.AddListener(StartScanning);
    }

    void Update()
    {
#if UNITY_EDITOR
        if (scanningEnabled && !isScanning && Time.time - lastScanTime > scanInterval)
        {
            lastScanTime = Time.time;
            StartCoroutine(ScanQRCode());
        }
#endif
    }

    void InitializeBarcodeReader()
    {
        barcodeReader = new BarcodeReader
        {
            AutoRotate = true,
            Options = { TryHarder = true }
        };
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
#if !UNITY_EDITOR
        if (scanningEnabled && !isScanning)
        {
            StartCoroutine(ScanQRCode());
        }
#endif
    }

    public void StartScanning()
    {
        if (!scanningEnabled)
        {
            scanningEnabled = true;
            ShowToast("Scanning started");
#if UNITY_EDITOR
            if (webCamTexture == null)
            {
                webCamTexture = new WebCamTexture();
            }
            webCamTexture.Play();
#endif
        }
    }

    IEnumerator ScanQRCode()
    {
        isScanning = true;

#if UNITY_EDITOR
        // Use webcam in Unity Editor
        if (webCamTexture == null || !webCamTexture.isPlaying)
        {
            isScanning = false;
            yield break;
        }
        yield return new WaitForEndOfFrame();
        cameraTexture = new Texture2D(webCamTexture.width, webCamTexture.height, TextureFormat.RGBA32, false);
        Color32[] pixels = webCamTexture.GetPixels32();
        cameraTexture.SetPixels32(pixels);
        cameraTexture.Apply();
#else
        // Use AR camera on mobile devices
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            isScanning = false;
            yield break;
        }
        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.MirrorY
        };
        int bufferSize = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(bufferSize, Allocator.Temp);
        image.Convert(conversionParams, buffer);
        image.Dispose();
        cameraTexture = new Texture2D(
            conversionParams.outputDimensions.x,
            conversionParams.outputDimensions.y,
            conversionParams.outputFormat,
            false);
        cameraTexture.LoadRawTextureData(buffer);
        cameraTexture.Apply();
        buffer.Dispose();
#endif

        // Decode the QR code (common to both paths)
        var result = barcodeReader.Decode(cameraTexture.GetPixels32(), cameraTexture.width, cameraTexture.height);
        if (result != null)
        {
            string locationName = result.Text;
            ProcessQRData(locationName);
            scanningEnabled = false;
#if UNITY_EDITOR
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
            }
#endif
            ShowToast("Scanning completed");
        }
        else
        {
            ShowToast("No QR code detected");
        }

        yield return new WaitForSeconds(0.5f);
        isScanning = false;

        if (!scanningEnabled) ShowToast("Scanning stopped");
    }

    void ProcessQRData(string locationName)
    {
        Transform targetTransform = GetTargetTransform(locationName);
        if (targetTransform != null)
        {
            RecenterUser(targetTransform.position);
            ShowToast($"Located: {locationName}");
        }
        else
        {
            ShowToast("Location not found");
        }
    }

    Transform GetTargetTransform(string locationName)
    {
        foreach (Target target in targetList)
        {
            if (target.locationName == locationName)
            {
                return target.targetTransform;
            }
        }
        return null;
    }

    void RecenterUser(Vector3 targetPosition)
    {
        if (sessionOrigin == null || sessionOrigin.Camera == null)
        {
            Debug.LogError("sessionOrigin or sessionOrigin.Camera is null");
            return;
        }

        Vector3 cameraWorldPosition = sessionOrigin.Camera.transform.position;
        Vector3 positionDifference = cameraWorldPosition - targetPosition;
        sessionOrigin.transform.position -= positionDifference;
        sessionOrigin.transform.rotation = Quaternion.identity;
    }

    void ShowToast(string message)
    {
        if (toastManager != null)
        {
            toastManager.ShowToast(message);
        }
        else
        {
            Debug.Log("Toast: " + message);
        }
    }
}