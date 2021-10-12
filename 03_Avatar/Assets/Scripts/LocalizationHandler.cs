﻿using Newtonsoft.Json;
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

    private bool useAltimeter = true;

    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    Dictionary<string, Avatar> avatars = new Dictionary<string, Avatar>();
    AvatarPose myLastPose = null;

    Queue<Action> runnerQ = new Queue<Action>();

    private double x_utm_origin = 534892.65866935183;
    private double y_utm_origin = 5211821.44362808;
    string debugFile;

    //debug
    public bool debugging = false;

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

        Debug.Log(payload);

        AvatarPose p = JsonConvert.DeserializeObject<AvatarPose>(payload);

        if(!debugging)
            File.AppendAllText(debugFile, payload + ";" + arCam.transform.localPosition + ";" + arCam.transform.localRotation);

        if(p.ID != myAvatarID)
        {
            //Debug.Log(payload);

            Avatar avatar = null;
            if (!avatars.ContainsKey(p.ID))
            {

                GameObject newPref = Instantiate(avatarPrefap, new Vector3(0,0,0), Quaternion.identity);
                Debug.Log("try create");
                newPref.name = p.ID;
                newPref.transform.parent = WorldOrigin.transform;
                avatar = newPref.GetComponent<Avatar>();
                avatars.Add(p.ID, avatar);
                Debug.Log("new prefap created");
            }
            else
            {
                Debug.Log("try get avatar");
                avatars.TryGetValue(p.ID, out avatar);
            }

            avatar.setNewPosition(p, x_utm_origin, y_utm_origin, MinRef);


            map.setAvatarPositionUTM(p.x, p.y, " ", 1, 2); //todo
        }
        else if(debugging)
        {
            myLastPose = p;

            HandleGPSUpdate(p.x, p.y, "bla" , 1);
        }

        return "";
    }


    bool MinRef = true;
    public void setMinRef() { MinRef = !MinRef; }

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

            if(capsLoc.GetUTMPosition(out x, out y, out z, out fix))
            {
                HandleGPSUpdate(x, y, z, fix);  //for northing
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

    private void HandleGPSUpdate(double x, double y, string z, int fixState)
    {

        setGPSFixText(fixState);
        //only use with RTK fixed positions!
        if (useGPSNorthing && fixState == 4)
        {
            float ts = (int)(System.DateTime.UtcNow - epochStart).TotalMilliseconds;
            Vector3 camposition = arCam.transform.localPosition;
            NorthingHandler.PostionElement p = new NorthingHandler.PostionElement(ts, x, y, camposition);
            northingHandler.PushPosition(p);
        }

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


    public void SetWorldOrigin()
    {


        if (debugging)
        {
            x_utm_origin = 534892.65866935183;
            y_utm_origin = 5211821.44362808;
            return;
        }

        Quaternion camrot = arCam.transform.localRotation;
        Vector3 camposition = arCam.transform.localPosition;
        double x, y;
        int fix;
        string z;

        bool res;
        float correction = 0;
        if (useGPSNorthing && (northingHandler.correctionsCount() > 500))
        {
            res = capsLoc.GetUTMPosition(out x, out y, out z, out fix);

            if (!res)
            {
                return;
            }

            correction = northingHandler.calculateCorrection();
            correction = correction * (-1);  //todo here -1 because in unity space left handed?
        }
        else
        {
            Quaternion q;
            res = capsLoc.GetGyroQuaternion(out q);

            if (!res)
                return;

            //first move our world Origin to current ARCamera Tracking position(current GPS position = new Origin)
            WorldOrigin.transform.localPosition = camposition;

            //correct so y = northing
            Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * camrot;
            Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

            //rotate to adjust northing (AR Camera = only local tracking = no real north)
            correction = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;
        }

        WorldOrigin.transform.localRotation = Quaternion.AngleAxis(correction, Vector3.up);
        WorldOrigin.transform.localPosition = arCam.transform.localPosition - new Vector3(0, -1.2f, 0); //todo 

        x_utm_origin = myLastPose.x;
        y_utm_origin = myLastPose.y;

    }


    public void OnToggleChange()
    {
        useAltimeter = !useAltimeter;
        Debug.Log(" Now altimeter is " + useAltimeter);
    }
}
