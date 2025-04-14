# AR Indoor Navigation App

Welcome to the **AR Indoor Navigation App**, an augmented reality (AR) solution designed to simplify indoor navigation in complex buildings, such as college campuses. By leveraging QR codes for precise positioning, Unity’s NavMesh for pathfinding, and ARFoundation for real-time AR path overlays, this app guides users seamlessly across single or multiple floors with visual and audio cues.

This repository contains the source code and setup instructions for developers to build, test, and extend the app.

---

## **Project Overview**

- **Purpose**: Address the challenge of indoor navigation where GPS fails, offering an intuitive AR-based alternative to traditional maps and signage.
- **Features**:
  - QR code scanning for accurate user positioning.
  - Real-time AR navigation paths displayed on a mobile device.
  - Support for multi-floor navigation with transitions (e.g., stairs, elevators).
  - Hands-free audio instructions for accessibility.
- **Use Case**: Ideal for large indoor environments like universities, offices, or hospitals.

---

## **Prerequisites**

Before setting up the project, ensure you have the following:

- **Unity Hub** and **Unity Editor** (version 2022.3 LTS recommended).
- An **AR-capable mobile device** (Android 7.0+ with ARCore or iOS with ARKit).
- A **webcam** (optional, for Editor testing).
- Basic familiarity with Unity and C# scripting.

---

## **Setup Instructions**

Follow these steps to configure the development environment for the AR Indoor Navigation App.

