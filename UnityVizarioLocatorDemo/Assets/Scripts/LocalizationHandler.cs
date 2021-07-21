using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LocalizationHandler : MonoBehaviour
{

    private VizarioGPS gps = null;
    private MapCreator map = null;

    private bool lastMqttStat = false;
    private bool lastChipStat = false;


   
    public bool useCallback = true;
    public GameObject IMUVisualization = null;
    public Text mqttConnectionText = null;
    public Text chipConnectionText = null;
    public Text gpsFixText = null;

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
}
