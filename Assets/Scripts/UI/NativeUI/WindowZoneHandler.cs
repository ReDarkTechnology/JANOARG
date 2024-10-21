using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WindowZoneHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public WindowZone ZoneType;

    public void OnPointerEnter(PointerEventData eventData)
    {
        BorderlessWindow.CurrentWindowZone = ZoneType;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        BorderlessWindow.CurrentWindowZone = WindowZone.Client;
    }
}
