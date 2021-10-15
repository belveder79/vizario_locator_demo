using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TestRunner : MonoBehaviour
{
    string debugFile = "debugFile.txt";
    StreamReader sr = null;

    public GameObject DebugCamera = null;
    // Start is called before the first frame update
    void Start()
    {

        string path = Path.Combine(Application.persistentDataPath, debugFile);
        sr = new StreamReader(path);

        //'{"ID":"020000000000","x":534706.528096143,"y":5211710.6930510988,"alt":285.1875,"q_x":0.2221793,"q_y":-0.247340277,"q_z":0.0261968821,"q_w":0.942752242}'
        rotCorrection(new Quaternion(0.2221793f, -0.247340277f, 0.0261968821f, 0.942752242f), new Quaternion(-0.3f, -0.1f, 0.0f, -1.0f));

        if (sr == null)
        {
            Debug.LogError("StreamReader null");
        }
    }

    // Update is called once per frame
    void Update()
    {
        NextUpdate();
    }

    public void NextUpdate()
    {
        if (sr != null)
        {
            string line = sr.ReadLine();

            Debug.Log(line);

            if (line == null)
                return;

            string[] splitted = line.Split(';');

            MQTTClient.Publish("PoseUpdate", splitted[0]);

            if (DebugCamera != null)
            {

                splitted[1] = splitted[1].Substring(1, splitted[1].Length - 2);
                string str_q = splitted[2].Substring(1, splitted[2].Length - 2);
                //Debug.Log(splitted[1]);

                splitted = splitted[1].Split(',');

                float x, y, z;

                float.TryParse(splitted[0], out x);
                float.TryParse(splitted[1], out y);
                float.TryParse(splitted[2], out z);

                //Debug.Log(x + ", " + y + ", " + z);

                DebugCamera.transform.localPosition = new Vector3(x, y, z);

                splitted = str_q.Split(',');

                float w;

                float.TryParse(splitted[0], out x);
                float.TryParse(splitted[1], out y);
                float.TryParse(splitted[2], out z);
                float.TryParse(splitted[3], out w);

                DebugCamera.transform.localRotation = new Quaternion(x, y, z, w);

            }
        }
    }

    public void rotCorrection(Quaternion mag, Quaternion arCam)
    {
        //correct so y = northing
        Quaternion arCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * arCam;
        Quaternion vizCorrected = Quaternion.FromToRotation(transform.up, Vector3.up) * mag;

        //rotate to adjust northing (AR Camera = only local tracking = no real north)
        float correction = arCorrected.eulerAngles.y - vizCorrected.eulerAngles.y;

        Debug.Log("correction: " + correction.ToString());
    }
}
