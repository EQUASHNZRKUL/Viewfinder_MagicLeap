using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.MagicLeap;
using Unity.Collections;

namespace MagicLeap
{
    public class RaycastMarker : MonoBehaviour
    {
        #region Private Variables
        [SerializeField]
        [Tooltip("Instantiates this prefab on a gameObject at the touch location.")]
        GameObject m_PlacedPrefab;
        public GameObject placedPrefab
        {
            get { return m_PlacedPrefab; }
            set { m_PlacedPrefab = value; }
        }

        private GameObject spawnedObject;

        // current index of spawnedObjects ready to spawn
        private int idx = 0; 

        private Camera _camera; 

        #endregion

        #region Unity Methods
        void Awake()
        {
            MagicLeapDevice.RegisterOnHeadTrackingMapEvent(OnHeadTrackingMapEvent);
            spawnedObject = Instantiate(m_PlacedPrefab, new Vector3(0.0f, 0.0f, 0.0f), new Quaternion(0.0f, 0.0f, 0.0f, 0.0f));
            _camera = Camera.main; 
        }

        void Update()
        {
        }

        void OnDestroy()
        {
            MagicLeapDevice.UnregisterOnHeadTrackingMapEvent(OnHeadTrackingMapEvent);
        }
        #endregion

        #region Private Methods

        #endregion

        #region Event Handlers
        public void OnRaycastHit(MLWorldRays.MLWorldRaycastResultState state, RaycastHit hit, float confidence, int index)
        {
            spawnedObject.transform.position = hit.transform.position;
        }


        private void OnHeadTrackingMapEvent(MLHeadTrackingMapEvent mapEvents)
        {
            if (mapEvents.IsLost())
            {
                // Destroy(spawnedObject);
            }
        }

        #endregion
    }
}