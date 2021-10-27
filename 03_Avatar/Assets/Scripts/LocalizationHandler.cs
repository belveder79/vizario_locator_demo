using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Vizario;
using System.IO;
using System.Security.Cryptography.X509Certificates;


public class LocalizationHandler : MonoBehaviour
{

    [Serializable]
    public class AvatarPose
    {
        public AvatarPose(string ID, double x, double y, float alt, Quaternion rot)
        {
            this.ID = ID;
            this.x = x;
            this.y = y;
            this.alt = alt;
            this.q_x = rot.x;
            this.q_y = rot.y;
            this.q_z = rot.z;
            this.q_w = rot.w;
        }

        public string ID { get; set; }
        public double x  { get; set; }
        public double y { get; set; }
        public float alt { get; set; }
        //public Quaternion rotation { get; set; }
        public float q_x { get; set; }
        public float q_y { get; set; }
        public float q_z { get; set; }
        public float q_w { get; set; }

        public Quaternion getQuaternion() { return new Quaternion(q_x, q_y, q_z, q_w);  }
    }

    private VizarioCapsLocManager capsLoc = null;
    private NorthingHandler northingHandler = null;
    private Raycaster raycaster = null;
    private MapCreator map = null;

    public string MqttServer = null;
    public int MqttPort = -1;

    public string certificate = null;
    public bool copyFromStreamingAssets = false;
    private string cert_ = null;

    private string myAvatarID;

    private bool lastMqttStat = false;
    private bool lastChipStat = false;
    private bool mapCreated = false;

    public bool useGPSNorthing = true;

    public Text mqttConnectionText = null;
    public Text chipConnectionText = null;
    public Text gpsFixText = null;

    public GameObject arCam = null;
    public GameObject avatarPrefap = null;
    public GameObject WorldOrigin = null;
    private GameObject OriginObjectHook = null;

    private bool useAltimeter = true;

    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    Dictionary<string, Avatar> avatars = new Dictionary<string, Avatar>();
    AvatarPose myLastPose = null;
    private double last_gps_ts = 0;

    Queue<Action> runnerQ = new Queue<Action>();

    private double x_utm_origin = 534892.65866935183;
    private double y_utm_origin = 5211821.44362808;
    private Vector3 ArCam_Origin = new Vector3(0, 0, 0);

    string debugFile;

    //debug
    public bool debugging = false;
    public UnityEngine.UI.Slider slider = null;

