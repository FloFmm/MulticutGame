using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class EdgeRenderer : MonoBehaviour
{
    public Transform pointA;            // Endpoint A
    public Transform pointB;            // Endpoint B
    public LineRenderer lineRenderer;   // The LineRenderer for drawing the edge
    public float touchThreshold;   // How close the finger must be to one of the segments to drag the middle point
    public float maxEdgeLengthStretch;   // If the touch gets further than this from A or B, reset the middle point
    private float edgeWidth;
    public List<int> availableCosts;
    public List<float> availableEdgeWidths;
    public List<Color> availableColors;
    public List<float> availablePitches;

    private int lineType;
    private CutPathManager cutPathManager;
    public GraphManager graphManager;
    private float maxEdgeLength;
    private float edgeLength;
    private Vector3 midPoint;          // Current position of the (invisible) middle point
    private bool dragging = false;      // Whether the user is currently dragging
    private List<Vector3> pathPositions;
    private float pitch;
    private int cost = 0;
    public int Cost
    {
        get => cost;
        set
        {
            cost = value;
            lineType = availableCosts.IndexOf(cost);
            if (lineType == -1)
            {
                lineType = 0;
            }
            lineRenderer.startWidth = availableEdgeWidths[lineType];
            lineRenderer.endWidth = availableEdgeWidths[lineType];
            lineRenderer.startColor = availableColors[lineType];
            lineRenderer.endColor = availableColors[lineType];
            pitch = availablePitches[lineType];
        }
    }
    private bool isCut = false;
    public bool IsCut
    {
        get => isCut;
        set
        {
            if (isCut != value)
                graphManager.updateScoreText(value, cost);
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
        GameObject pathGeneratorObject = GameObject.Find("InputManager");
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
    }

    void Update()
    {
        pathPositions = cutPathManager.GetPathPositions();
        // Debug.Log(pathPositions.Count);
        if (pathPositions.Count >= 2)
            ProcessTouch();
        else if (pathPositions.Count == 0 && dragging)
            ResetMiddlePoint();
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
            }
        }
    }

    // Reset the middle point to the center between A and B.
    void ResetMiddlePoint()
    {
        dragging = false;
        midPoint = (pointA.position + pointB.position) * 0.5f;
        GetComponent<SoundPlayer>().PlaySoundWithPitch(pitch);
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
