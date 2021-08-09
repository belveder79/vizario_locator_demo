using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationHandler : MonoBehaviour
{

    private VizarioGPS gps = null;
    private MapCreator map = null;
    private NorthingHandler northingHandler = null;

    private bool lastMqttStat = false;
    private bool lastChipStat = false;
    private bool runLocalGPS = true;



    public bool useCallback = true;
    public bool useGPSNorthing = true;
    public GameObject IMUVisualization = null;
    public Text mqttConnectionText = null;
    public Text chipConnectionText = null;
    public Text gpsFixText = null;

    private bool mapCreated = false;

    public bool copyCalibFromResources = false;
    public string calibFile = "";

    //visualize object
    public GameObject WorldOrigin = null;
    //example how to add pre external obj with utm coords into scene
    //IF 11 534753,313	5211701,173
    //public double ObjUtmX = 0;
    //public double ObjUtmY = 0;

    public GameObject arCam = null;
    public PlaceOnPlane placePlane = null;

    public GameObject prefabToPlace = null;

    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);

    private List<GameObject> placedObjcts = new List<GameObject>();

    //debugging
    //private string debugFile = "debug.txt";
    public Text infoText = null;

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

        if(WorldOrigin == null || placePlane == null) // ObjToVisualize == null ||
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

        northingHandler = GameObject.Find("MapComponent").GetComponent<NorthingHandler>();

        if (northingHandler == null)
        {
            Debug.LogError("NorthingHandler not in MapComponent!");
            return;
        }

        if (gpsFixText == null || mqttConnectionText == null || chipConnectionText == null)
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

        //var lat = 48.16224235117572;
        //var lon = 16.349907735268857;
        //map.CreateMap(lat - 0.001, lon - 0.002, lat + 0.001, lon + 0.002);


        //debugging
        //if(!File.Exists(Path.Combine(Application.persistentDataPath, debugFile)))
        //{
        //    File.WriteAllText(Path.Combine(Application.persistentDataPath, debugFile), "utm_x;utm_y;gpsFix;gyro;camPos;camRot;\n");
        //}

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

        //only use with RTK fixed positions!
        if(useGPSNorthing && fixState == 4)
        {
            float ts = (int)(System.DateTime.UtcNow - epochStart).TotalMilliseconds;
            Vector3 camposition = arCam.transform.localPosition;
            NorthingHandler.PostionElement p = new NorthingHandler.PostionElement(ts, x, y, camposition);
            northingHandler.PushPosition(p);
        }

        //debugging
        //Quaternion camrot = arCam.transform.localRotation;
        //Vector3 camposition = arCam.transform.localPosition;
        //Quaternion q;
        //bool ret = gps.GetGyroQuaternion(out q);

        //if (!ret)
        //{
        //    Debug.Log("no gyro fix");
        //    return;
        //}

        //File.AppendAllText(Path.Combine(Application.persistentDataPath, debugFile), x.ToString("F3") + ";" + y.ToString("F3") + ";"+ fixState + ";"  +
        //                       q + ";" + camposition.ToString() + ";" + camrot.ToString() + "\n");
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
                gpsFixText.text = "No Fix";
            }
            else if (lastGPSStat < 2)
            {
                gpsFixText.color = Color.yellow;
                gpsFixText.text = "3D Fix";
            }
            else if(lastGPSStat == 5)
            {
                gpsFixText.color = Color.green;
                gpsFixText.text = "RTK Float";
            }
            else
            {
                gpsFixText.color = Color.green;
                gpsFixText.text = "RTK Fixed";
            }
        }
    }

    //measure and add Object in scene
    public void AddObject()
    {
        if (gps != null)
        {
            Vector3 origin; Pose PlanePose; Quaternion originRot;
            bool ret = placePlane.getRayHit(out origin, out PlanePose, out originRot);

            if (!ret)
            {
                Debug.Log("ray did not hit anything");
                return;
            }

            ////local testing
            //var newObj = Instantiate(prefabToPlace, PlanePose.position, Quaternion.identity);
            //newObj.transform.localRotation = PlanePose.rotation;

            double x, y;
            string z;
            int fix;

            ret = gps.GetUTMPosition(out x, out y, out z, out fix);

            if (!ret)
            {
                Debug.Log("no gps fix");
                return;
            }

            Debug.Log("ray hit: " + ret.ToString());
            if (ret)
            {
                var newObj = Instantiate(prefabToPlace, PlanePose.position, Quaternion.identity);
                newObj.transform.localRotation = PlanePose.rotation;
                Text objTxt = newObj.GetComponentInChildren<Canvas>().GetComponentInChildren<Text>();

                var relative_dis = origin - PlanePose.position;
                placedObjcts.Add(newObj);

                float correction = 0;
                if (useGPSNorthing && (northingHandler.correctionsCount() > 1000))
                {
                    correction = northingHandler.calculateCorrection();
                    correction = correction * (-1);  //todo change caluclations to no need of negation

                    //debugging
                    Quaternion q;
                    ret = gps.GetGyroQuaternion(out q);

                    if (!ret)
                        return;

                    //correct so y = northing 
                    Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * originRot;
                    Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

                    //rotate to adjust northing (AR Camera = only local tracking = no real north)
                    float correction2 = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;

                    infoText.text += correction + " vs. " + correction2 + "\n";
                }
                else
                {
                    //roate relative distance Vector, since the calculation is in ARFoundation coord sys, which is not north orientated
                    //first move our world Origin to current ARCamera Tracking position(current GPS position = new Origin)
                    Quaternion q;
                    ret = gps.GetGyroQuaternion(out q);

                    if (!ret)
                    {
                        Debug.Log("no gyro fix");
                        return;
                    }

                    //correct so y = northing 
                    Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * originRot;
                    Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

                    //rotate to adjust northing (AR Camera = only local tracking = no real north)
                    correction = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;
                }

                //todo validation with iPad lidar
                relative_dis = Quaternion.Euler(0, correction, 0) * relative_dis; 

                double m_x = x + relative_dis.x;
                double m_y = y + relative_dis.z;

                newObj.name = "Measurement " + placedObjcts.Count.ToString();
                objTxt.text = "Measurement " + placedObjcts.Count.ToString() + "\nx: " + m_x.ToString("F3") + " m \ny: " + m_y.ToString("F3") + " m";

                newObj.transform.parent = WorldOrigin.transform;
            }
        }
    }

    public void ClearPlacedObjects()
    {
        foreach(var obj in placedObjcts)
        {
            Destroy(obj);
        }
        placedObjcts.Clear();
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

        float correction = 0;
        if (useGPSNorthing && (northingHandler.correctionsCount() > 1000))
        {
            correction = northingHandler.calculateCorrection();
            correction = correction * (-1);  //todo change caluclations to no need of negation

            //debugging
            Quaternion q;
            res = gps.GetGyroQuaternion(out q);

            if (!res)
                return;

            //todo use another GameObject, so all child components will be automatically be transformed to the new Origin by Unity

            //first move our world Origin to current ARCamera Tracking position(current GPS position = new Origin)
            WorldOrigin.transform.localPosition = camposition;

            //correct so y = northing 
            Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * camrot;
            Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

            //rotate to adjust northing (AR Camera = only local tracking = no real north)
            float correction2 = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;
            infoText.text += correction + " vs. " + correction2 + "\n";
        }
        else
        {
            Quaternion q;
            res = gps.GetGyroQuaternion(out q);

            if (!res)
                return;

            //todo use another GameObject, so all child components will be automatically be transformed to the new Origin by Unity

            //first move our world Origin to current ARCamera Tracking position(current GPS position = new Origin)
            WorldOrigin.transform.localPosition = camposition;

            //correct so y = northing 
            Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * camrot;
            Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

            //rotate to adjust northing (AR Camera = only local tracking = no real north)
            correction = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;
        }

        WorldOrigin.transform.localRotation = Quaternion.AngleAxis(correction, Vector3.up);

        ////place Object to Visualize in World (y = z bc of unity)
        ////ObjToVisualize.transform.localPosition = new Vector3((float)(ObjUtmX - x), 0.5f, (float)(ObjUtmY - y));
        //Vector3 relPos = new Vector3((float)(ObjUtmX - x), 0.5f, (float)(ObjUtmY - y));
        //Vector3 RayOrigin = WorldOrigin.transform.localPosition + relPos;
        //Vector3 PlanePos;
        //bool ret = placePlane.getPlanePos(RayOrigin, out PlanePos);
        //Debug.Log("ray hit: " + ret.ToString());

        ////not 0.5f, should be half height of object 
        //ObjToVisualize.transform.localPosition = relPos + new Vector3(0, PlanePos.y + 0.5f, 0); //if plane not hit, height will be 0
      
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
