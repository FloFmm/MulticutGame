using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class CutPathManager : MonoBehaviour
{
    public GraphManager graphManager;
    private LineRenderer lineRenderer;
    private Touchscreen touchscreen;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 0;
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
        if ((touchscreen != null && !touchscreen.primaryTouch.press.isPressed && GameData.LastCutPathPositions.Count > 0)) 
        {
            if (!graphManager.isValidMulticut())
            {
                // reset invalid cuts
                foreach (GameObject edgeObj in GameData.LastCutEdges)
                {
                    if (edgeObj != null)
                    {
                        // != null test is needed because of object destruction on scene change
                        EdgeRenderer edgeRenderer = edgeObj.GetComponent<EdgeRenderer>();
                        edgeRenderer.IsCut = !edgeRenderer.IsCut;
                    }
                }
            }
            GameData.LastCutPathPositions.Clear();  // Reset the path for the next input
            GameData.LastCutEdges.Clear();  // Reset the path for the next input
            lineRenderer.positionCount = 0;  // Optionally clear the line
        }
    }

    private void HandleTouchInput()
    {
        Vector2 touchPosition = touchscreen.primaryTouch.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, 10f));

        if (GameData.LastCutPathPositions.Count == 0 || GameData.LastCutPathPositions[GameData.LastCutPathPositions.Count - 1] != worldPos)
            GameData.LastCutPathPositions.Add(worldPos);

        lineRenderer.positionCount = GameData.LastCutPathPositions.Count;
        lineRenderer.SetPositions(GameData.LastCutPathPositions.ToArray());
    }

    private void HandleMouseInput()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, 10f));

        if (GameData.LastCutPathPositions.Count == 0 || GameData.LastCutPathPositions[GameData.LastCutPathPositions.Count - 1] != worldPos)
            GameData.LastCutPathPositions.Add(worldPos);

        lineRenderer.positionCount = GameData.LastCutPathPositions.Count;
        lineRenderer.SetPositions(GameData.LastCutPathPositions.ToArray());
    }
}
