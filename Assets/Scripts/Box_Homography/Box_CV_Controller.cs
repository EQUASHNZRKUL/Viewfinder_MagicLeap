using System;
using System.Collections.Generic;
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
using OpenCVForUnity.Features2dModule;
using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.Xfeatures2dModule;

/// <summary>
/// Listens for touch events and performs an AR raycast from the screen touch point.
/// AR raycasts will only hit detected trackables like feature points and planes.
///
/// If a raycast hits a trackable, the <see cref="placedPrefab"/> is instantiated
/// and moved to the hit position.
/// </summary>
public class Box_CV_Controller : MonoBehaviour
{
    public static double THRESH_VAL = 150.0;
    public static int K_ITERATIONS = 10;
    public static double HOMOGRAPHY_WIDTH = 640.0;
    public static double HOMOGRAPHY_HEIGHT = 480.0;

    public Mat imageMat = new Mat(480, 640, CvType.CV_8UC1);
    public Mat outMat = new Mat(480, 640, CvType.CV_8UC1);

    private Mat cached_initMat = new Mat (480, 640, CvType.CV_8UC1);
    private Mat cached_homoMat1 = new Mat (480, 640, CvType.CV_8UC1);
    private Mat cached_homoMat2 = new Mat (480, 640, CvType.CV_8UC1);
    private Mat cached_homoMat3 = new Mat (480, 640, CvType.CV_8UC1);

    private MatOfKeyPoint keyMat = new MatOfKeyPoint();
    private Point[] srcPointArray = new Point[7];
    private Point[] regPointArray = new Point[4];
    private Point[] dstPointArray = new Point[4];
    private Point[] face1Array = new Point[4];
    private Point[] face2Array = new Point[4];
    private Point[] face3Array = new Point[4];

    private ScreenOrientation? m_CachedOrientation = null;
    private Texture2D m_Texture;

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
    RawImage m_TopImage1; 
    public RawImage topImage1
    {
        get { return m_TopImage1; }
        set { m_TopImage1 = value; }
    }

    [SerializeField]
    RawImage m_TopImage2; 
    public RawImage topImage2
    {
        get { return m_TopImage2; }
        set { m_TopImage2 = value; }
    }

    [SerializeField]
    RawImage m_TopImage3; 
    public RawImage topImage3
    {
        get { return m_TopImage3; }
        set { m_TopImage3 = value; }
    }

    [SerializeField]
    Text m_ImageInfo;
    public Text imageInfo
    {
        get { return m_ImageInfo; }
        set { m_ImageInfo = value; }
    }

    [SerializeField]
    ARSessionOrigin m_ARSessionManager;
    public ARSessionOrigin sessionManager
    {
        get { return m_ARSessionManager; }
        set { m_ARSessionManager = value; }
    }

    void Awake()
    {
        Debug.Log("StartTest");
        Screen.autorotateToLandscapeLeft = true; 
        m_ARRaycastManager = GetComponent<ARRaycastManager>();
    }

