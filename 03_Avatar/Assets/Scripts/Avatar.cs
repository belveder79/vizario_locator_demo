using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static LocalizationHandler;

public class Avatar : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void setNewPosition(AvatarPose p, double x_ref, double y_ref, float h_ref, bool use_height = true)
    {
        double x_dis = p.x - x_ref;
        double y_dis = p.y - y_ref;

        float alt_dis = p.alt - h_ref;
        //Debug.Log(x_dis + ", " + y_dis);
        //alt_dis = 0;
        if(use_height)
            gameObject.transform.localPosition = new Vector3((float)x_dis, alt_dis, (float)y_dis);
        else
            gameObject.transform.localPosition = new Vector3((float)x_dis, 0.03f, (float)y_dis);

        gameObject.transform.localRotation = p.getQuaternion();
    }
}
