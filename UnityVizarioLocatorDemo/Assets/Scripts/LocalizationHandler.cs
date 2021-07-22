﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationHandler : MonoBehaviour
{

    private VizarioGPS gps = null;
    private MapCreator map = null;

    private bool lastMqttStat = false;
    private bool lastChipStat = false;
    private bool runLocalGPS = true;



    public bool useCallback = true;
    public GameObject IMUVisualization = null;
    public Text mqttConnectionText = null;
    public Text chipConnectionText = null;
    public Text gpsFixText = null;

    // Start is called before the first frame update
    void Start()
    {

        // todo check where to place imu calib load rescource and copy to persitctence ?

        if (!System.IO.File.Exists(Application.persistentDataPath + "/imuCalib.xml"))
        {
            //xmlAsset = Resources.Load("Config.xml");
            //xmlContent = xmlAsset.text;
            //System.IO.File.WriteAllText(Application.persistentDataPath + "/Config.xml", xmlContent);
        }



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

        if(gpsFixText == null || mqttConnectionText == null || chipConnectionText == null)
        {
            Debug.LogError("some text fields no set!");
            
        }
        else
        {
            gpsFixText.text = " " + lastGPSStat;
            gpsFixText.color = Color.red;

            mqttConnectionText.text = "Disconnedted";
            mqttConnectionText.color = Color.red;

            chipConnectionText.text = "Disconnedted";
            chipConnectionText.color = Color.red;
        }

        if (useCallback)
        {
            gps.SetAltiCallback(AltiCallback);
            gps.SetGPSCallback(GPSCallback);
            gps.SetGyroCallback(GyroCallback);
        }

        StartCoroutine(LocationCoroutine());
       
    }

    // Update is called once per frame
    void Update()
    {
        if (gps == null)
            gps = GameObject.Find("Runtime").GetComponent<VizarioGPS>();

        if (lastMqttStat != gps.IsMqttConnected())
        {
            lastMqttStat = gps.IsMqttConnected();
            if (lastMqttStat)
            {
                mqttConnectionText.text = "Connected";
                mqttConnectionText.color = Color.green;
            }              
            else
            {
                mqttConnectionText.text = "Disconnedted";
                mqttConnectionText.color = Color.red;
            }
                
        }

        if (lastChipStat != gps.IsChipConnected())
        {
            lastChipStat = gps.IsChipConnected();
            if (lastChipStat)
            {
                chipConnectionText.text = "Connected";
                chipConnectionText.color = Color.green;
            }
            else
            {
                chipConnectionText.text = "Disconnedted";
                chipConnectionText.color = Color.red;
            }
               
        }

        if (!useCallback && gps != null)
        {
            Quaternion q;

            if (gps.GetGyroQuaternion(out q))
            {
                IMUVisualization.transform.localRotation = q;
            }
        }
    }

    private void OnDestroy()
    {
        runLocalGPS = false;
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
        map.setAvatarPositionUTM(x, y, z, fixState, 1);
        setGPSFixText(fixState);
    }

    private void AltiCallback(float altitude, float temp)
    {
        Debug.Log("altitude: " + altitude + ", temp: " + temp);
    }


    private int lastGPSStat = -1;
    private void setGPSFixText(int state)
    {
        if (lastGPSStat != state)
        {
            lastGPSStat = state;
            gpsFixText.text = " " + lastGPSStat;
            if (lastGPSStat == 0)
            {
                gpsFixText.color = Color.red;
                //if (arrow != null)
                //{
                //    Debug.Log("set red");
                //    arrow.GetComponent<MeshRenderer>().material = red;
                //}
            }
            else if (lastGPSStat < 2)
            {
                gpsFixText.color = Color.yellow;
                //if (arrow != null)
                //{
                //    arrow.GetComponent<MeshRenderer>().material = yellow;
                //}
            }
            else
            {
                gpsFixText.color = Color.green;
                //if (arrow != null)
                //{
                //    arrow.GetComponent<MeshRenderer>().material = green;
                //}
            }
        }
    }


    // ------------------------------ local GPS
    IEnumerator LocationCoroutine()
    {
        // Uncomment if you want to test with Unity Remote
        /*#if UNITY_EDITOR
                yield return new WaitWhile(() => !UnityEditor.EditorApplication.isRemoteConnected);
                yield return new WaitForSecondsRealtime(5f);
        #endif*/
#if UNITY_EDITOR
        // No permission handling needed in Editor
#elif UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.CoarseLocation)) {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.CoarseLocation);
        }

        // First, check if user has location service enabled
        if (!UnityEngine.Input.location.isEnabledByUser) {
            // TODO Failure
            Debug.LogFormat("Android and Location not enabled");
            yield break;
        }

#elif UNITY_IOS
        if (!UnityEngine.Input.location.isEnabledByUser) {
            // TODO Failure
            Debug.LogFormat("IOS and Location not enabled");
            yield break;
        }
#endif
        // Start service before querying location
        UnityEngine.Input.location.Start(500f, 500f);

        // Wait until service initializes
        int maxWait = 15;
        while (UnityEngine.Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSecondsRealtime(1);
            maxWait--;
        }

        // Editor has a bug which doesn't set the service status to Initializing. So extra wait in Editor.
#if UNITY_EDITOR
        int editorMaxWait = 15;
        while (UnityEngine.Input.location.status == LocationServiceStatus.Stopped && editorMaxWait > 0)
        {
            yield return new WaitForSecondsRealtime(1);
            editorMaxWait--;
        }
#endif

        // Service didn't initialize in 15 seconds
        if (maxWait < 1)
        {
            // TODO Failure
            Debug.LogFormat("Timed out");
            yield break;
        }

        // Connection has failed
        if (UnityEngine.Input.location.status != LocationServiceStatus.Running)
        {
            // TODO Failure
            Debug.LogFormat("Unable to determine device location. Failed with status {0}", UnityEngine.Input.location.status);
            yield break;
        }
        else
        {
            Debug.LogFormat("Location service live. status {0}", UnityEngine.Input.location.status);
            // Access granted and location value could be retrieved
            Debug.LogFormat("Location: "
                + UnityEngine.Input.location.lastData.latitude + " "
                + UnityEngine.Input.location.lastData.longitude + " "
                + UnityEngine.Input.location.lastData.altitude + " "
                + UnityEngine.Input.location.lastData.horizontalAccuracy + " "
                + UnityEngine.Input.location.lastData.timestamp);

            while (runLocalGPS)
            {
                var _latitude = UnityEngine.Input.location.lastData.latitude;
                var _longitude = UnityEngine.Input.location.lastData.longitude;

                double x, y;
                string z;
                PositionConverter.LatLongtoUTM(_latitude, _longitude, out x, out y, out z);
                Debug.Log("x: " + x + ", y: " + y + ", z: " + z + ", fix: " + -1);
                map.setAvatarPositionUTM(x, y, z, -1, 2);

                yield return new WaitForSecondsRealtime(1);
            }
        }

        // Stop service if there is no need to query location updates continuously
        UnityEngine.Input.location.Stop();
    }
}
