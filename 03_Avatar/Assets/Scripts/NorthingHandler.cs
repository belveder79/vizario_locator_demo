using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class NorthingHandler : MonoBehaviour
{
    string debugFile;

    public class PostionElement
    {
        public PostionElement(double ts, double ts_gps, double utm_x, double utm_y, Vector3 localPosition)
        {
            this.ts = ts;
            this.ts_gps = ts_gps;
            this.utmPosition = new Vector2((float)utm_x, (float)utm_y);
            this.localPosition = new Vector2(localPosition.x, localPosition.z);
        }

        public double ts { get; }

        public double ts_gps { get; }

        public Vector2 utmPosition { get; }

        public Vector2 localPosition { get; }

        public float DistanceUTM(Vector2 p)
        {
            return Vector2.Distance(p, this.utmPosition);
        }
    }

    private Queue<PostionElement> p_queue = new Queue<PostionElement>();

    private List<PostionElement> p_positions = new List<PostionElement>();
    private List<float> p_corrections = new List<float>();

    private double last_ts = 0;

    //Parameters
    public int max_corrections_len = 10000;
    public int max_copmareable_points = 300;
    public float min_distance = 0.5f;
    public float max_distance = 2f;
    public double max_time_offset = 20000; //ms


    //debugging
    //public Text correctionsTxt = null;

    // Start is called before the first frame update
    void Start()
    {
      debugFile = Path.Combine(Application.persistentDataPath, "debugFileGPSNorthing.txt");
      File.WriteAllText(debugFile, "");
    }

    // Update is called once per frame
    void Update()
    {
        if(p_queue.Count > 0)
        {
            HandlePoint(p_queue.Dequeue());
            //Debug.Log("queue count: " + p_queue.Count.ToString());
            ////debugging
            //if (p_queue.Count > 10)
            //    correctionsTxt.text += "queue to biiig";
        }
    }

    private void HandlePoint(PostionElement p)
    {
        last_ts = p.ts;
        p_positions.RemoveAll(TooOld);

        List<PostionElement> valid_positions = p_positions.FindAll(InDistance(p));

        foreach (var to_compair in valid_positions) {
            var utm_n = getAlpha(p.utmPosition, to_compair.utmPosition);
            var ar_n = getAlpha(p.localPosition, to_compair.localPosition);


            float corr = utm_n - ar_n;
            if (corr < 0)
                corr += 360;
            p_corrections.Add(corr);
        }

        p_positions.Add(p);
        if (p_positions.Count > max_copmareable_points)
            p_positions.RemoveRange(0, p_positions.Count - max_copmareable_points);

        if (p_corrections.Count > max_corrections_len)
        {
            //Debug.Log("Count corrections: " + p_corrections.Count.ToString());
            //correctionsTxt.text = "corrections: " + p_corrections.Count.ToString();
            p_corrections.RemoveRange(0, p_corrections.Count - max_corrections_len);
        }
    }

    private float getAlpha(Vector2 p1, Vector2 p2) {
        float c = Vector2.Distance(p1, p2);
        float x = Mathf.Abs(p1[0] - p2[0]);
        float y = Mathf.Abs(p1[1] - p2[1]);
        float x_ = (p2[0] - p1[0]);
        float y_ = (p2[1] - p1[1]);

        c = c + 0.000000001f;

        // check how triangle lines up
        if ((x_ >= 0) && (y_ >= 0))
            // x gegenkathete
            return Mathf.Asin(x / c) * 180 / Mathf.PI;
        if ((x_ >= 0) && (y_ < 0))
            return Mathf.Asin(y / c) * 180 / Mathf.PI + 90;
        if ((x_ < 0) && (y_ < 0))
            return Mathf.Asin(x / c) * 180 / Mathf.PI + 180;
        if ((x_ < 0) && (y_ >= 0))
            return Mathf.Asin(y / c) * 180 / Mathf.PI + 270;

        throw new Exception("should not get here");
    }

    public int correctionsCount()
    {
        return p_corrections.Count;
    }

    public float calculateCorrection()
    {
        List<float> sorted = new List<float>(p_corrections);
        sorted.Sort();
        int weg = (int) (p_corrections.Count * 0.15);
        float avg_corr_rad = sorted.GetRange(weg, p_corrections.Count - weg).Average();
        return avg_corr_rad;
    }

    private Predicate<PostionElement> InDistance(PostionElement mainPosition)
    {
        return delegate (PostionElement p)
        {
            float dis = mainPosition.DistanceUTM(p.utmPosition);
            return (min_distance < dis) && (dis < max_distance);
        };
    }

    private bool TooOld(PostionElement p)
    {
        return p.ts < (last_ts - max_time_offset);
    }

    public void PushPosition(PostionElement p) {

        File.AppendAllText(debugFile, p.utmPosition.ToString() + ";" + p.localPosition.ToString() + "\n");
        if (p_positions.Count == 0)
        {
            p_queue.Enqueue(p);
        }
        else if (p_positions[p_positions.Count - 1].DistanceUTM(p.utmPosition) > 0.1)
        {
            p_queue.Enqueue(p);
        }
    }
}
