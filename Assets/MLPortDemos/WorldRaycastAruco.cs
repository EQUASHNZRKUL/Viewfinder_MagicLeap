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
using UnityEngine.Events;

namespace UnityEngine.XR.MagicLeap
{
    /// <summary>
    /// Encapsulates an ML raycast against the physical world from the headpose position and orientation.
    /// </summary>
    public class WorldRaycastAruco : BaseRaycast
    {
        #region Public Variables
        [System.Serializable]
        public new class RaycastResultEvent : UnityEvent<MLWorldRays.MLWorldRaycastResultState, RaycastHit, float, int> { }

        [Space]
        [Tooltip("The callback handler for raycast result.")]
        public new RaycastResultEvent OnRaycastResult;
        #endregion

        #region Private Variables
        private Camera _camera;
        private Vector3 direction;
        private int aruco_i;  
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
                if (direction != null) {
                    return direction; 
                }
                return _camera.transform.forward;
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
            direction = ray; 
            aruco_i = index;
        }

        /// <summary>
        /// Callback handler called when raycast call has a result.
        /// </summary>
        /// <param name="state"> The state of the raycast result.</param>
        /// <param name="point"> Position of the hit.</param>
        /// <param name="normal"> Normal of the surface hit.</param>
        /// <param name="confidence"> Confidence value on hit.</param>
        override protected void HandleOnReceiveRaycast(MLWorldRays.MLWorldRaycastResultState state, Vector3 point, Vector3 normal, float confidence)
        {
            RaycastHit result = GetWorldRaycastResult(state, point, normal, confidence);
            OnRaycastResult.Invoke(state, result, confidence, aruco_i);

            _isReady = true;
        }
        #endregion
    }
}
