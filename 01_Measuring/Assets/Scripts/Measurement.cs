using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Measurement : MonoBehaviour
{
    int m_id;
    GameObject m_gameObject;
    double m_utm_x;
    double m_utm_y;
    bool m_isSelected = false;
    Text m_listText = null;

    public Measurement(int id, GameObject m_gameObject, double utm_x, double utm_y)
    {
        m_id = id;
        this.m_gameObject = m_gameObject;
        m_utm_x = utm_x;
        m_utm_y = utm_y;
    }

    public string GetListText()
    {
        return "Measurement " + m_id.ToString() + ": " + m_utm_x.ToString("F3") + "; " + m_utm_y.ToString("F3");
    }

    public void Destroy()
    {
        Destroy(m_gameObject);
        Destroy(m_listText);
    }

    public void Select()
    {
        m_isSelected = !m_isSelected;
    }

    public void SetListText()
    {
        m_listText.text = GetListText();
        m_listText.name = m_id.ToString();
        if(m_isSelected)
            m_listText.color = new Color(1, 210f/255f, 95f/255f);
        else
            m_listText.color = new Color(1, 1, 1);
    }

    public bool IsSelected()
    {
        return m_isSelected;
    }

    public int ID() { return m_id; }

    public void SetText(Text t)
    {
        m_listText = t;
    }
    
    public Vector2 AsVector2()
    {
        return new Vector2((float)m_utm_x, (float)m_utm_y);
    }
}
