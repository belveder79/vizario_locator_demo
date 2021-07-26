using System.Collections;
using System.Collections.Generic;
using System.IO;
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

    private bool mapCreated = false;

    public bool copyCalibFromResources = false;
    public string calibFile = "";

    //visualize object
    public GameObject WorldOrigin = null;
    public GameObject ObjToVisualize = null;
    public GameObject ObjToVisualizeVerify = null;

    //IF 11 534753,313	5211701,173
    public double ObjUtmX = 0;
    public double ObjUtmY = 0;
    public GameObject arCam = null;

    // Start is called before the first frame update
    void Start()
    {

        // todo check where to place imu calib load rescource and copy to persitctence ?
        if (copyCalibFromResources)
        {
            string m_Path = Application.persistentDataPath;
            TextAsset calibAsset = (TextAsset)Resources.Load(Path.Combine("imu_calibrations", System.IO.Path.GetFileNameWithoutExtension(calibFile)));
            Debug.Log(Path.Combine("imu_calibrations", System.IO.Path.GetFileNameWithoutExtension(calibFile)));

            if (!Directory.Exists(m_Path))
                Directory.CreateDirectory(m_Path);

            string calibFilePath = Path.Combine(m_Path, calibFile);
            Debug.Log(calibFilePath);
            if (File.Exists(calibFilePath))
                File.Delete(calibFilePath);

            File.WriteAllText(calibFilePath, calibAsset.text);
            Debug.Log("calib copied");

        }

        if(WorldOrigin == null || ObjToVisualize == null)
        {
            Debug.LogError("Objects for visualization not linked");
            return;
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

        //Debug.Log("x: " + x + ", y: " + y + ", z: " + z + ", fix: " + fixState);

        if (!mapCreated){

            
            gps.GetLatLonPoition(out var lat, out var lon, out int state);

            map.CreateMap(lat - 0.001, lon - 0.002, lat + 0.001, lon + 0.002);
            
            mapCreated = true;
            Debug.Log("set map coords.");
        }

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

    // setup origin for real world visualization (origin is init pose of AROrigin) 
    public void SetWorldOrigin()
    {

        Quaternion camrot = arCam.transform.localRotation;
        Vector3 camposition = arCam.transform.localPosition;
        double x, y;
        int fix;
        string z;

        bool res = gps.GetUTMPosition(out x, out y, out z, out fix);

        if (!res)
        {
            return;
        }

        Quaternion q;
        res = gps.GetGyroQuaternion(out q);

        if (!res)
            return;


        //double x = 534755.313;
        //double y = 5211704.173;
        //Quaternion q = Quaternion.identity;

        //first move our world Origin to current ARCamera Tracking position(current GPS position = new Origin)
        WorldOrigin.transform.localPosition = camposition;

        //correct so y = northing 
        Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * camrot;
        Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

        //rotate to adjust northing (AR Camera = only local tracking = no real north)
        float correction = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;
        WorldOrigin.transform.localRotation = Quaternion.AngleAxis(correction, Vector3.up);


        //place Object to Visualize in World (y = z bc of unity)
        ObjToVisualize.transform.localPosition = new Vector3((float)(ObjUtmX - x), 0.5f, (float)(ObjUtmY - y));
        ObjToVisualizeVerify.transform.localPosition = new Vector3((float)(x - ObjUtmX), 0.5f, (float)(y - ObjUtmY));

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
                //Debug.Log("x: " + x + ", y: " + y + ", z: " + z + ", fix: " + -1);
                map.setAvatarPositionUTM(x, y, z, -1, 2);



                //if (!mapCreated)
                //{
                //    map.CreateMap(_latitude - 0.001, _longitude - 0.002, _latitude + 0.001, _longitude + 0.002);

                //    mapCreated = true;
                //    Debug.Log("set map coords.");
                //}

                yield return new WaitForSecondsRealtime(1);
            }
        }

        // Stop service if there is no need to query location updates continuously
        UnityEngine.Input.location.Stop();
    }
}
