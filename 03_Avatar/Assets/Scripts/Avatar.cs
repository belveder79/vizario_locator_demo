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

    public void setNewPosition(AvatarPose p, AvatarPose ploc, Vector3 arCamPos)
    {

        double x_dis = p.x - ploc.x;
        double y_dis = p.y - ploc.y;
        float alt_dis = p.alt - ploc.alt;

        //Debug.Log(x_dis + ", " + y_dis);
        alt_dis = 0;

        gameObject.transform.localPosition = new Vector3((float)x_dis, alt_dis, (float)y_dis) + arCamPos;
        gameObject.transform.localRotation = p.getQuaternion();
    }
}
