using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class Raycaster : MonoBehaviour
{
    private ARRaycastManager m_RaycastManager;
    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    public Camera camera = null;

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public float getGroundPlaneHeight()
    {
        float height = 0;
        Vector3 sPose = new Vector3(camera.transform.forward.x, 0, camera.transform.forward.z);
        sPose = camera.transform.position + sPose.normalized * 2;

        bool ret = m_RaycastManager.Raycast(new Ray(sPose, new Vector3(0, 1, 0)), s_Hits, TrackableType.PlaneWithinPolygon);
        if (ret)
        {
            var hitPose = s_Hits[0].pose;
            var hitPosition = hitPose.position;
            var hitRot = hitPose.rotation;

            height = sPose.y - hitPosition.y;
        }
        return height;
    }
}
