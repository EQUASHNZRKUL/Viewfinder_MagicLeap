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

namespace UnityEngine.XR.MagicLeap
{
    /// <summary>
    /// Encapsulates an ML raycast against the physical world from the headpose position and orientation.
    /// </summary>
    [AddComponentMenu("Magic Leap/Raycast/World Raycast Head")]
    public class WorldRaycastAruco : BaseRaycast
    {
        #region Private Variables
        private Camera _camera;
        private Vector3 direction; 
        #endregion

        #region Protected Properties
        /// <summary>
        /// Returns the position of current headpose.
        /// </summary>
        override protected Vector3 Position
        {
            get
            {
                return _camera.transform.position;
            }
        }

        /// <summary>
        /// Returns the direction of current headpose.
        /// </summary>
        override protected Vector3 Direction
        {
            get
            {
                Debug.LogFormat("Direction: {0}", direction);
                if (direction != Vector3.zero)
                {
                    Debug.LogFormat("Inside direction: {0}", direction);
                    return direction; 
                }
                return Vector3.zero;
            }
        }

        /// <summary>
        /// Returns the up vector of current headpose.
        /// </summary>
        override protected Vector3 Up
        {
            get
            {
                return _camera.transform.up;
            }
        }
        #endregion

        #region Unity Methods
        /// <summary>
        /// Initialize variables.
        /// </summary>
        void Awake()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                Debug.LogError("Error: WorldRaycastHead._camera is null, disabling script.");
                enabled = false;
                return;
            }
        }
        #endregion

        #region Event Handlers
        public void ArucoRayReceived(Vector3 ray, int index)
        {
            Debug.LogFormat("WRA: 95 -- Received: {0} to {1}", index, ray);
            direction = ray; 
        }
        #endregion
    }
}
