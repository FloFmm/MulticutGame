using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class EdgeRenderer : MonoBehaviour
{
    [Header("Assign these in the Inspector")]
    public Transform pointA;            // Endpoint A
    public Transform pointB;            // Endpoint B
    public LineRenderer lineRenderer;   // The LineRenderer for drawing the edge
    private CutPathManager cutPathManager;

    [Header("Touch Settings")]
    public float touchThreshold;   // How close the finger must be to one of the segments to drag the middle point
    public float maxEdgeLengthStretch;   // If the touch gets further than this from A or B, reset the middle point
    private float maxEdgeLength;
    private float edgeLength;
    private Vector3 midPoint;          // Current position of the (invisible) middle point
    private bool dragging = false;      // Whether the user is currently dragging
    private List<Vector3> pathPositions;
    private int cost = 0;

    private static readonly List<Color> costColors = new()
    {
        Color.white,                                      // 0
        Color.green,                                      // 1
        Color.Lerp(Color.green, Color.cyan, 0.5f),        // 2
        Color.cyan,                                       // 3
        Color.Lerp(Color.cyan, Color.blue, 0.5f),         // 4
        Color.blue,                                       // 5
        Color.Lerp(Color.blue, Color.magenta, 0.5f),      // 6
        Color.magenta,                                    // 7
        Color.Lerp(Color.magenta, Color.red, 0.33f),      // 8
        Color.Lerp(Color.magenta, Color.red, 0.66f),      // 9
        Color.red,                                        // 10
        Color.Lerp(Color.red, Color.black, 0.5f),         // 11
        Color.black                                       // 12
    };
    public int Cost
    {
        get => cost;
        set
        {
            cost = value;
            lineRenderer.startWidth = Mathf.Clamp(4f + (cost + 5)/10f * 8f, 4f, 12f);
            lineRenderer.endWidth = Mathf.Clamp(4f + (cost + 5)/10f * 8f, 4f, 12f);
            // Clamp the cost to the color list range
            int colorIndex = Mathf.Clamp(cost+6, 0, costColors.Count - 1);
            Color color = costColors[colorIndex];
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }

    private bool isCut = false;
    public bool IsCut
    {
        get => isCut;
        set
        {
            isCut = value;
            Color color = lineRenderer.startColor;
            float alpha = isCut ? 0.3f : 1f; // half-transparent if cut
            color.a = alpha;

            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }

    private bool optimalCut = true;
    public bool OptimalCut
    {
        get => optimalCut;
        set
        {
            optimalCut = value;
        }
    }
    
    

    void Awake()
    {
        // get the cutPathM
        GameObject pathGeneratorObject = GameObject.Find("inputManager");
        cutPathManager = pathGeneratorObject.GetComponent<CutPathManager>();
    }

    void Start()
    {
        // Initialize the middle point at the center between A and B.
        midPoint = (pointA.position + pointB.position) * 0.5f;
        maxEdgeLength = maxEdgeLengthStretch * Vector3.Distance(pointA.position, pointB.position);
        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, pointA.position);
        lineRenderer.SetPosition(1, midPoint);
        lineRenderer.SetPosition(2, pointB.position);

        // LineRenderer color 
        // lineRenderer.startColor = Color.white;
        // lineRenderer.endColor = Color.white;

        // LineRenderer width
        // lineRenderer.startWidth = 5.0f;  // Set the thickness at the start of the line
        // lineRenderer.endWidth = 5.0f;
    }

    void Update()
    {
        pathPositions = cutPathManager.GetPathPositions();
        // Debug.Log(pathPositions.Count);
        if (pathPositions.Count >= 2)
            ProcessTouch();
        else if (pathPositions.Count == 0 && dragging)
            ResetMiddlePoint();
        // Mobile touch handling or mouse simulation for testing in the editor.
        // #if UNITY_EDITOR
        //     if (Mouse.current.leftButton.isPressed)
        //     {
        //         Vector2 screenPos = Mouse.current.position.ReadValue();
        //         screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height); // Safety clamp
        //         Vector3 touchPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        //         ProcessTouch(touchPos);
        //     }
        //     else if (dragging)
        //     {
        //         ResetMiddlePoint();
        //     }
        // #else
        //     if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        //     {
        //         Vector2 screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        //         screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height); // Safety clamp
        //         Vector3 touchPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
        //         ProcessTouch(touchPos);
        //     }
        //     else if (dragging)
        //     {
        //         ResetMiddlePoint();
        //     }
        // #endif
    }

    // Process the current touch (or mouse) position.
    void ProcessTouch()
    {
        Vector2 lastPoint = pathPositions[pathPositions.Count - 1];
        Vector2 secondLastPoint = pathPositions[pathPositions.Count - 2];
        float distSegment1 = DistancePointToLine(lastPoint, pointA.position, midPoint);
        float distSegment2 = DistancePointToLine(lastPoint, midPoint, pointB.position);
        if (lineSegmentsIntersect(pointA.position, midPoint, secondLastPoint, lastPoint) || lineSegmentsIntersect(pointB.position, midPoint, secondLastPoint, lastPoint) || distSegment1 < touchThreshold || distSegment2 < touchThreshold)
        {
            midPoint = lastPoint;
            dragging = true;
            UpdateLine();
        }
        else if (dragging)
        {
            // if the finger gets too far away while dragging: reset
            // should not occur but does
            ResetMiddlePoint();
        }
        // Calculate distances from the touch position to the two segments
        // float distSegment1 = DistancePointToLine(lastPoint, pointA.position, midPoint);
        // float distSegment2 = DistancePointToLine(lastPoint, midPoint, pointB.position);
        // // If the touch is close enough to either segment, update the middle point.
        // float maxDist = touchThreshold;
        // if (dragging)
        //     maxDist *= 3;
        // if (distSegment1 < touchThreshold || distSegment2 < touchThreshold)
        // {
        //     midPoint = lastPoint;
        //     dragging = true;
        //     UpdateLine();
        // }
        // else if (dragging)
        // {
        //     // if the finger gets too far away while dragging: reset
        //     // should not occur but does
        //     ResetMiddlePoint();
        // }
    }

    public static bool lineSegmentsIntersect(Vector2 lineOneA, Vector2 lineOneB, Vector2 lineTwoA, Vector2 lineTwoB)
    {
        return (((lineTwoB.y - lineOneA.y) * (lineTwoA.x - lineOneA.x) > (lineTwoA.y - lineOneA.y) * (lineTwoB.x - lineOneA.x)) != ((lineTwoB.y - lineOneB.y) * (lineTwoA.x - lineOneB.x) > (lineTwoA.y - lineOneB.y) * (lineTwoB.x - lineOneB.x)) && ((lineTwoA.y - lineOneA.y) * (lineOneB.x - lineOneA.x) > (lineOneB.y - lineOneA.y) * (lineTwoA.x - lineOneA.x)) != ((lineTwoB.y - lineOneA.y) * (lineOneB.x - lineOneA.x) > (lineOneB.y - lineOneA.y) * (lineTwoB.x - lineOneA.x)));
    }

    // Update the positions on the LineRenderer.
    void UpdateLine()
    {

        lineRenderer.SetPosition(1, midPoint);
        if (dragging) // if the touch is far from either endpoint, reset the middle point.
        {
            edgeLength = Vector3.Distance(pointA.position, midPoint) + Vector3.Distance(midPoint, pointB.position);
            if (edgeLength > maxEdgeLength)
            {
                ResetMiddlePoint();
                IsCut = !IsCut;
                // if (IsCut) {
                //     lineRenderer.startColor = Color.black;
                //     lineRenderer.endColor = Color.black;
                // }
                // else 
                // {
                //     lineRenderer.startColor = Color.white;
                //     lineRenderer.endColor = Color.white;
                // }
            }
        }
    }

    // Reset the middle point to the center between A and B.
    void ResetMiddlePoint()
    {
        dragging = false;
        midPoint = (pointA.position + pointB.position) * 0.5f;
        UpdateLine();
    }

    // Helper function: returns the distance from a point to the closest point on the line segment.
    float DistancePointToLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
    {
        Vector3 lineDir = lineEnd - lineStart;
        float lineLen = lineDir.magnitude;

        // Avoid division by zero if the line is extremely short.
        if (lineLen == 0f)
            return Vector3.Distance(point, lineStart);

        // Find the projection of 'point' onto the line defined by lineStart and lineEnd.
        float t = Mathf.Clamp01(Vector3.Dot(point - lineStart, lineDir) / (lineLen * lineLen));
        Vector3 projection = lineStart + t * lineDir;
        return Vector3.Distance(point, projection);
    }
}
