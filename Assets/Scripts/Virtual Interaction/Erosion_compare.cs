using System;
// using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using OpenCVForUnity;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;

/// <summary>
/// Listens for touch events and performs an AR raycast from the screen touch point.
/// AR raycasts will only hit detected trackables like feature points and planes.
///
/// If a raycast hits a trackable, the <see cref="placedPrefab"/> is instantiated
/// and moved to the hit position.
/// </summary>
// [RequireComponent(typeof(ARCameraManager))]
// [RequireComponent(typeof(RawImage))]
public class Erosion_compare : MonoBehaviour
{
    public Mat imageMat = new Mat(480, 640, CvType.CV_8UC1);
    public Mat threshMat = new Mat(480, 640, CvType.CV_8UC1);
    public Mat erodeMat = new Mat(480, 640, CvType.CV_8UC1);
    public Mat dilMat = new Mat(480, 640, CvType.CV_8UC1);
    public Mat outMat = new Mat(480, 640, CvType.CV_8UC1);

    public double THRESH_VAL = 170.0;
    private Mat struct_elt = new Mat (3, 3, CvType.CV_8UC1);

    public Texture2D m_Texture;
    public Texture2D e_Texture;

    private ScreenOrientation? m_CachedOrientation = null;

    [SerializeField]
    ARCameraManager m_ARCameraManager;
    public ARCameraManager cameraManager
    {
        get {return m_ARCameraManager; }
        set {m_ARCameraManager = value; }
    }

    [SerializeField]
    RawImage m_RawImage;
    public RawImage rawImage 
    {
        get { return m_RawImage; }
        set { m_RawImage = value; }
    }

    [SerializeField]
    RawImage e_RawImage;
    public RawImage erodeImage 
    {
        get { return e_RawImage; }
        set { e_RawImage = value; }
    }

    [SerializeField]
    Text m_ImageInfo;
    public Text imageInfo
    {
        get { return m_ImageInfo; }
        set { m_ImageInfo = value; }
    }

    void Awake()
    {
        Debug.Log("StartTest");
    }

    void OnEnable()
    {
        if (m_ARCameraManager != null)
            m_ARCameraManager.frameReceived += OnCameraFrameReceived;
    }

    void OnDisable()
    {
        if (m_ARCameraManager != null)
            m_ARCameraManager.frameReceived -= OnCameraFrameReceived;
    }


    void ComputerVisionAlgo(IntPtr greyscale) 
    {
        Utils.copyToMat(greyscale, imageMat);

        Imgproc.threshold(imageMat, threshMat, THRESH_VAL, 255.0, Imgproc.THRESH_BINARY_INV);
        
        struct_elt = Imgproc.getStructuringElement(Imgproc.MORPH_ELLIPSE, new Size(8, 8));
        Imgproc.erode(threshMat, erodeMat, struct_elt);
        Imgproc.dilate(erodeMat, dilMat, struct_elt);
        
        erodeMat = dilMat;
        outMat = threshMat;
    }

    // void ConfigureRawImageInSpace(Vector2 img_dim)
    // {
    //     Vector2 ScreenDimension = new Vector2(Screen.width, Screen.height);
    //     int scr_w = Screen.width;
    //     int scr_h = Screen.height; 

    //     float img_w = img_dim.x;
    //     float img_h = img_dim.y;

    //     float w_ratio = (float)scr_w/img_w;
    //     float h_ratio = (float)scr_h/img_h;
    //     float scale = Math.Max(w_ratio, h_ratio);

    //     Debug.LogFormat("Screen Dimensions: {0} x {1}\n Image Dimensions: {2} x {3}\n Ratios: {4}, {5}", 
    //         scr_w, scr_h, img_w, img_h, w_ratio, h_ratio);
    //     Debug.LogFormat("RawImage Rect: {0}", m_RawImage.uvRect);

    //     m_RawImage.SetNativeSize();
    //     m_RawImage.transform.position = new Vector3(scr_w/2, scr_h/2, 0.0f);
    //     m_RawImage.transform.localScale = new Vector3(scale, scale, 0.0f);
    // }

    void ConfigureRawImageInSpace(Vector2 img_dim)
    {
        Vector2 ScreenDimension = new Vector2(Screen.width, Screen.height);
        int scr_w = Screen.width;
        int scr_h = Screen.height; 

        float img_w = img_dim.x;
        float img_h = img_dim.y;

        float w_ratio = (float)scr_w/img_w;
        float h_ratio = (float)scr_h/img_h;
        float scale = Math.Max(w_ratio, h_ratio);

        Debug.LogFormat("Screen Dimensions: {0} x {1}\n Image Dimensions: {2} x {3}\n Ratios: {4}, {5}", 
            scr_w, scr_h, img_w, img_h, w_ratio, h_ratio);
        Debug.LogFormat("RawImage Rect: {0}", m_RawImage.uvRect);

        m_RawImage.SetNativeSize();
        m_RawImage.transform.position = new Vector3(scr_w/2, scr_h/4, 0.0f);
        m_RawImage.transform.localScale = new Vector3(scale/2, scale/2, 0.0f);

        e_RawImage.SetNativeSize();
        e_RawImage.transform.position = new Vector3(scr_w/2, 3*scr_h/4, 0.0f);
        e_RawImage.transform.localScale = new Vector3(scale/2, scale/2, 0.0f);
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // CAMERA IMAGE HANDLING
        XRCameraImage image;
        if (!cameraManager.TryGetLatestImage(out image))
        {
            Debug.Log("Uh Oh");
            return;
        }

        Vector2 img_dim = image.dimensions;
        
        XRCameraImagePlane greyscale = image.GetPlane(0);

        if (m_Texture == null || m_Texture.width != image.width)
        {
            var format = TextureFormat.RGBA32;
            m_Texture = new Texture2D(image.width, image.height, format, false);
            e_Texture = new Texture2D(image.width, image.height, format, false);
        }

        image.Dispose();

        // Process the image here: 
        unsafe {
            IntPtr greyPtr = (IntPtr) greyscale.data.GetUnsafePtr();
            ComputerVisionAlgo(greyPtr);
            Utils.matToTexture2D(outMat, m_Texture, true, 0);
            Utils.matToTexture2D(erodeMat, e_Texture, false, 0);
        }

        Debug.Log(m_CachedOrientation);
        if (m_CachedOrientation == null || m_CachedOrientation != Screen.orientation)
        {
            // TODO: Debug why doesn't initiate with ConfigRawimage(). The null isn't triggering here. Print cached Orientation
            m_CachedOrientation = Screen.orientation;
            ConfigureRawImageInSpace(img_dim);
        }

        m_RawImage.texture = (Texture) m_Texture;
        e_RawImage.texture = (Texture) e_Texture;

        // double[] c_data = circMat.get(0, 0);
        // m_ImageInfo.text = string.Format("Circle Count: {0}\n Circle[0]: {1} x {2} -- {3}", 
        // circMat.size().width, c_data[0], c_data[1], c_data[2]);
    }
}
