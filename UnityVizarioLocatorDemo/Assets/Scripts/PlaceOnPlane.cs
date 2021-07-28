using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceOnPlane : MonoBehaviour
{
    private ARRaycastManager m_RaycastManager;

    public Camera camera = null;
    public GameObject test = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Awake()
    {
        m_RaycastManager = GetComponent<ARRaycastManager>();
    }

    // Update is called once per frame
    void Update()
    {
        bool ret = m_RaycastManager.Raycast(new Ray(camera.transform.position, camera.transform.forward), s_Hits, TrackableType.All);
        if (ret)
        {
            var hitPose = s_Hits[0].pose;
            var hitPosition = hitPose.position;

            
            //todo nice fadenkreuz
            test.transform.localPosition = hitPosition;

        }
        else
        {
            Debug.Log("no hit");
        }
    }



    static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();
    public bool getPlanePos(Vector3 origin, out Vector3 hitPosition)
    {
        Ray ray = new Ray(origin, Vector3.down);
        Debug.Log(Vector3.down);
        bool ret = m_RaycastManager.Raycast(ray, s_Hits);

        if (ret)
        {
            var hitPose = s_Hits[0].pose;
            hitPosition = hitPose.position;

            //watch out, will be placed in the middle of the plane
            //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //cube.transform.localPosition = position;
            return true;
        }
        
        Debug.Log("ray hit nothin");
        hitPosition = new Vector3(0,0,0);
        return false;
    }


    public bool getRayHit(out Vector3 pos)
    {
        pos = new Vector3();
        bool ret = m_RaycastManager.Raycast(new Ray(camera.transform.position, camera.transform.forward), s_Hits, TrackableType.All);
        if (ret)
        {
            var hitPose = s_Hits[0].pose;
            var hitPosition = hitPose.position;


            //todo measure distance !
            pos = hitPosition;
            return true;
        }
        else
        {
            return false;
        }
    }
}