    void OnEnable()
    {
        if (m_ARCameraManager != null)
        {
            m_ARCameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    void OnDisable()
    {
        if (m_ARCameraManager != null)
            m_ARCameraManager.frameReceived -= OnCameraFrameReceived;
    }

    float CameraToPixelX(double x) { return (float) (3.4375 * x); }

    float CameraToPixelY(double y){ return (float) (1080.0 - (3.375*(y - 80.0))); }

    // Returns scrPointArray for public access
    public Point[] GetC1Points()
    {
        Debug.LogFormat("CV: {0}", srcPointArray[0]);
        return srcPointArray;
    }

    void swap_src(int i, int j)
    {
        Point tmp = srcPointArray[i];
        srcPointArray[i] = srcPointArray[j];
        srcPointArray[j] = tmp; 
    }

    // Lazy Box point sorting (hardcoded)
    void SortBox() {
        Debug.Log("SB - 243");
        // Find mean point
        double x_mean = 0;
        double y_mean = 0; 
        for (int i = 0; i < 7; i++)
        {
            x_mean = x_mean + srcPointArray[i].x;
            y_mean = y_mean + srcPointArray[i].y;
        }
        x_mean = x_mean / 7;
        y_mean = y_mean / 7;
        
        Debug.Log("SB - 255");
        // Find centroid
        {
            double min_dist = Math.Pow((srcPointArray[6].x - x_mean),2) + Math.Pow((srcPointArray[6].y - y_mean), 2);
            int min_i = 6;
            for (int i = 0; i < 6; i++)
            {
                double dist = Math.Pow((srcPointArray[i].x - x_mean),2) + Math.Pow((srcPointArray[i].y - y_mean), 2);
                if (dist < min_dist)
                {
                    min_dist = dist;
                    min_i = i;
                }
            }

            // Swapping centroid to srcPointArray[6];
            swap_src(min_i, 6);
            Point centroid = srcPointArray[6];

            // Inserting centroid into face arrays
            face1Array[1] = centroid; 
            face2Array[2] = centroid; 
            face3Array[0] = centroid; 
        }

        Debug.LogFormat("Unsorted Source Points: {0} \n {1} \n {2} \n {3} \n {4} \n {5} \n {6}", 
            srcPointArray[0], srcPointArray[1], srcPointArray[2], srcPointArray[3], 
            srcPointArray[4], srcPointArray[5], srcPointArray[6]);

        // Getting the Facial points:
        // FACE 1: (min y and 2 min x)
        {
            int min_j = 5;
            for (int i = 0; i < 5; i++)
            {
                if (srcPointArray[i].y < srcPointArray[min_j].y)
                {
                    min_j = i; 
                }
            }
            face1Array[3] = srcPointArray[min_j];
            face3Array[2] = srcPointArray[min_j];
            Debug.LogFormat("Point (5): {0}", srcPointArray[min_j]);
            swap_src(min_j, 5);

            int min_x = 4; 
            for (int i = 0; i < 4; i++) {
                if (srcPointArray[i].x < srcPointArray[min_x].x) {
                    min_x = i; 
                }
            }
            Debug.LogFormat("Point (4): {0}", srcPointArray[min_x]);
            swap_src(min_x, 4);

            int min2_x = 3;
            for (int i = 0; i < 3; i++) {
                if (srcPointArray[i].x < srcPointArray[min2_x].x) {
                    min2_x = i;
                }
            }
            Debug.LogFormat("Point (3): {0}", srcPointArray[min2_x]);
            swap_src(min2_x, 3);

            if (srcPointArray[4].y > srcPointArray[3].y)
            {
                swap_src(3, 4);
            }
            face1Array[0] = srcPointArray[3];
            face1Array[2] = srcPointArray[4];
        }

        // FACE 3: (2 max x)
        {
            int max_x = 2; 
            for (int i = 0; i < 2; i++)
            {
                if (srcPointArray[i].x > srcPointArray[max_x].x)
                {
                    max_x = i; 
                }
            }
            Debug.LogFormat("Point (2): {0}", srcPointArray[max_x]);
            swap_src(max_x, 2);

            if (srcPointArray[0].x > srcPointArray[1].x)
                swap_src(0, 1);
            Debug.LogFormat("Point (1): {0}", srcPointArray[1]);

            if (srcPointArray[1].y < srcPointArray[2].y)
                swap_src(1, 2);
            Debug.LogFormat("Point (0): {0}", srcPointArray[0]);
            
            face3Array[1] = srcPointArray[1];
            face3Array[3] = srcPointArray[2];
        }

        // src[0] test: 
        int max_y = 6;
        for (int i = 0; i < 6; i++)
        {
            if (srcPointArray[i].y > srcPointArray[max_y].y)
            {
                max_y = i; 
            }
        }
        Debug.LogFormat("Box Function broken: {0}", (max_y != 0));

        // FACE 2: 
        {
            face2Array[0] = srcPointArray[3];
            face2Array[1] = srcPointArray[0];
            face2Array[2] = srcPointArray[6];
            face2Array[3] = srcPointArray[1];
        }

        Debug.LogFormat("Sorted Source Points: {0} \n {1} \n {2} \n {3} \n {4} \n {5} \n {6}", 
            srcPointArray[0], srcPointArray[1], srcPointArray[2], srcPointArray[3], 
            srcPointArray[4], srcPointArray[5], srcPointArray[6]);

        Debug.LogFormat("FACE 1 Points; {0} \n {1} \n {2} \n {3}", 
            face1Array[0], face1Array[1], face1Array[2], face1Array[3]);

        Debug.LogFormat("FACE 2 Points; {0} \n {1} \n {2} \n {3}", 
            face2Array[0], face2Array[1], face2Array[2], face2Array[3]);

        Debug.LogFormat("FACE 3 Points; {0} \n {1} \n {2} \n {3}", 
            face3Array[0], face3Array[1], face3Array[2], face3Array[3]);
    }

    // Detects Blobs with Detector Framework and stores Top-down view into cached_homoMat
    void BlobDetection() {
        SimpleBlobDetector detector = SimpleBlobDetector.create();
        Core.flip(cached_initMat, imageMat, 0);
        keyMat = new MatOfKeyPoint();
        detector.detect(imageMat, keyMat);

        Features2d.drawKeypoints(imageMat, keyMat, outMat);

        if (keyMat.rows() < 7) 
            return; 

        for (int i = 0; i < 7; i++)
        {
            srcPointArray[i] = new Point(keyMat.get(i, 0)[0], keyMat.get(i, 0)[1]);
        }
        
        SortBox();
    }

    void Rectify(ref Point[] facePointArray, ref Mat cachedMat) {
        regPointArray[0] = new Point(0.0, HOMOGRAPHY_HEIGHT);
        regPointArray[1] = new Point(HOMOGRAPHY_WIDTH, HOMOGRAPHY_HEIGHT);
        regPointArray[2] = new Point(0.0, 0.0);
        regPointArray[3] = new Point(HOMOGRAPHY_WIDTH, 0.0);

        MatOfPoint2f srcPoints = new MatOfPoint2f(facePointArray);
        MatOfPoint2f regPoints = new MatOfPoint2f(regPointArray);

        Debug.LogFormat("Rectify Face Points; {0} \n {1} \n {2} \n {3}", 
            facePointArray[0], facePointArray[1], facePointArray[2], facePointArray[3]);

        // Creating the H Matrix
        Mat Homo_Mat = Calib3d.findHomography(srcPoints, regPoints);

        Imgproc.warpPerspective(imageMat, cachedMat, Homo_Mat, new Size(HOMOGRAPHY_WIDTH, HOMOGRAPHY_HEIGHT));
    }

    // Warps cached_homoMat to outMat
    void HomographyTransform(ref Mat homoMat) 
    {
        Corner_AR_Controller Homo_Controller = m_ARSessionManager.GetComponent<Corner_AR_Controller>();
        Point[] c2_scrpoints = Homo_Controller.GetScreenpoints(false);

        MatOfPoint2f initPoints = new MatOfPoint2f(regPointArray);
        MatOfPoint2f currPoints = new MatOfPoint2f(c2_scrpoints);

        Mat H = Calib3d.findHomography(initPoints, currPoints);

        Imgproc.warpPerspective(homoMat, outMat, H, new Size(HOMOGRAPHY_WIDTH, HOMOGRAPHY_HEIGHT));
        Core.flip(outMat, outMat, 0);
    }

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
        m_RawImage.transform.position = new Vector3(scr_w/4, scr_h/4, 0.0f);
        m_RawImage.transform.localScale = new Vector3(scale/4, scale/4, 0.0f);
        // m_RawImage.transform.position = new Vector3(scr_w/2, scr_h/2, 0.0f);
        // m_RawImage.transform.localScale = new Vector3(scale, scale, 0.0f);

        m_TopImage1.SetNativeSize();
        m_TopImage1.transform.position = new Vector3(5*scr_w/6, scr_h/6, 0.0f);
        m_TopImage1.transform.localScale = new Vector3(scale/6, scale/6, 0.0f);

        m_TopImage2.SetNativeSize();
        m_TopImage2.transform.position = new Vector3(5*scr_w/6, scr_h/2, 0.0f);
        m_TopImage2.transform.localScale = new Vector3(scale/6, scale/6, 0.0f);

        m_TopImage3.SetNativeSize();
        m_TopImage3.transform.position = new Vector3(5*scr_w/6, 5*scr_h/6, 0.0f);
        m_TopImage3.transform.localScale = new Vector3(scale/6, scale/6, 0.0f);
    }

    void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        // Camera data extraction
        XRCameraImage image;
        if (!cameraManager.TryGetLatestImage(out image))
        {
            Debug.Log("Uh Oh");
            return;
        }

        Vector2 img_dim = image.dimensions;
        XRCameraImagePlane greyscale = image.GetPlane(0);

        // Instantiates new m_Texture if necessary
        if (m_Texture == null || m_Texture.width != image.width)
        {
            var format = TextureFormat.RGBA32;
            m_Texture = new Texture2D(image.width, image.height, format, false);
        }

        image.Dispose();

        // Process the image here: 
        unsafe {
            IntPtr greyPtr = (IntPtr) greyscale.data.GetUnsafePtr();

            // TOUCH: Detect corners and set as source points
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    // Cache original image
                    Utils.copyToMat(greyPtr, cached_initMat);

                    // Detect reference points
                    BlobDetection();

                    Rectify(ref face1Array, ref cached_homoMat1);
                    Rectify(ref face2Array, ref cached_homoMat2);
                    Rectify(ref face3Array, ref cached_homoMat3);

                    // Display cached top-down
                    Texture2D topTexture1 = new Texture2D((int) img_dim.x, (int) img_dim.y, TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(cached_homoMat1, topTexture1, false, 0);
                    m_TopImage1.texture = (Texture) topTexture1;

                    Texture2D topTexture2 = new Texture2D((int) img_dim.x, (int) img_dim.y, TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(cached_homoMat2, topTexture2, false, 0);
                    m_TopImage2.texture = (Texture) topTexture2;

                    Texture2D topTexture3 = new Texture2D((int) img_dim.x, (int) img_dim.y, TextureFormat.RGBA32, false);
                    Utils.matToTexture2D(cached_homoMat3, topTexture3, false, 0);
                    m_TopImage3.texture = (Texture) topTexture3;

                    Debug.Log("OCFR: 510");
                }
            }
            
            // Warps cached top-down and gets outMat. 
            // HomographyTransform(ref cached_homoMat);

            // Displays OpenCV Mat as a Texture
            Utils.matToTexture2D(outMat, m_Texture, false, 0);
        }

        // Sets orientation of screen if necessary
        if (m_CachedOrientation == null || m_CachedOrientation != Screen.orientation)
        {
            // TODO: Debug why doesn't initiate with ConfigRawimage(). The null isn't triggering here. Print cached Orientation
            m_CachedOrientation = Screen.orientation;
            ConfigureRawImageInSpace(img_dim);
        }

        // Debug.Log("OCFR: 529");

        m_RawImage.texture = (Texture) m_Texture;

        m_ImageInfo.text = string.Format("Number of Blobs: {0}", keyMat.rows());
    }

    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    ARRaycastManager m_ARRaycastManager;

    ARSessionOrigin m_SessionOrigin;
}