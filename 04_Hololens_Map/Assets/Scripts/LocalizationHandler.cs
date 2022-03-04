using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Vizario;

public class LocalizationHandler : MonoBehaviour
{

    private VizarioCapsLocManager capsLoc = null;
    private MapCreator map = null;
    //private NorthingHandler northingHandler = null;

    private bool lastMqttStat = false;
    private bool lastChipStat = false;

    public bool useGPSNorthing = true;
    public GameObject IMUVisualization = null;

    public Text mqttConnectionText = null;
    public Text chipConnectionText = null;
    public Text gpsFixText = null;

    private bool mapCreated = false;

    private GameObject listViewPanel = null;
    private GameObject buttonPanel = null;
    private GameObject textPanel = null;
    private GameObject leftSpacingPanel = null;
    private GameObject topSpacingPanel = null;

    public Text textPrefap = null;

    //visualize object
    public GameObject WorldOrigin = null;
    //example how to add pre external obj with utm coords into scene
    //IF 11 534753,313	5211701,173
    //public double ObjUtmX = 0;
    //public double ObjUtmY = 0;

    public GameObject arCam = null;
    //public PlaceOnPlane placePlane = null;

    public GameObject prefabToPlace = null;

    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    //private List<Measurement> placedObjcts = new List<Measurement>();

    // Start is called before the first frame update
    void Start()
    {
        //VizarioCapsLocInternal capsloc1 = VizarioCapsLocInternal.GetInstance();
        //if (capsloc1 != null)
        //    Debug.Log("here");


        //if (WorldOrigin == null || placePlane == null) // ObjToVisualize == null ||
        //{
        //    Debug.LogError("Objects for visualization not linked");
        //    return;
        //}

        capsLoc = GameObject.Find("CapsLocRuntime").GetComponent<VizarioCapsLocManager>();

        if (capsLoc == null)
        {
            Debug.LogError("VizarioGPSBehaviour not in CapsLocRuntime!");          
        }

        map = GameObject.Find("MapComponent").GetComponent<MapCreator>();

        if (map == null)
        {
            Debug.LogError("MapCreator not in MapComponent!");
            return;
        }

        //northingHandler = GameObject.Find("MapComponent").GetComponent<NorthingHandler>();

        //if (northingHandler == null)
        //{
        //    Debug.LogError("NorthingHandler not in MapComponent!");
        //    return;
        //}

        if (textPrefap == null)
        {
            Debug.LogError("textPrefap not linked.");
        }

        //listViewPanel = GameObject.Find("listPanel");
        //buttonPanel = GameObject.Find("buttonPanel");
        //textPanel = GameObject.Find("TextPanel");
        //leftSpacingPanel = GameObject.Find("LeftSpacing");
        //topSpacingPanel = GameObject.Find("TopSpacing");

        //if (listViewPanel == null || buttonPanel == null || textPanel == null)
        //{
        //    Debug.LogError("MeasuremetnsPanel not found");
        //}
        //else
        //{
        //    listViewPanel.SetActive(false);
        //    buttonPanel.SetActive(false);
        //    textPanel.SetActive(false);
        //}



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
            //Quaternion q;
            //if (capsLoc.GetGyroQuaternion(out q))
            //{
            //    HandleGyroUpdate(q);
            //}

            double x, y;
            int fix;
            string z;

            if(capsLoc.GetUTMPosition(out x, out y, out z, out fix))
            {
                HandleGPSUpdate(x, y, z, fix);
            }

            //no need to call this every Update, also not needed for this example 
            //float alt, temp;
            //if(capsLoc.GetAltimeterValues(out alt, out temp))
            //{
            //    HandleAlitUpdate(alt, temp);
            //}
        }
    }

    private void OnDestroy()
    {

    }

    private void HandleGyroUpdate(Quaternion quaternion)
    {
        if (IMUVisualization != null)
        {
            IMUVisualization.transform.localRotation = quaternion;
        }
    }

    private void HandleGPSUpdate(double x, double y, string z, int fixState)
    {
        if (!mapCreated) {


            capsLoc.GetLatLonPoition(out var lat, out var lon, out int state);

            map.CreateMap(lat - 0.001, lon - 0.002, lat + 0.001, lon + 0.002);

            mapCreated = true;
            Debug.Log("set map coords.");
        }

        map.setAvatarPositionUTM(x, y, z, fixState, 1);
        setGPSFixText(fixState);

        //only use with RTK fixed positions!
        //if (useGPSNorthing && fixState == 4)
        //{
        //    float ts = (int)(System.DateTime.UtcNow - epochStart).TotalMilliseconds;
        //    Vector3 camposition = arCam.transform.localPosition;
        //    NorthingHandler.PostionElement p = new NorthingHandler.PostionElement(ts, x, y, camposition);
        //    northingHandler.PushPosition(p);
        //}

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
}
