using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUI : MonoBehaviour
{
    public Camera CamMap1 = null;
    public Camera CamMap2 = null;


    private VizarioGPS gps = null;

    public Text infoTxt = null;
    public InputField txtGyro = null;
    public InputField txtAlti = null;

    public GameObject btnAdd = null;
    public GameObject guiMenu = null;
    public GameObject btnClear = null;

    // Start is called before the first frame update
    void Start()
    {
        gps = GameObject.Find("Runtime").GetComponent<VizarioGPS>();

        if (gps == null)
        {
            Debug.LogError("VizarioGPSBehaviour not in Runtime!");
            return;
        }

        //here you can register for responses of the chip to check if settings where correctly set.
        gps.setResponseCallback(responseCallback);

        if(infoTxt == null || txtAlti == null || txtGyro == null || btnAdd == null || guiMenu == null || btnClear == null)
        {
            Debug.LogError("GUI Elements not linked!");
            return;
        }
    }

    //responses as json
    private string responseCallback(string[] arg)
    {
        //most messages like this: arg[0] topic; arg[1] msg
        //eg. msg: {"cmd":"cmd/1/sensor/gyroscope/power","rsp":"success"}
        infoTxt.text += "\n" + arg[1];

        return "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleValueChanged(Toggle change)
    {
        btnAdd.SetActive(change.isOn);
        btnClear.SetActive(change.isOn);
        guiMenu.SetActive(!change.isOn);
    }

    public void CameraSlider(float value)
    {
        value = (value * 20) + 20;

        Debug.Log(value);

        CamMap1.transform.localPosition = new Vector3(CamMap1.transform.localPosition.x, value, CamMap1.transform.localPosition.z);
        CamMap2.transform.localPosition = new Vector3(CamMap1.transform.localPosition.x, value, CamMap1.transform.localPosition.z);
    }


    // -------------------- Chip Settings can also be changed via the Unity API, here some examples
    // -------------------- Set Update Rate (Hz) of sensors (GPS always yields new pos when available) 
    public void SetGyroUpdateRate()
    {
        if(gps != null)
        {
            float.TryParse(txtGyro.text, out var value);

            if(value < 0)
            {
                infoTxt.text = "value must be positiv";
                return;
            }

            if (value > 120)
            {
                infoTxt.text = "update rate greater than 120 only possible via udp, only during calibration atm.";
                return;
            }

            gps.GyroUpdateRate(value);
            infoTxt.text = "set update rate to " + value;
        }
    }

    public void SetAltiUpdateRate()
    {
        if (gps != null)
        {
            float.TryParse(txtAlti.text, out var value);

            if (value < 0)
            {
                infoTxt.text = "value must be positiv";
                return;
            }

            if (value > 120)
            {
                infoTxt.text = "update rate greater than 120 only possible via udp, only during calibration atm.";
                return;
            }

            gps.AltiUpdateRate(value);
            infoTxt.text = "set Alti update rate to " + value;
        }
    }


    //------------------- Enable/Disable sensors 
    //here fixed, state can also be queried from chip (todo show in demo)
    private bool GyroState = true;
    public void SetGyroPower()
    {
        if(gps != null)
        {
            GyroState = !GyroState;
            gps.GyroPower(GyroState);
            infoTxt.text = "set Gyro Power to " + GyroState;
        }
    }

    private bool AltiState = true;
    public void SetAltiPower()
    {
        if (gps != null)
        {
            AltiState = !AltiState;
            gps.AltiPower(AltiState);
            infoTxt.text = "set Gyro Power to " + AltiState;
        }
    }

    private bool GPSState = true;
    public void SetGPSPower()
    {
        if (gps != null)
        {
            GPSState = !GPSState;
            gps.GPSPower(GPSState);
            infoTxt.text = "set Gyro Power to " + GPSState;
        }
    }

    public void SaveConfig()
    {
        if (gps != null)
        {
            //save current configuration on chip
            gps.SaveConfig();
            infoTxt.text = "save Config";
        }
    }

    public void ResetConfig()
    {
        if(gps != null)
        {
            infoTxt.text = "3 possible resets";

            // 0 = "hard"
            // 1 = "wifi"
            // 2 = "factory"
            gps.ResetConfig(0);
        }
    }

    //--------------------------- Set NTRIP Server  
    public void SetNTRIPServer()
    {
        if(gps != null)
        {
            bool enable = true;
            string server = "myServer";
            int port = 1883;
            string stream = "myStream";
            string username = "usr";
            string password = "pwd";

            infoTxt.text = "meaningless values atm. (todo)";

            //SetNTRIP(bool enable, String server, int port, String stream, String username, String password)
            gps.SetNTRIP(enable, server, port, stream, username, password);
        }
    }
}
