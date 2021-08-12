using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using Vizario;


public class MapCreator : MonoBehaviour
{
    public int mapScale = 300;
    private int sizeX = -1; 
    private int sizeY = -1;
    double leftBottom_x = -1f, leftBottom_y = -1f;
    double rightTop_x =-1f, rightTop_y = -1f;
    double center_x = -1f, center_y = -1f;
    private int mapObjY = 100;


    private float mapLength = 0;
    private float mapHeight = 0;
    private float planeLength = 0;
    private float planeHeight = 0;


    //private GameObject mapPlane = null;
    private Material frontPlane = null;
    private GameObject mapPlane = null;

    public GameObject avatarVizario = null;
    public GameObject avatarInternal = null;
    public bool handleOrientation = true;
    public bool useCallbacks = true;

    string m_Path;
    private bool map_created = false;

    // Start is called before the first frame update
    void Start()
    {
    }

    public void CreateMap(double minlat, double minlon, double maxlat, double maxlon)
    {
        StartCoroutine(SetUpMap(minlat, minlon, maxlat, maxlon));
    }


    
    IEnumerator SetUpMap(double minlat, double minlon, double maxlat, double maxlon)
    {

        string z;
        PositionConverter.LatLongtoUTM(minlat, minlon, out leftBottom_x, out leftBottom_y, out z);
        Debug.Log("x: " + leftBottom_x + " , y: " + leftBottom_y);
        PositionConverter.LatLongtoUTM(maxlat, maxlon, out rightTop_x, out rightTop_y, out z);
        center_x = leftBottom_x + (rightTop_x - leftBottom_x) / 2;
        center_y = leftBottom_y + (rightTop_y - leftBottom_y) / 2;

        string currentToken = "";
        string req_for_token = "https://www.openstreetmap.org"; //random url?
        UnityWebRequest www = UnityWebRequest.Get(req_for_token);
        yield return www.SendWebRequest();
        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
            Debug.Log("error in get token");
        }
        else
        {
            Debug.Log("POST successful!");
            StringBuilder sb = new StringBuilder();
            foreach (System.Collections.Generic.KeyValuePair<string, string> dict in www.GetResponseHeaders())
            {
                sb.Append(dict.Key).Append(": \t[").Append(dict.Value).Append("]\n");
            }

            // Print Headers
            //Debug.Log(sb.ToString());
            string tC = sb.ToString();
            var cc = tC.IndexOf("_osm_totp_token=");
            currentToken = tC.Substring(cc + "_osm_totp_token=".Length, 6);
            //string num = tC.Trim(, 2);
            Debug.Log(currentToken);
        }

        if(currentToken == "")
        {
            Debug.LogError("Failed to get token");
        }

        string req = string.Format("https://render.openstreetmap.org/cgi-bin/export?bbox={0},{1},{2},{3}&scale=737&format=png",  minlon, minlat, maxlon, maxlat);
        Debug.Log(req);
        www = UnityWebRequestTexture.GetTexture(req);
        www.SetRequestHeader("cookie", "_osm_totp_token="+currentToken);
        yield return www.SendWebRequest();

        if (www.isNetworkError || www.isHttpError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("got result");
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;

            if (mapPlane == null)
            {
                mapPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                mapPlane.transform.parent = this.transform;
                mapPlane.name = "Map Plane";
            }
            if (frontPlane == null)
            {
                frontPlane = new Material(Shader.Find("Unlit/Texture"));
            }

            sizeX = tex.width;
            sizeY = tex.height;

            Vector3 scale = new Vector3(sizeX / mapScale, 1, sizeY / mapScale);
            Debug.Log(scale);
            mapPlane.transform.localScale = scale;
            mapPlane.transform.localPosition = new Vector3(0, mapObjY, 0);

            // Image file exists - load bytes into texture

            frontPlane.mainTexture = tex;

            // Apply to Plane
            MeshRenderer mr = mapPlane.GetComponent<MeshRenderer>();
            mr.material = frontPlane;

            mapLength = (float)(leftBottom_x - rightTop_x);
            mapHeight = (float)(leftBottom_y - rightTop_y);
            planeLength = sizeX / mapScale;
            planeHeight = sizeY / mapScale;

            Debug.Log("map setted up");
            map_created = true;
            //setAvatarPosition(6.362731, 8.517936, 0);
            //setAvatarPositionUTM(534805.0, 5211784.0, "33", 0, 1);
        }
    }


    private void OnDestroy()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }


    int lastGPSStat = 0;
    public void setAvatarPositionUTM(double x, double y, String z, int fix, int avatarID)
    {
        //Debug.Log(" x: " + x + " y:" + y);

        if (!map_created)
            return;

        float x_ = (float) (x - center_x);
        float y_ = (float) (y - center_y);

        //Debug.Log(" x: " + x_ + " y:" + y_);

        x_ = x_ * (planeLength / mapLength);
        y_ = y_ * (planeHeight / mapHeight);

        //Debug.Log(" x: " + x_ + " y:" + y_);
        if(avatarID == 1)
            avatarVizario.transform.localPosition = new Vector3(x_ * 10, mapObjY + 1f, y_ * 10);  //10 times bc size of plane
        else
            avatarInternal.transform.localPosition = new Vector3(x_ * 10, mapObjY + 1f, y_ * 10);  //10 times bc size of plane
    }
}
