// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2019 Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Creator Agreement, located
// here: https://id.magicleap.com/creator-terms
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%
using System; 
using System.Collections; 
using System.Collections.Generic; 

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.XR.MagicLeap;

using OpenCVForUnity;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ArucoModule;

namespace MagicLeap
{
    public class CVController : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("Object to set new images on.")]
        private GameObject _previewObject = null;

        [System.Serializable]
        private class RaycastTriggerEvent : UnityEvent<Vector3, int>
        // private class RaycastTriggerEvent : UnityEvent<Vector3>
        {}

        [SerializeField, Space]
        private RaycastTriggerEvent OnArucoRayFound = null;

        // Constants
        private static int FACE_COUNT = 7; 

        // Mats
        public Mat outMat = new Mat(1080, 1920, CvType.CV_8UC1);
        private Mat cached_initMat = new Mat (1080, 1920, CvType.CV_8UC1);
        private Mat ids = new Mat(1080, 1920, CvType.CV_8UC1);
        private List<Mat> corners = new List<Mat>();

        // Face corner indices for each face
        private int[,] face_index = { {3, 6, 4, 5}, {0, 1, 3, 6}, {6, 1, 5, 2} };

        // Point Lists
        private Vector3[] src_ray_array = new Vector3[FACE_COUNT];
        private Vector3[] src_world_array = new Vector3[FACE_COUNT];

        // Face Lists
        private bool[] faceX_full = new bool[3];

        #endregion

        #region Helper Functions
        int count_src_nulls() {
            int acc = 0;
            for (int i = 0; i < 7; i++)
            {
                if (src_world_array[i] == null) {
                    acc++; 
                }
            }
            return (7 - acc); 
        }

        bool check_faces(int face_i) {
            for (int i = 0; i < 4; i++) {
                int src_i = face_index[face_i, i]; 
                if (src_world_array[src_i] == null) {
                    return false; 
                }
            }
            return true; 
        }
        
        int arucoTosrc(int a) {
            if (a == 7) { return 4; }
            else if (a == 6) { return 5; }
            else if (a == 10) { return 6; }
            else {return a; }
        }

        int srcToarcuo(int s) {
            if (s == 4) { return 7; }
            else if (s == 5) {return 6; }
            else if (s == 6) {return 10; }
            else {return s; }
        }
        #endregion

        #region Unity Functions
        private void Awake() {
            Debug.Log("equash: awake");
        }
        #endregion

        void ArucoDetection() {
            // Detect ArUco markers
            Dictionary dict = Aruco.getPredefinedDictionary(Aruco.DICT_4X4_1000);
            Aruco.detectMarkers(cached_initMat, dict, corners, ids);
            Aruco.drawDetectedMarkers(cached_initMat, corners, ids);
            Debug.Log("AD - 93: Markers Detected");
            Debug.LogFormat("Corners: {0}", corners.Count);

            // Get desired corner of marker
            Point[] src_point_array = new Point[FACE_COUNT];
            for (int i = 0; i < corners.Count; i++) {
                int aruco_id = (int) (ids.get(i, 0)[0]);
                int src_i = arucoTosrc(aruco_id);
                int corner_i = aruco_id % 4;

                Debug.LogFormat("AD - 101: aruco_id: {0}; corner_i: {1}; src_i: {2}", aruco_id, corner_i, src_i);

                // Store corner[i] into spa[src_i]
                src_point_array[src_i] = new Point(corners[i].get(0, corner_i)[0], corners[i].get(0, corner_i)[1]);

                // Display the corner as circle on outMat. 
                Imgproc.circle(cached_initMat, src_point_array[src_i], 10, new Scalar(255, 255, 0));
            }

            // Converting to Ray values for Raycast
            Camera _cam = Camera.main;
            if (_cam != null) {
                for (int i = 0; i < FACE_COUNT; i++) {
                    if (src_point_array[i] != null) {
                        src_ray_array[i] = _cam.ScreenPointToRay(
                            new Vector3((float) src_point_array[i].x,(float) src_point_array[i].y, 0)).direction;
                        OnArucoRayFound.Invoke(src_ray_array[i], i);
                    }
                }
            }
            Debug.LogFormat("Detected Direction: {0}", src_ray_array[0]);
            Debug.LogFormat("Camera Direction: {0}", _cam.transform.forward);

            // Setting World Points (via raycast): 

            // Count non-null source points 
            bool spa_full = (count_src_nulls() == 7);

            // Check if have valid faces
            for (int i = 0; i < 3; i++) {
                // faceX_full[i] = check_faces(i); 
                faceX_full[i] = check_faces(i); 
            }

            Core.flip(cached_initMat, outMat, 0);
        }

        #region Event Handlers
        /// <summary>
        /// Updates preview object with new captured image
        /// </summary>
        /// <param name="texture">The new image that got captured.</param>
        public void OnImageCaptured(Texture2D texture)
        {
            // Convert Texture to Mat
            cached_initMat = new Mat(1080, 1920, CvType.CV_8UC1); 

            Utils.texture2DToMat(texture, cached_initMat, true, 0);

            // Processing the Mat
            ArucoDetection();

            Texture2D out_texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            Utils.matToTexture2D(outMat, out_texture, false, 0);

            if(_previewObject != null)
            {
                _previewObject.SetActive(true);
                Renderer renderer = _previewObject.GetComponent<Renderer>();
                if(renderer != null)
                {
                    renderer.material.mainTexture = out_texture;
                }
            }
        }

        public void OnRaycastHit(MLWorldRays.MLWorldRaycastResultState state, RaycastHit hit, float confidence, int index)
        {
            src_world_array[index] = hit.transform.position;
            Debug.LogFormat("World Point {0} = {1} w/ confidence {2}", index, src_world_array[index], confidence);
        }
        #endregion
    }
}
