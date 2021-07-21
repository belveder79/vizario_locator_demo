using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationHandler : MonoBehaviour
{

    private VizarioGPS gps = null;
    private MapCreator map = null;


    public GameObject IMUVisualization = null;
    public bool useCallback = true;

    // Start is called before the first frame update
    void Start()
    {
        gps = GameObject.Find("Runtime").GetComponent<VizarioGPS>();

        if (gps == null)
        {
            Debug.LogError("VizarioGPSBehaviour not in Runtime!");
            return;
        }

        map = GameObject.Find("MapComponent").GetComponent<MapCreator>();

        if (map == null)
        {
            Debug.LogError("MapCreator not in MapComponent!");
            return;
        }

        if (useCallback)
        {
            gps.SetAltiCallback(AltiCallback);
            gps.SetGPSCallback(GPSCallback);
            gps.SetGyroCallback(GyroCallback);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GyroCallback(Quaternion quaternion)
    {
        if (IMUVisualization != null)
        {
            //Debug.Log(quaternion.ToString());
            IMUVisualization.transform.localRotation = quaternion;
        }
    }

    private void GPSCallback(double x, double y, string z, int fixState)
    {
        Debug.Log("x: " + x + ", y: " + y + ", z: " + z + ", fix: " + fixState);
        map.setAvatarPositionUTM(x, y, z, fixState);
    }

    private void AltiCallback(float altitude, float temp)
    {
        Debug.Log("altitude: " + altitude + ", temp: " + temp);
    }
}