    // Start is called before the first frame update
    void Start()
    {
        capsLoc = GameObject.Find("CapsLocRuntime").GetComponent<VizarioCapsLocManager>();

        if (capsLoc == null)
        {
            Debug.LogError("VizarioCapsLocManager not in CapsLocRuntime!");
        }

        northingHandler = GameObject.Find("AvatarSession").GetComponent<NorthingHandler>();

        if (northingHandler == null)
        {
            Debug.LogError("NorthingHandler not in AvatarSession!");
            //return;
        }

        raycaster = GameObject.Find("AR Session Origin").GetComponent<Raycaster>();

        if(raycaster == null)
        {
            Debug.LogError("Raycaster not in Ar Session Origin");
        }

        map = GameObject.Find("MapComponent").GetComponent<MapCreator>();

        if (map == null)
        {
            Debug.LogError("map not in MapComponent");
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

        if (MqttServer == null || MqttServer == "")
        {
            Debug.LogError("Mqtt Server not specified");
            return;
        }
        if( MqttPort == -1)
        {
            Debug.Log("no Mqtt Port specified, try using 1883");
            MqttPort = 1883;
        }

        myAvatarID = GetMacAddress();
        if (myAvatarID == null) {
            Debug.Log("mac adress not ready, use random num");

            myAvatarID = UnityEngine.Random.Range(0f, 999999f).ToString();
        }
        else
        {
            Debug.Log("ID = " + myAvatarID);
        }

        OriginObjectHook = new GameObject("Object Hook");
        OriginObjectHook.transform.parent = WorldOrigin.transform;


        if (debugging)
            myAvatarID = "020000000000";

        if(certificate != null) {
          string filepath = copyFromStreamingAssets ? Application.streamingAssetsPath : Application.persistentDataPath;
          filepath = Path.Combine(filepath, certificate);

          if(copyFromStreamingAssets) {
            string fileText = ReadFileAsString(filepath, copyFromStreamingAssets);

            if (copyFromStreamingAssets)
                filepath = Path.Combine(Application.persistentDataPath, certificate);
                File.WriteAllText(filepath, fileText);
          }
          cert_ = filepath;
        }
        else
        {
            Debug.Log("cert is null, so do not use tls.");
        }

        debugFile = Path.Combine(Application.persistentDataPath, "debugFile.txt");

        if(!debugging)
            File.WriteAllText(debugFile, "");

        if(slider != null)
            slider.onValueChanged.AddListener(delegate {SliderChanged(); });

        StartCoroutine(StartupMqtt());
    }

    IEnumerator StartupMqtt()
    {

        MQTTClient.SetHost(MqttServer, MqttPort);
        MQTTClient.SetClientID(myAvatarID);

        if(cert_ != null) {
            var caCert = new X509Certificate(cert_);
            MQTTClient.StartClientWithCert(caCert);
        }
        else
          MQTTClient.StartClient();

        yield return new WaitForSeconds(1);

        MQTTClient.Subscribe("PoseUpdate");

        MQTTClient.RegisterCallback(MQTTNotify);
    }

    private string HandleAvatarPoseUpdate(string[] args)
    {
        var topic = args[0];
        var payload = args[1];

        //Debug.Log(payload);

        AvatarPose p = JsonConvert.DeserializeObject<AvatarPose>(payload);

        if(!debugging)
            File.AppendAllText(debugFile, payload + ";" + arCam.transform.localPosition.ToString("F4") + ";" + arCam.transform.localRotation.ToString("F4") +  "\n");

        if(p.ID != myAvatarID)
        {
            //Debug.Log(payload);

            Avatar avatar = null;
            if (!avatars.ContainsKey(p.ID))
            {

                GameObject newPref = Instantiate(avatarPrefap, new Vector3(0,0,0), Quaternion.identity);
                Debug.Log("try create");
                newPref.name = p.ID;
                newPref.transform.parent = OriginObjectHook.transform;
                avatar = newPref.GetComponent<Avatar>();
                avatars.Add(p.ID, avatar);
                Debug.Log("new prefap created");
            }
            else
            {
                Debug.Log("try get avatar");
                avatars.TryGetValue(p.ID, out avatar);
            }
            OriginObjectHook.transform.localPosition = new Vector3(0,0,0);
            avatar.setNewPosition(p, x_utm_origin, y_utm_origin);


            map.setAvatarPositionUTM(p.x, p.y, " ", 1, 2); //todo
        }
        else if(debugging)
        {
            myLastPose = p;

            HandleGPSUpdate(p.x, p.y, "bla" , 1, 111);
        }

        return "";
    }

/// <summary>
/// Read from StreamingAssets
/// </summary>
/// <param name="url">string with filepath</param>
    static IEnumerator ReadStreamingAsset(string url)
{
    WWW www = new WWW(url);

    while (!www.isDone)
        yield return null;

    if (string.IsNullOrEmpty(www.error))
        yield return www.text;
    else
        yield return www.error;
}

/// <summary>
/// only works for persistent folder!
/// </summary>
/// <param name="path">Path to file.</param>
/// <param name="streamingassets">Is to be read from streaming assets folder</param>
public static string ReadFileAsString(string path, bool streamingassets = false)
{
    string returnstring = "";
    try
    {
#if NETFX_CORE // THIS IS THE WSA IMPLEMENTATION
            Debug.Log("WSA File Read...");
            Task<string> readtask = Task.Run(() => ReadTextContentAsync(path));
            readtask.Wait();
            return readtask.Result;
            //returnstring = System.IO.File.ReadAllText(path);
#elif UNITY_STANDALONE_OSX || NOUWP || UNITY_EDITOR_WIN  // THIS ONLY JUMPS IN FOR OSX CURRENTLY AND WINDOWS NATIVE
        Debug.Log("OSX/Windows File Read...");
        returnstring = System.IO.File.ReadAllText(path);
#elif UNITY_IOS || (UNITY_ANDROID && !UNITY_EDITOR)
        if (streamingassets) {
#if UNITY_IOS
            string tpath = "file://" + path;
#else
                string tpath = path; // check jar extension...
#endif
            IEnumerator e = ReadStreamingAsset(tpath);
            while (e.MoveNext())
                if (e.Current != null)
                {
                    returnstring = e.Current as string;
                    break;
                }
            }
            else
            {
                Debug.Log("IOS/Android File Read...");
                returnstring = System.IO.File.ReadAllText(path);
            }
#else // ALL OTHER OPERATING SYSTEMS
                Debug.Log("Generic File Read...");
                FileStream fileStream = new FileStream(path, FileMode.Open);
                StreamReader reader = new StreamReader(fileStream);
                returnstring = reader.ReadToEnd();
#endif

        }
        catch (Exception e)
        {
            Debug.Log("Platform::ReadFileAsString: ERROR => " + e.ToString());
            // throw;
        }
        return returnstring;
    }

    // Update is called once per frame
    void Update()
    {

        if (runnerQ.Count > 0)
        {
            var f = runnerQ.Dequeue();
            f?.Invoke();
        }

        if (capsLoc == null)
            capsLoc = GameObject.Find("CapsLocRuntime").GetComponent<VizarioCapsLocManager>();

        if (lastMqttStat != capsLoc.IsMqttConnected())
        {
            lastMqttStat = capsLoc.IsMqttConnected();
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

        if (lastChipStat != capsLoc.IsChipConnected())
        {
            lastChipStat = capsLoc.IsChipConnected();
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

        //todo just to debug
        //534753,313	5211701,173
        //string[] args = { "bla", "{\"ID\":\"123400\",\"x\":534753.313,\"y\":5211701.173,\"alt\":288.125,\"q_x\":0,\"q_y\":0,\"q_z\":0,\"q_w\":1}" };
        //HandleAvatarPoseUpdate(args);


        if (capsLoc != null)
        {
            //this would be one option, the other one is using CapsLocBehaviour directly on the GameObject as you can see in the scene
            Quaternion q;
            if (capsLoc.GetGyroQuaternion(out q))
            {
                //HandleGyroUpdate(q);
            }

            double x, y;
            int fix;
            string z;
            double ts;
            if(capsLoc.GetUTMPositionTs(out x, out y, out z, out fix, out ts))
            {
                HandleGPSUpdate(x, y, z, fix, ts);  //for northing
            }
            else
            {
                return;
            }

            float alt, temp;
            if (useAltimeter)
            {
                //no need to call this every Update, also not needed for this example
                if (capsLoc.GetAltimeterValues(out alt, out temp))
                {
                    //HandleAlitUpdate(alt, temp);
                }
            }
            else
            {
                alt = raycaster.getGroundPlaneHeight();
            }

            AvatarPose p = new AvatarPose(myAvatarID, x, y, alt, q);

            if(false)
            {
                p = new AvatarPose(myAvatarID, 1000, 60, 340, Quaternion.Euler(0,45,0));
            }

            myLastPose = p;
            try {
              var json = JsonConvert.SerializeObject(p);
              //Debug.Log(json.ToString());
              //var json = JsonConvert.SerializeObject(jsonObj[0]);
              //Debug.Log(json.ToString());
              //https://github.com/jilleJr/Newtonsoft.Json-for-Unity.git#13.0.102
              MQTTClient.Publish("PoseUpdate", json);
            }
            catch(Exception e)   {
                Debug.Log(e.ToString());
            }
        }
    }

    public string MQTTNotify(string[] args)
    {
        runnerQ.Enqueue(() => {
            HandleAvatarPoseUpdate(args);
        });
        return "";
    }

    private void OnDestroy()
    {
        MQTTClient.CallDispose();
    }

    private void HandleGyroUpdate(Quaternion quaternion)
    {

    }

    private void HandleGPSUpdate(double x, double y, string z, int fixState, double ts_gps)
    {
        // todo GPS CALIB !!!!
        
        if (ts_gps == last_gps_ts)
            return;

        last_gps_ts = ts_gps;

        setGPSFixText(fixState);

        double ts_internal = (DateTime.UtcNow - epochStart).TotalMilliseconds * 1000000;  //to ns
        Vector3 camposition = arCam.transform.localPosition;

        //Debug.Log(ts_gps.ToString("F9") + " - " + ts_internal.ToString("F9"));

        //only use with RTK fixed positions!
        if (useGPSNorthing && fixState == 4)
        {
            NorthingHandler.PostionElement p = new NorthingHandler.PostionElement(ts_internal, ts_gps, x, y, camposition);
            northingHandler.PushPosition(p);
        }

        if (shouldWeSetWorldOrigin(x, y, camposition))
            SetWorldOrigin();

        if (!mapCreated)
        {

            bool ret = capsLoc.GetLatLonPoition(out var lat, out var lon, out int state);


            if(debugging)
            {
                lat = 47.05902547761709f; lon = 15.459495326448641f;
                ret = true;
            }

            if(ret == false)
              return;

            map.CreateMap(lat - 0.001, lon - 0.002, lat + 0.001, lon + 0.002);

            mapCreated = true;
            Debug.Log("set map coords.");
        }
        else
        {
            map.setAvatarPositionUTM(x, y, z, fixState, 1);
        }
    }

    private void HandleAlitUpdate(float altitude, float temp)
    {
        Debug.Log("altitude: " + altitude + ", temp: " + temp);
    }

    private int lastGPSStat = -1;
    private void setGPSFixText(int state)
    {
        if (lastGPSStat != state)
        {
            lastGPSStat = state;
            gpsFixText.text = " no Fix";
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
            else if (lastGPSStat == 2)
            {
                gpsFixText.color = Color.yellow;
                gpsFixText.text = "DGPS";
            }
            else if (lastGPSStat == 5)
            {
                gpsFixText.color = new Color(255, 127, 51); //orange
                gpsFixText.text = "RTK Float";
            }
            else
            {
                gpsFixText.color = Color.green;
                gpsFixText.text = "RTK Fixed";
            }
        }
    }

    string GetMacAddress()
    {
        var macAdress = "";
        var nics = NetworkInterface.GetAllNetworkInterfaces();
        var i = 0;
        foreach (NetworkInterface adapter in nics)
        {
            PhysicalAddress address = adapter.GetPhysicalAddress();
            if (address.ToString() != "")
            {
                macAdress = address.ToString();
                return macAdress;
            }
        }
        return null;
    }


    bool shouldWeSetWorldOrigin(double x, double y, Vector3 ArCamPose)
    {
        double x_dis_utm_space = x - x_utm_origin;
        double y_dis_utm_space = y - y_utm_origin;

        Vector2 utm_dis = new Vector2((float)x_dis_utm_space, (float)y_dis_utm_space);
        Vector3 ar_dis3 = new Vector3(ArCamPose.x - ArCam_Origin.x, 0, ArCamPose.z - ArCam_Origin.z);

        ar_dis3 = Quaternion.AngleAxis(-current_north_fix, Vector3.up) * ar_dis3;

        Vector2 ar_dis = new Vector2(ar_dis3.x, ar_dis3.z);

        if (Vector2.Distance(utm_dis, ar_dis) > 1)
            return true;

        return false;
    }


    float current_north_fix = 0;
    public void SetWorldOrigin()
    {
        if (debugging)
        {
            x_utm_origin = 534892.65866935183;
            y_utm_origin = 5211821.44362808;

            WorldOrigin.transform.localPosition = arCam.transform.localPosition + new Vector3(0, -1.2f, 0);

            return;
        }

        OriginObjectHook.transform.parent = null;

        Quaternion camrot = arCam.transform.localRotation;
        Vector3 camposition = arCam.transform.localPosition;
        double x, y;
        int fix;
        string z;

        bool res;
        float correction = 0;
        bool was_gps = false;
        if (useGPSNorthing && (northingHandler.correctionsCount() > 500))
        {
            res = capsLoc.GetUTMPosition(out x, out y, out z, out fix);

            if (!res)
            {
                return;
            }

            correction = northingHandler.calculateCorrection();
            correction = correction * (-1);  //todo here -1 because in unity space left handed?
            was_gps = true;
        }
        else
        {
            Quaternion q;
            res = capsLoc.GetGyroQuaternion(out q);

            if (!res)
            {
                Debug.LogError("no quaternion");
                return;
            }

            //first move our world Origin to current ARCamera Tracking position(current GPS position = new Origin)
            //WorldOrigin.transform.localPosition = camposition;

            //correct so y = northing
            Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * camrot;
            Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

            //rotate to adjust northing (AR Camera = only local tracking = no real north)
            correction = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;
        }

        current_north_fix = correction;
        WorldOrigin.transform.localPosition = arCam.transform.localPosition + new Vector3(0, -1.2f, 0); //todo
        WorldOrigin.transform.localRotation = Quaternion.AngleAxis(correction, Vector3.up);
        slider.value = correction;
        x_utm_origin = myLastPose.x;
        y_utm_origin = myLastPose.y;
        ArCam_Origin = camposition;

        if (!debugging)
            File.AppendAllText(debugFile, x_utm_origin + ";" + y_utm_origin + ";" + camposition.ToString("F4") + ";" + correction.ToString("F4") + ";" + was_gps.ToString() + "\n"); ;

        OriginObjectHook.transform.parent = WorldOrigin.transform;
    }

    public void SliderChanged()
    {
        WorldOrigin.transform.localRotation = Quaternion.AngleAxis(slider.value, Vector3.up);
    }

    public void OnToggleChange()
    {
        useAltimeter = !useAltimeter;
        Debug.Log(" Now altimeter is " + useAltimeter);
    }
}
