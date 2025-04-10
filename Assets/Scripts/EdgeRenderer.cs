using UnityEngine;

public class EdgeRenderer : MonoBehaviour
{
    [Header("Assign these in the Inspector")]
    public Transform pointA;            // Endpoint A
    public Transform pointB;            // Endpoint B
    public LineRenderer lineRenderer;   // The LineRenderer for drawing the edge

    [Header("Touch Settings")]
    public float touchThreshold;   // How close the finger must be to one of the segments to drag the middle point
    public float maxEdgeLengthStretch;   // If the touch gets further than this from A or B, reset the middle point
    private float maxEdgeLength;
    private float edgeLength;
    private Vector3 midPoint;          // Current position of the (invisible) middle point
    private bool dragging = false;      // Whether the user is currently dragging

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
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.white;

        // LineRenderer width
        lineRenderer.startWidth = 5.0f;  // Set the thickness at the start of the line
        lineRenderer.endWidth = 5.0f;
    }

    void Update()
    {
        // Mobile touch handling or mouse simulation for testing in the editor.
#if UNITY_EDITOR
        if (Input.GetMouseButton(0))
        {
            // Convert mouse position to world point.
            Vector3 screenPos = Input.mousePosition;
            // Ensure a proper z value based on your scene’s camera configuration.
            screenPos.z = 10f;  
            Vector3 touchPos = Camera.main.ScreenToWorldPoint(screenPos);

            ProcessTouch(touchPos);
        }
        else if (dragging)
        {
            ResetMiddlePoint();
        }
#else
    if (Input.touchCount > 0)  // Check if there is at least one touch
    {
        Touch touch = Input.GetTouch(0);

        // Convert touch position to world space. Adjust the z value as needed.
        Vector3 touchPos = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, 10f));

        ProcessTouch(touchPos);  // Process the touch position
    }
    else if (dragging)
    {
        ResetMiddlePoint();  // Reset if no touch is detected
    }
#endif
    }

    // Process the current touch (or mouse) position.
    void ProcessTouch(Vector3 touchPos)
    {
        // Calculate distances from the touch position to the two segments.
        float distSegment1 = DistancePointToLine(touchPos, pointA.position, midPoint);
        float distSegment2 = DistancePointToLine(touchPos, midPoint, pointB.position);
        // If the touch is close enough to either segment, update the middle point.
        if (distSegment1 < touchThreshold || distSegment2 < touchThreshold)
        {
            midPoint = touchPos;
            dragging = true;
            UpdateLine();
        }
        else if (dragging) { 
            // if the finger gets too far away while dragging: reset
            // should not occur but does
            ResetMiddlePoint();
        }
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
