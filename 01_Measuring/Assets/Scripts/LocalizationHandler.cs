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
    private NorthingHandler northingHandler = null;

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
    public PlaceOnPlane placePlane = null;

    public GameObject prefabToPlace = null;

    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    private List<Measurement> placedObjcts = new List<Measurement>();

    // Start is called before the first frame update
    void Start()
    {
        //VizarioCapsLocInternal capsloc1 = VizarioCapsLocInternal.GetInstance();
        //if (capsloc1 != null)
        //    Debug.Log("here");


        if (WorldOrigin == null || placePlane == null) // ObjToVisualize == null ||
        {
            Debug.LogError("Objects for visualization not linked");
            return;
        }

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

        northingHandler = GameObject.Find("MapComponent").GetComponent<NorthingHandler>();

        if (northingHandler == null)
        {
            Debug.LogError("NorthingHandler not in MapComponent!");
            return;
        }

        if (textPrefap == null)
        {
            Debug.LogError("textPrefap not linked.");
        }

        listViewPanel = GameObject.Find("listPanel");
        buttonPanel = GameObject.Find("buttonPanel");
        textPanel = GameObject.Find("TextPanel");
        leftSpacingPanel = GameObject.Find("LeftSpacing");
        topSpacingPanel = GameObject.Find("TopSpacing");

        if (listViewPanel == null || buttonPanel == null || textPanel == null)
        {
            Debug.LogError("MeasuremetnsPanel not found");
        }
        else
        {
            listViewPanel.SetActive(false);
            buttonPanel.SetActive(false);
            textPanel.SetActive(false);
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

            double x, y, ts;
            int fix;
            string z;

            if(capsLoc.GetUTMPositionTs(out x, out y, out z, out fix, out ts))
            {
                HandleGPSUpdate(x, y, z, fix, ts);
            }

            //no need to call this every Update, also not needed for this example 
            //float alt, temp;
            //if(capsLoc.GetAltimeterValues(out alt, out temp))
            //{
            //    HandleAlitUpdate(alt, temp);
            //}
        }

        if (store_pos)
        {
            if (cam_posizions.Count > 60)
                cam_posizions.Clear();
            
            float ts = (int)(System.DateTime.UtcNow - epochStart).TotalMilliseconds;
            Vector3 camposition = arCam.transform.localPosition;
            cam_posizions.Add(new Tuple<float, Vector3>(ts, camposition));

        }
    }

    private void OnDestroy()
    {
        SaveFile();
    }

    private void HandleGyroUpdate(Quaternion quaternion)
    {
        if (IMUVisualization != null)
        {
            IMUVisualization.transform.localRotation = quaternion;
        }
    }


    string logging = "";
    bool store_pos = false;
    List<Tuple<float, Vector3>> cam_posizions = new List<Tuple<float, Vector3>>();
    private void HandleGPSUpdate(double x, double y, string z, int fixState, double ts_gps)
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
        if (useGPSNorthing && fixState == 4)
        {
            float ts = (int)(System.DateTime.UtcNow - epochStart).TotalMilliseconds;
            Vector3 camposition = arCam.transform.localPosition;

            store_pos = true;

            float last_ts = -99;
            Vector3 last_pos = new Vector3(0, 0, 0);
            float distance_ts = 99999999;
            int indx = -1;

            for (var i = cam_posizions.Count - 1; i >= 0; i--)
            {
                if (distance_ts > Mathf.Abs((float)(cam_posizions[i].Item1 - ts_gps)))
                {
                    distance_ts = Mathf.Abs((float)(cam_posizions[i].Item1 - ts_gps));
                    indx = i;
                }

            }

            logging += String.Format("{};{};{};{};{};{};{};{};\n", x, y, ts_gps, last_ts, last_ts, camposition, ts, cam_posizions.Count);

            NorthingHandler.PostionElement p;
            if (indx != -1)
                p = new NorthingHandler.PostionElement(ts, x, y, last_pos);
            else
                p = new NorthingHandler.PostionElement(ts, x, y, camposition);
            northingHandler.PushPosition(p);
        }

    }

    public void SaveFile()
    {

        string ts = String.Format("{}.txt", (int)(System.DateTime.UtcNow - epochStart).TotalMilliseconds);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, ts), logging);
        logging = "";
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


    private int measurementCounter = 0;
    //measure and add Object in scene
    public void TakeMeasurement()
    {
        if (capsLoc != null)
        {
            Vector3 origin; Pose PlanePose; Quaternion originRot;
            bool ret = placePlane.getRayHit(out origin, out PlanePose, out originRot);

            if (!ret)
            {
                Debug.Log("ray did not hit anything");
                return;
            }

            double x, y;
            string z;
            int fix;

            ret = capsLoc.GetUTMPosition(out x, out y, out z, out fix);

            if (!ret)
            {
                Debug.Log("no gps fix");
                return;
            }

            if (ret)
            {
                var newObj = Instantiate(prefabToPlace, PlanePose.position, Quaternion.identity);
                if (Mathf.Abs(PlanePose.rotation.eulerAngles.x - 5) <= 5)
                    newObj.transform.localRotation = Quaternion.Euler(0, originRot.eulerAngles.y, 0);
                else
                    newObj.transform.localRotation = PlanePose.rotation;

                Text objTxt = newObj.GetComponentInChildren<Canvas>().GetComponentInChildren<Text>();

                var relative_dis = PlanePose.position - origin; //vec from origin to plane


                float correction = 0;
                if (useGPSNorthing && (northingHandler.correctionsCount() > 500))  //GPS Northing is quite new, so we do not know the sweetspots of params atm
                {
                    correction = northingHandler.calculateCorrection();
                    correction = correction * (-1);

                    //debugging
                    Quaternion q;
                    ret = capsLoc.GetGyroQuaternion(out q);

                    if (!ret)
                        return;

                    //correct so y = northing
                    Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * originRot;
                    Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

                }
                else
                {
                    //roate relative distance Vector, since the calculation is in ARFoundation coord sys, which is not north orientated
                    //first move our world Origin to current ARCamera Tracking position(current GPS position = new Origin)
                    Quaternion q;
                    ret = capsLoc.GetGyroQuaternion(out q);

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

                relative_dis = Quaternion.Euler(0, -correction, 0) * relative_dis;   //minus for compass correction for sure

                double m_x = x + relative_dis.x;
                double m_y = y + relative_dis.z;

                newObj.name = "Measurement " + placedObjcts.Count.ToString();
                objTxt.text = "Measurement " + placedObjcts.Count.ToString() + "\nx: " + m_x.ToString("F3") + " m \ny: " + m_y.ToString("F3") + " m";

                newObj.transform.parent = WorldOrigin.transform;

                placedObjcts.Add(new Measurement(++measurementCounter, newObj, m_x, m_y));
            }
        }
    }

    public void ClearPlacedObjects()
    {
        foreach (var obj in placedObjcts)
        {
            obj.Destroy();
            //Destroy(obj);
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


    public void showListView()
    {
        if (listViewPanel == null || buttonPanel == null)
        {
            Debug.LogError("Object is null");
            return;
        }

        bool listEnabled = !listViewPanel.active;

        RectTransform parent = listViewPanel.GetComponent<RectTransform>();
        listViewPanel.SetActive(listEnabled);
        buttonPanel.SetActive(listEnabled);
        textPanel.SetActive(listEnabled);

        if (leftSpacingPanel != null && topSpacingPanel != null)
        {
            LayoutElement layoutLeft = leftSpacingPanel.GetComponent<LayoutElement>();
            LayoutElement layoutTop = topSpacingPanel.GetComponent<LayoutElement>();
            if (listEnabled)
            {
                layoutLeft.flexibleWidth = 81;
                layoutTop.flexibleHeight = 27;
            }
            else
            {
                layoutLeft.flexibleWidth = 75;
                layoutTop.flexibleHeight = 26;
            }
        }

        if (!listEnabled)
        {
            foreach (Transform child in parent)
            {
                Destroy(child.gameObject);
            }
            return;
        }


        for (int index = 0; index < placedObjcts.Count; ++index)
        {
            Text t = Instantiate(textPrefap);
            t.transform.parent = parent;

            placedObjcts[index].SetText(t);
            placedObjcts[index].SetListText();
        }
    }

    public void DeleteSelected()
    {
        foreach (var m in placedObjcts)
        {
            if (m.IsSelected()) {
                m.Destroy();
            }
        }

        placedObjcts.RemoveAll(item => item.IsSelected() == true);

        showListView();
        showListView();

    }

    public void CalculateDistance()
    {
        List<Measurement> toMeasure = new List<Measurement>();
        Text txt = textPanel.GetComponentInChildren<Text>();

        foreach (var m in placedObjcts)
        {
            if (m.IsSelected())
            {
                if(toMeasure.Count == 2)
                {
                    //todo set text to many
                    txt.text = "too many selections.";
                    return;
                }

                toMeasure.Add(m);
            }
        }

        if (toMeasure.Count != 2)
        {
            //todo set text to many
            txt.text = "too view selections.";
            return;
        }

        float distance = Vector2.Distance(toMeasure[0].AsVector2(), toMeasure[1].AsVector2());
        Debug.Log(distance);
        txt.text = toMeasure[0].ID().ToString() + " to " + toMeasure[1].ID().ToString() + " = " + distance.ToString("F3") + "m";
    }

    public void selectItem(int id)
    {
        foreach(var obj in placedObjcts)
        {
            if(obj.ID() == id)
            {
                obj.Select();
                obj.SetListText();
                return;
            }
        }
    }
}
