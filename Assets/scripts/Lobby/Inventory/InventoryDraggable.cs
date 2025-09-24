using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableInventory : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    public Canvas canvas;
    public RectTransform headerZone; // Assign the header panel in the inspector
    private RectTransform rectTransform;
    private bool isDraggingFromHeader = false;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        // If headerZone is not assigned, try to find it automatically
        if (headerZone == null)
        {
            // Look for a child object named "Header" or similar
            headerZone = transform.Find("Header") as RectTransform;
        }
    }

    // This method is called when clicking on the header
    public void OnPointerDown(PointerEventData eventData)
    {
        // Check if the click is within the header zone
        if (headerZone != null && RectTransformUtility.RectangleContainsScreenPoint(headerZone, eventData.position, eventData.pressEventCamera))
        {
            isDraggingFromHeader = true;
        }
        else
        {
            isDraggingFromHeader = false;
        }
    }

    // This method is called when dragging
    public void OnDrag(PointerEventData eventData)
    {
        // Only drag if we started dragging from the header
        if (isDraggingFromHeader)
        {
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        }
    }
}