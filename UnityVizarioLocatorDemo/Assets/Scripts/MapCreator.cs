﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using Vizario;

[Serializable]
public class MapData
{
    public float left { get; set; }
    public float right { get; set; }
    public float top { get; set; }
    public float bottom { get; set; }
}

[Serializable]
public class LogData
{
    public int mIdx;
    public double lat;
    public double lon;
    public float x_nulled;
    public float y_nulled;
    public int fix;
    public Quaternion imu_values;
}

public class MapCreator : MonoBehaviour
{

    public bool copyMapFromResources = true;

    private string mapFile = "img_map_tu.png";
    private string mapXML = "xml_map_tu.xml";
    public int mapScale = 300;
    private int sizeX = -1; 
    private int sizeY = -1;
    MapData mapData = null;
    double leftBottom_x = -1f, leftBottom_y = -1f;
    double rightTop_x =-1f, rightTop_y = -1f;
    double center_x = -1f, center_y = -1f;


    private Vector3 currentPosition = new Vector3(0, 0, 0);

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
        return;

        m_Path = Path.Combine(Application.persistentDataPath, "maps");
        Debug.Log(m_Path);

        //do on first run at least
        if (copyMapFromResources)
        {
            TextAsset xmlAsset = (TextAsset) Resources.Load(Path.Combine("maps", System.IO.Path.GetFileNameWithoutExtension(mapXML)));


            if (!Directory.Exists(m_Path))
                Directory.CreateDirectory(m_Path);

            string xmlFilePath = Path.Combine(m_Path, mapXML);
            Debug.Log(xmlFilePath);
            if (File.Exists(xmlFilePath))
                File.Delete(xmlFilePath);

            File.WriteAllText(xmlFilePath, xmlAsset.text);
            Debug.Log("xml copied");

            string imgFilePath = Path.Combine(m_Path, mapFile);
            if (File.Exists(imgFilePath))
                File.Delete(imgFilePath);

            //if null pointer, make sure Texture type is 2D !
            Texture2D texture =  Resources.Load(Path.Combine("maps", System.IO.Path.GetFileNameWithoutExtension(mapFile))) as Texture2D;

            byte[] bytes = texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(imgFilePath, bytes);
            Debug.Log("img copied");

        }

        

        //Output the Game data path to the console
        Debug.Log("dataPath : " + m_Path);

        string filePath = Path.Combine(m_Path, mapXML);
        if (System.IO.File.Exists(filePath))
        {
            //System.IO.FileStream file = System.IO.StreamReader(filePath);
            System.IO.StreamReader file = new System.IO.StreamReader(filePath);
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(MapData));
            mapData = (MapData)xmlSerializer.Deserialize(file);

            string z;
            PositionConverter.LatLongtoUTM(mapData.bottom, mapData.left, out leftBottom_x, out leftBottom_y, out z);
            Debug.Log("x: " + leftBottom_x + " , y: " + leftBottom_y);
            PositionConverter.LatLongtoUTM(mapData.top, mapData.right, out rightTop_x, out rightTop_y, out z);
            center_x = leftBottom_x + (rightTop_x - leftBottom_x) / 2;
            center_y = leftBottom_y + (rightTop_y - leftBottom_y) / 2;
        }
        else
        {
            Debug.LogError("no file found : " + filePath);
        }

        filePath = Path.Combine(m_Path, mapFile);

        if (System.IO.File.Exists(filePath) && mapData != null)
        {
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
            var bytes = System.IO.File.ReadAllBytes(filePath);
            var tex = new Texture2D(1, 1);
            tex.LoadImage(bytes);
            sizeX = tex.width;
            sizeY = tex.height;

            Vector3 scale = new Vector3(sizeX / mapScale, 1, sizeY / mapScale);
            Debug.Log(scale);
            mapPlane.transform.localScale = scale;

            // Image file exists - load bytes into texture
            
            frontPlane.mainTexture = tex;

            // Apply to Plane
            MeshRenderer mr = mapPlane.GetComponent<MeshRenderer>();
            mr.material = frontPlane;

            mapLength = (float) (leftBottom_x - rightTop_x);
            mapHeight = (float)(leftBottom_y - rightTop_y);
            planeLength = sizeX / mapScale;
            planeHeight = sizeY / mapScale;           

            Debug.Log("map setted up");

            //setAvatarPosition(6.362731, 8.517936, 0);
            setAvatarPositionUTM(534805.0, 5211784.0,"33", 0, 1);
            //double lat, lon;
            //int state;
            //PositionConverter.ParseNMEA("$GNGGA,135434.148,0001.377,N,00844.048,E,1,12,1.0,0.0,M,0.0,M,,*62", out lat, out lon, out state);


            double x, y;
            string z;

            PositionConverter.LatLongtoUTM(55.687957f, 12.544012f, out x, out y, out z);

            Debug.Log("utm: " + z + " " + x + ", " + y);
        }
        else
        {
            Debug.Log("no file found : " + filePath);
        }

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

        string req = string.Format("https://render.openstreetmap.org/cgi-bin/export?bbox={0},{1},{2},{3}&scale=737&format=png",  minlon, minlat, maxlon, maxlat);
        Debug.Log(req);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(req);
        www.SetRequestHeader("cookie", "_osm_totp_token=272110");
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
            setAvatarPositionUTM(534805.0, 5211784.0, "33", 0, 1);
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
            avatarVizario.transform.localPosition = new Vector3(x_ * 10, 0.5f, y_ * 10);  //10 times bc size of plane
        else
            avatarInternal.transform.localPosition = new Vector3(x_ * 10, 0.5f, y_ * 10);  //10 times bc size of plane
    }
}