### **Step 1: Install Unity**
1. Download and install **Unity Hub** from [unity.com](https://unity.com).
2. In Unity Hub, go to **Installs** > **Add**, and select **Unity 2022.3 LTS**.
3. Include modules for:
   - **Android Build Support** (for mobile deployment).
   - **Windows Build Support (IL2CPP)** (for Editor testing).
4. Complete the installation (approximately 2-5 GB).

### **Step 2: Clone the Repository**
1. Clone this repository to your local machine:
   ```bash
   git clone https://github.com/your-username/ar-indoor-navigation.git
   ```
2. Open the project folder in Unity Hub via **Projects** > **Add**.

### **Step 3: Install Required Packages**
1. Open the project in Unity Editor.
2. Navigate to **Window** > **Package Manager**.
3. Install the following packages:
   - **ARFoundation** (version 5.1.x recommended).
   - **ARCore XR Plugin** (for Android).
   - **ARKit XR Plugin** (for iOS).
4. Import **ZXing.Unity**:
   - Download the latest ZXing.Unity package from [GitHub](https://github.com/glassesfactory/ZXing.Unity) or a trusted source.
   - Import via **Assets** > **Import Package** > **Custom Package**.
5. **NavMesh**: No additional installation needed (built into Unity).

### **Step 4: Configure Project Settings**
1. Switch to the target platform:
   - Go to **File** > **Build Settings**.
   - Select **Android** (or iOS) and click **Switch Platform**.
2. Update Player Settings:
   - In **Build Settings**, click **Player Settings**.
   - Set **Company Name** (e.g., “ARNav”) and **Product Name** (e.g., “IndoorNav”).
   - Under **Other Settings**:
     - **Minimum API Level**: Android 7.0 (API 24) or higher.
     - **Scripting Backend**: IL2CPP.
     - **Target Architectures**: ARMv7 and ARM64.
3. Enable AR:
   - Go to **Edit** > **Project Settings** > **XR Plug-in Management**.
   - Check **ARCore** (Android) or **ARKit** (iOS).

### **Step 5: Set Up the Unity Scene**
1. Open the main scene (e.g., `Assets/Scenes/MainScene.unity`) or create a new one:
   - Right-click **Project** > **Create** > **Scene**.
2. Add AR components:
   - Hierarchy > **XR** > **AR Session**.
   - Hierarchy > **XR** > **AR Session Origin** (rename to `XROrigin`).
   - Add an **AR Camera** under `XROrigin` with an **AR Camera Manager** component.
3. Create a navigable floor:
   - Hierarchy > **3D Object** > **Plane** (scale to 10x1x10).
   - Mark as **Navigation Static** (Inspector > Navigation tab).
   - Go to **Window** > **AI** > **Navigation** > **Bake** to generate NavMesh.
4. Set up QR targets:
   - Create empty GameObjects (e.g., `Room101`) at specific positions (e.g., x=2, y=0, z=2).
   - Configure in the `ARNavigationManager` script (see below).

---

## **Execution Instructions**

### **Step 1: Prepare QR Codes**
1. Generate QR codes matching target names in `ARNavigationManager` (e.g., “Room101”, “Library”).
   - Use an online tool like [qr-code-generator.com](https://www.qr-code-generator.com).
2. Save and print 2-3 QR codes for testing.
3. Place them in your demo environment or on a mock layout.

### **Step 2: Configure the ARNavigationManager Script**
1. Ensure the `ARNavigationManager.cs` script is in your project (under `Assets/Scripts`).
   - Example implementation (simplified):
     ```csharp
     using UnityEngine;
     using UnityEngine.XR.ARFoundation;
     using UnityEngine.XR.ARSubsystems;
     using ZXing;
     using System.Collections;
     using Unity.Collections;
     using UnityEngine.UI;

     public class ARNavigationManager : MonoBehaviour
     {
         public XROrigin sessionOrigin;
         public ARCameraManager arCameraManager;
         public Button scanButton;
         private Texture2D cameraTexture;
         private BarcodeReader barcodeReader = new BarcodeReader();
         private bool scanningEnabled = false;
         private bool isScanning = false;

         [System.Serializable]
         public class Target { public string locationName; public Transform targetTransform; }
         public Target[] targetList;

         void Start()
         {
             scanButton.onClick.AddListener(() => scanningEnabled = true);
             arCameraManager.frameReceived += args => { if (scanningEnabled && !isScanning) StartCoroutine(ScanQRCode()); };
         }

         IEnumerator ScanQRCode()
         {
             isScanning = true;
             if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
             {
                 var conversionParams = new XRCpuImage.ConversionParams
                 {
                     inputRect = new RectInt(0, 0, image.width, image.height),
                     outputDimensions = new Vector2Int(image.width / 2, image.height / 2),
                     outputFormat = TextureFormat.RGBA32,
                     transformation = XRCpuImage.Transformation.MirrorY
                 };
                 var buffer = new NativeArray<byte>(image.GetConvertedDataSize(conversionParams), Allocator.Temp);
                 image.Convert(conversionParams, buffer);
                 image.Dispose();
                 cameraTexture = new Texture2D(conversionParams.outputDimensions.x, conversionParams.outputDimensions.y, conversionParams.outputFormat, false);
                 cameraTexture.LoadRawTextureData(buffer);
                 cameraTexture.Apply();
                 buffer.Dispose();

                 var result = barcodeReader.Decode(cameraTexture.GetPixels32(), cameraTexture.width, cameraTexture.height);
                 if (result != null)
                 {
                     foreach (var target in targetList)
                     {
                         if (target.locationName == result.Text)
                         {
                             sessionOrigin.transform.position = target.targetTransform.position;
                             Debug.Log($"Located: {result.Text}");
                             scanningEnabled = false;
                             break;
                         }
                     }
                 }
             }
             isScanning = false;
             yield return null;
         }
     }
     ```
2. Attach the script to a GameObject (e.g., “Manager”).
3. In the Inspector, assign:
   - `sessionOrigin`: The XROrigin GameObject.
   - `arCameraManager`: The AR Camera’s ARCameraManager component.
   - `scanButton`: A UI Button for triggering scans.
   - `targetList`: Add entries (e.g., “Room101” with its Transform).

### **Step 3: Test in Unity Editor**
1. Add a UI Button to the scene (Hierarchy > **UI** > **Button**).
2. Press **Play** in Unity Editor.
3. Simulate QR scanning (optional: add debug input, e.g., `if (Input.GetKeyDown(KeyCode.Space)) scanningEnabled = true;`).
4. Verify the session origin moves to the target position.

### **Step 4: Build and Deploy to Mobile**
1. Connect an AR-capable device (Android/iOS) via USB.
2. Go to **File** > **Build Settings**:
   - Add the current scene.
   - Select **Android** (or iOS).
   - Click **Build and Run**.
3. Save the APK (e.g., `IndoorNav.apk`) and wait for deployment (5-10 minutes).
4. On the device, grant camera permissions when prompted.

### **Step 5: Run the Demo**
1. Open the app on your mobile device.
2. Tap the scan button.
3. Scan a printed QR code (e.g., “Room101”).
4. Verify the AR session aligns with the target position (extend with pathfinding for navigation).

---

## **Troubleshooting**
- **QR Code Not Detected**: Ensure ZXing is properly imported and camera permissions are enabled in Player Settings.
- **AR Not Working**: Check ARCore/ARKit plugins in XR Plug-in Management and device compatibility.
- **NavMesh Issues**: Re-bake the NavMesh if paths don’t generate correctly.
- **Build Errors**: Verify platform settings and resolve missing dependencies in Package Manager.

---

## **Future Enhancements**
- Add multi-floor support with NavMeshLinks for stairs/elevators.
- Integrate audio cues for hands-free navigation.
- Expand QR code coverage for larger environments.
- Optimize performance for low-end devices.

---

## **Contributing**
Contributions are welcome! Please submit issues or pull requests for bug fixes, optimizations, or new features.

---
