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

using UnityEngine;
using UnityEngine.UI;

using OpenCVForUnity;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.ImgprocModule;

namespace MagicLeap
{
    public class CVController : MonoBehaviour
    {
        #region Private Variables
        [SerializeField, Tooltip("Object to set new images on.")]
        private GameObject _previewObject = null;

        // Mats
        public Mat outMat = new Mat(1080, 1920, CvType.CV_8UC1);
        private Mat cached_initMat = new Mat (1080, 1920, CvType.CV_8UC1);

        #endregion

        #region Event Handlers
        /// <summary>
        /// Updates preview object with new captured image
        /// </summary>
        /// <param name="texture">The new image that got captured.</param>
        public void OnImageCaptured(Texture2D texture)
        {
            // Convert Texture to Mat
            Debug.Log("equash 59: Set Texture Trace");
            Debug.LogFormat("equash 60: texture h x w x format: {0} x {1} x {2}", texture.height, texture.width, texture.format);
            cached_initMat = new Mat(1080, 1920, CvType.CV_8UC1); 

            Debug.LogFormat("equash 48: {0}", (cached_initMat == null));
            Utils.texture2DToMat(texture, cached_initMat, false, 0);

            // Processing the Mat
            outMat = cached_initMat;
            Debug.Log("Mat Processed");

            Texture2D out_texture = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            Utils.matToTexture2D(outMat, out_texture, true, 0);

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
        #endregion

        private void Awake() {
            Debug.Log("equash: awake");
        }
    }
}
