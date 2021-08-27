using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{

    private LocalizationHandler localizationHandler = null;

    private void Start()
    {
        localizationHandler = GameObject.Find("MapComponent").GetComponent<LocalizationHandler>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(localizationHandler == null)
        {
            Debug.LogError("no localization handler linked");
            return;
        }

        int id;

        if(int.TryParse(gameObject.name, out id))
        {
            localizationHandler.selectItem(id);
            return;
        }
        Debug.Log("clicked " + gameObject.name + " but not parsable");
    }
}
