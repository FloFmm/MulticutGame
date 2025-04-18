using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CutPathManager : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private List<Vector3> pathPositions;
    private Touchscreen touchscreen;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
        pathPositions = new List<Vector3>();
        touchscreen = Touchscreen.current;
        lineRenderer.startWidth = 5.0f;  // Set the thickness at the start of the line
        lineRenderer.endWidth = 5.0f;
    }

    void Update()
    {
        // Check for touch input if on an actual touchscreen
        if (touchscreen != null && touchscreen.primaryTouch.press.isPressed)
        {
            HandleTouchInput();
        }

        // Optionally handle touch/mouse end
        if ((touchscreen != null && !touchscreen.primaryTouch.press.isPressed && pathPositions.Count > 0)) 
        {
            pathPositions.Clear();  // Reset the path for the next input
            lineRenderer.positionCount = 0;  // Optionally clear the line
        }
    }

    private void HandleTouchInput()
    {
        Vector2 touchPosition = touchscreen.primaryTouch.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, 10f));

        if (pathPositions.Count == 0 || pathPositions[pathPositions.Count - 1] != worldPos)
            pathPositions.Add(worldPos);

        lineRenderer.positionCount = pathPositions.Count;
        lineRenderer.SetPositions(pathPositions.ToArray());
    }

    private void HandleMouseInput()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10f));

        if (pathPositions.Count == 0 || pathPositions[pathPositions.Count - 1] != worldPos)
            pathPositions.Add(worldPos);

        lineRenderer.positionCount = pathPositions.Count;
        lineRenderer.SetPositions(pathPositions.ToArray());
    }

    public List<Vector3> GetPathPositions()
    {
        return pathPositions;
    }
}
