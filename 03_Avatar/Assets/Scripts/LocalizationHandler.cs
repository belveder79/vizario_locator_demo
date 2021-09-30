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
    }

    private VizarioCapsLocManager capsLoc = null;
    private NorthingHandler northingHandler = null;

    public string MqttServer = null;
    public int MqttPort = -1;
    private string myAvatarID;

    private bool lastMqttStat = false;
    private bool lastChipStat = false;

    public bool useGPSNorthing = true;

    public Text mqttConnectionText = null;
    public Text chipConnectionText = null;
    public Text gpsFixText = null;

    public GameObject arCam = null;
    public GameObject avatarPrefap = null;

    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    Dictionary<string, Avatar> avatars = new Dictionary<string, Avatar>();
    AvatarPose myLastPose = null;

    Queue<Action> runnerQ = new Queue<Action>();

    // Start is called before the first frame update
    void Start()
    {
        capsLoc = GameObject.Find("CapsLocRuntime").GetComponent<VizarioCapsLocManager>();

        if (capsLoc == null)
        {
            Debug.LogError("VizarioGPSBehaviour not in CapsLocRuntime!");          
        }

        northingHandler = GameObject.Find("AvatarSession").GetComponent<NorthingHandler>();

        if (northingHandler == null)
        {
            Debug.LogError("NorthingHandler not in AvatarSession!");
            //return;
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
        StartCoroutine(StartupMqtt());
    }

    IEnumerator StartupMqtt()
    {

        MQTTClient.SetHost(MqttServer, MqttPort);
        MQTTClient.SetClientID(myAvatarID);
        MQTTClient.StartClient();

        yield return new WaitForSeconds(1);

        MQTTClient.Subscribe("PoseUpdate");

        MQTTClient.RegisterCallback(MQTTNotify);
    }

    private string HandleAvatarPoseUpdate(string[] args)
    {
        var topic = args[0]; 
        var payload = args[1];


        AvatarPose p = JsonConvert.DeserializeObject<AvatarPose>(payload);

        if(p.ID != myAvatarID)
        {
            Debug.Log(payload);

            Avatar avatar = null;
            if (!avatars.ContainsKey(p.ID))
            {
                
                GameObject newPref = Instantiate(avatarPrefap, new Vector3(0,0,0), Quaternion.identity);
                Debug.Log("try create");
                newPref.name = p.ID;
                avatar = newPref.GetComponent<Avatar>();
                avatars.Add(p.ID, avatar);
                Debug.Log("new prefap created");
            }
            else
            {
                Debug.Log("try get avatar");
                avatars.TryGetValue(p.ID, out avatar);
            }

            avatar.setNewPosition(p, myLastPose, arCam.transform.localPosition);
        }


        return "";
    }

    // Update is called once per frame
    void Update()
    {
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

            //no need to call this every Update, also not needed for this example 
            float alt, temp;
            if (capsLoc.GetAltimeterValues(out alt, out temp))
            {
                //HandleAlitUpdate(alt, temp);
            }

            AvatarPose p = new AvatarPose(myAvatarID, x, y, alt, q);

            if(true)
            {
                p = new AvatarPose(myAvatarID, 1000, 60, 340, q);
            }

            myLastPose = p;
            var json = JsonConvert.SerializeObject(p);
            
            //Debug.Log(json.ToString());
            MQTTClient.Publish("PoseUpdate", json);
        }


        if (runnerQ.Count > 0)
        {
            var f = runnerQ.Dequeue();
            f?.Invoke();
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
}
