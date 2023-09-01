using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Vizario;

public class LoggerScript : MonoBehaviour
{
    private VizarioCapsLocManager capsLoc = null;
    public Text mqttConnectionText = null;
    public Text chipConnectionText = null;
    public Text gpsFixText = null;

    private bool lastMqttStat = false;
    private bool lastChipStat = false;

    // Start is called before the first frame update
    void Start()
    {
        capsLoc = GameObject.Find("CapsLocRuntime").GetComponent<VizarioCapsLocManager>();

        if (capsLoc == null)
        {
            Debug.LogError("VizarioGPSBehaviour not in CapsLocRuntime!");
        }
        else
        {
            
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
    }

    // Update is called once per frame
    void Update()
    {
        if (capsLoc == null)
            capsLoc = GameObject.Find("CapsLocRuntime").GetComponent<VizarioCapsLocManager>();
    }

    private void HandleGPSUpdate(double x, double y, string z, int fixState)
    {

        capsLoc.GetLatLonPoition(out var lat, out var lon, out int state);
        setGPSFixText(fixState);
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
