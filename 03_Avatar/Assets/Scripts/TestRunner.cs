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

        if(sr == null)
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
}
