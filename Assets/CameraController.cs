using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 1f;
    public float minZoom = 1f;
    public float maxZoom = 100f;

    private Camera cam;
    private Vector3 dragOrigin;
    private bool isDragging = false;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        HandleDragging();
        HandleZooming();
    }

    void HandleDragging()
    {
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isDragging)
        {
            Vector3 difference = dragOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
            cam.transform.position += difference;
        }
    }

    void HandleZooming()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            float newSize = cam.orthographicSize - scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
}