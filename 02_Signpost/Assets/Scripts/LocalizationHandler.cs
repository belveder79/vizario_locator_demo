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
    private Locations locations = null;

    private bool lastMqttStat = false;
    private bool lastChipStat = false;

    public bool useGPSNorthing = true;
    public GameObject IMUVisualization = null;

    public Text mqttConnectionText = null;
    public Text chipConnectionText = null;
    public Text gpsFixText = null;

    private bool mapCreated = false;

    public GameObject arCam = null;
    public PlaceOnPlane placePlane = null;

    public GameObject prefabToPlace = null;
    public Material postMaterial = null;

    System.DateTime epochStart = new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
    private List<Measurement> placedObjcts = new List<Measurement>();


    // Start is called before the first frame update
    void Start()
    {
        //VizarioCapsLocInternal capsloc1 = VizarioCapsLocInternal.GetInstance();
        //if (capsloc1 != null)
        //    Debug.Log("here");


        if (placePlane == null) // ObjToVisualize == null ||
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

        locations = GameObject.Find("MapComponent").GetComponent<Locations>();

        if (locations == null)
        {
            Debug.LogError("Locations not in MapComponent, only important in Sign post!");

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

    public void ClearPlacedObjects()
    {
        foreach (var obj in placedObjcts)
        {
            obj.Destroy();
            //Destroy(obj);
        }
        placedObjcts.Clear();
    }

    private float angleFromCoordinates(float lat1, float lon1, float lat2, float lon2) {

        //todo need in rad ?

        float dLon = (lon2 - lon1);

        float y = Mathf.Sin(dLon) * Mathf.Cos(lat2);
        float x = Mathf.Cos(lat1) * Mathf.Sin(lat2) - Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(dLon);

        float brng = Mathf.Atan2(y, x);

        brng = brng * 180/Mathf.PI;
        brng = (brng + 360) % 360;
        brng = 360 - brng;  // count degrees clockwise - remove to make counter-clockwise

        if(brng < 0)
            Debug.LogError("should not be negative" );

        return brng;
    }

    private float distanceFromCoordinates(float lat1, float lon1, float lat2, float lon2)
    {

        lat1 = lat1 * Mathf.PI / 180;
        lon1 = lon1 * Mathf.PI / 180;
        lat2 = lat2 * Mathf.PI / 180;
        lon2 = lon2 * Mathf.PI / 180;

        // approximate radius of earth in km
        float R = 6373.0f;

        float dlon = lon2 - lon1;
        float dlat = lat2 - lat1;

        float a = Mathf.Pow(Mathf.Sin(dlat / 2), 2) + Mathf.Cos(lat1) * Mathf.Cos(lat2) * Mathf.Pow(Mathf.Sin(dlon / 2), 2);
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

        return R * c;
    }

    int signPlacedCounter = 0;
    public void AddSignPost()
    {
        if (capsLoc != null)
        {
            Vector3 origin; Pose PlanePose; Quaternion originRot;
            bool ret = placePlane.getRayHit(out origin, out PlanePose, out originRot);

            //if (!ret)
            //{
            //    Debug.Log("ray did not hit anything");
            //    return;
            //}


            PlanePose.position = new Vector3(1, 0, 1);
            double lat, lon;
            int fix;

            ret = capsLoc.GetLatLonPoition(out lat, out lon, out fix);

            //debug
            ret = true;
            lat = 47.05797695771782f;
            lon = 15.45748363236058f;

            if (!ret)
            {
                Debug.Log("no gps fix");
                return;
            }

            if (ret)
            {

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

                    //debug
                    ret = true;
                    q = Quaternion.AngleAxis(-120, Vector3.up);

                    if (!ret)
                    {
                        Debug.Log("no gyro fix");
                        return;
                    }

                    Quaternion camrot = arCam.transform.localRotation;

                    //correct so y = northing
                    Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * camrot;
                    Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * q;

                    //rotate to adjust northing (AR Camera = only local tracking = no real north)
                    correction = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;
                }

                relative_dis = Quaternion.Euler(0, -correction, 0) * relative_dis;   //minus for compass correction for sure

                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.transform.localScale = new Vector3(0.03f, 0.75f, 0.03f);
                post.transform.localPosition = PlanePose.position + new Vector3(0,0.75f,0);
                post.transform.localRotation = Quaternion.AngleAxis(correction, Vector3.up);

                if(postMaterial != null)
                {
                    MeshRenderer mr = post.GetComponent<MeshRenderer>();
                    mr.material = postMaterial;
                }

                List<Locations.Location> signs = locations.getRandomLocations(8);

                float[,] signAngles = new float[10, 2];
                for (int i = 0; i < 10; i++) { signAngles[i, 0] = -1; signAngles[i, 1] = -1; }

                var newObj = Instantiate(prefabToPlace, PlanePose.position, Quaternion.identity);
                newObj.transform.parent = post.transform;

                Vector3 p = new Vector3(0f, 0.95f, 5.0f);
                Quaternion rot = Quaternion.Euler(0, 0, 0);
                newObj.transform.localPosition = rot * p;
                newObj.transform.localRotation = rot;

                Text[] objTxt = newObj.GetComponentInChildren<Canvas>().GetComponentsInChildren<Text>();
                objTxt[0].text = " North";
                objTxt[1].text = " North";
                signAngles[0, 0] = 0;

                foreach (var loc in signs) {
                    float angle = angleFromCoordinates((float)lat, (float)lon, loc.Latitude, loc.Longitude);
                    float distance = distanceFromCoordinates((float)lat, (float)lon, loc.Latitude, loc.Longitude);

                    newObj = Instantiate(prefabToPlace, PlanePose.position, Quaternion.identity);
                    newObj.transform.parent = post.transform;

                    rot = Quaternion.Euler(0, angle, 0);
                    newObj.transform.localRotation = rot;

                    float signHeight = 0.95f;

                    for (int i = 0; i < 10; i++)
                    {
                        if(signAngles[i,0] == -1)
                        {
                            signAngles[i, 0] = angle;
                            break;
                        }
                        else if ((signAngles[i, 1] == -1) && (Mathf.Abs(Mathf.DeltaAngle(angle, signAngles[i, 0])) > 90f))
                        {
                            signAngles[i, 1] = angle;
                            break;
                        }

                        signHeight -= 0.1f;
                    }

                    p = new Vector3(0f, signHeight, 5.0f);
                    newObj.transform.localPosition = rot * p;

                    objTxt = newObj.GetComponentInChildren<Canvas>().GetComponentsInChildren<Text>();
                    objTxt[0].text = loc.Name + " " + distance.ToString("F2") + " km";
                    objTxt[1].text = loc.Name + " " + distance.ToString("F2") + " km";
                }

                placedObjcts.Add(new Measurement(++signPlacedCounter, post, 11, 11));
            }
        }
    }
}
