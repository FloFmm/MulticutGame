using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro; 

public class EdgeRenderer : MonoBehaviour
{
    public Transform pointA;            // Endpoint A
    public Transform pointB;            // Endpoint B
    public LineRenderer lineRenderer;   // The LineRenderer for drawing the edge
    public float touchThreshold;   // How close the finger must be to one of the segments to drag the middle point
    // public float maxEdgeLengthStretch;   // If the touch gets further than this from A or B, reset the middle point
    public float cutDistance;
    public GameObject costNodePrefab;

    // cost Node
    private GameObject costNode;
    private TextMeshProUGUI costNodeText;
    private SpriteRenderer costNodeRenderer;

    private float edgeWidth;
    public GraphManager graphManager;
    private int lineType;
    private CutPathManager cutPathManager;
    private float edgeLength;
    private Vector3 midPoint;          // Current position of the (invisible) middle point
    private bool dragging = false;      // Whether the user is currently dragging
    private float pitch;
    private Edge edge;
    public Edge Edge
    {
        get => edge;
        set
        {
            edge = value;
            Cost = edge.Cost;
            IsCut = edge.IsCut;
            OptimalCut = edge.OptimalCut;
        }
    }
    public int Cost
    {
        get => edge.Cost;
        set
        {
            edge.Cost = value;
            lineType = GameData.edgeCosts.IndexOf(edge.Cost);
            if (lineType == -1)
            {
                lineType = 0;
            }
            lineRenderer.startWidth = GameData.edgeWidths[lineType];
            lineRenderer.endWidth = GameData.edgeWidths[lineType];
            lineRenderer.startColor = GameData.ColorPalette.edgeColors[lineType];
            lineRenderer.endColor = GameData.ColorPalette.edgeColors[lineType];
            pitch = GameData.edgePitches[lineType];

            // cost Node
            if (costNodeRenderer != null)
                costNodeRenderer.color = lineRenderer.startColor;
            if (costNodeText != null)
                costNodeText.text = $"{edge.Cost}";
        }
    }
    // private bool isCut = false;
    public bool IsCut
    {
        get => edge.IsCut;
        set
        {
            if (edge.IsCut != value)
            {
                edge.IsCut = value;
                graphManager.updateConnectedComponents(edge);
                graphManager.updateScoreText(value, edge.Cost);
            }
            Color color = lineRenderer.startColor;
            float alpha = edge.IsCut ? 0.05f : 1f; // transparent if cut
            color.a = alpha;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
        }
    }
    // private bool optimalCut = true;
    public bool OptimalCut
    {
        get => edge.OptimalCut;
        set
        {
            edge.OptimalCut = value;
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
        // maxEdgeLength = maxEdgeLengthStretch * Vector3.Distance(pointA.position, pointB.position);
        lineRenderer.positionCount = 3;
        lineRenderer.SetPosition(0, pointA.position);
        lineRenderer.SetPosition(1, midPoint);
        lineRenderer.SetPosition(2, pointB.position);

        // initialize cost Node
        costNode = Instantiate(costNodePrefab, midPoint, Quaternion.identity, graphManager.transform);
        costNodeRenderer = costNode.GetComponent<SpriteRenderer>();
        costNodeRenderer.color = lineRenderer.startColor;
        costNodeText = costNode.GetComponentInChildren<TextMeshProUGUI>();
        costNodeText.text = $"{-edge.Cost}";

        edgeLength = Vector3.Distance(pointA.position, midPoint) + Vector3.Distance(midPoint, pointB.position);
    }

    void Update()
    {
        if (dragging || IsCut || edgeLength < 100f)
            costNode.SetActive(false);
        else
            costNode.SetActive(true);

        // only display costNode if its not close to an edge
        if (costNode.activeInHierarchy)
        {
            foreach (GameObject other_edge in graphManager.getEdges())
            {
                if (gameObject != other_edge)
                {
                    float distance = GetPerpendicularDistance(
                        other_edge.GetComponent<EdgeRenderer>().pointA.position,
                        other_edge.GetComponent<EdgeRenderer>().pointB.position,
                        costNode.transform.position
                    );
                    if (distance <= 50)
                    {
                        costNode.SetActive(false);
                        break;
                    }
                }
            }
        }


        if (GameData.LastCutPathPositions.Count >= 2)
                ProcessTouch();
            else if (GameData.LastCutPathPositions.Count == 0 && dragging)
                ResetMiddlePoint();
    }

    float GetPerpendicularDistance(Vector3 pointA, Vector3 pointB, Vector3 point)
    {
        Vector3 AB = pointB - pointA;
        Vector3 AP = point - pointA;
        
        float magnitudeAB = AB.magnitude;
        if (magnitudeAB == 0f)
            return Vector3.Distance(pointA, point);  // A and B are the same point

        float t = Vector3.Dot(AP, AB) / (magnitudeAB * magnitudeAB);
        t = Mathf.Clamp01(t);  // clamp t to segment

        Vector3 closestPoint = pointA + t * AB;
        return Vector3.Distance(point, closestPoint);
    }

    // Process the current touch (or mouse) position.
    void ProcessTouch()
    {
        Vector2 lastPoint = GameData.LastCutPathPositions[GameData.LastCutPathPositions.Count - 1];
        Vector2 secondLastPoint = GameData.LastCutPathPositions[GameData.LastCutPathPositions.Count - 2];
        float distSegment1 = DistancePointToLine(lastPoint, pointA.position, midPoint);
        float distSegment2 = DistancePointToLine(lastPoint, midPoint, pointB.position);
        if (lineSegmentsIntersect(pointA.position, midPoint, secondLastPoint, lastPoint) || lineSegmentsIntersect(pointB.position, midPoint, secondLastPoint, lastPoint) || distSegment1 < touchThreshold || distSegment2 < touchThreshold)
        {
            if ((GameData.ScissorIsActive && !IsCut) || (!GameData.ScissorIsActive && IsCut))
            {
                midPoint = lastPoint;
                dragging = true;
                UpdateLine();
            }
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
            // edgeLength = Vector3.Distance(pointA.position, midPoint) + Vector3.Distance(midPoint, pointB.position);
            // if (edgeLength > maxEdgeLength)
            if (DistancePointToLine(midPoint, pointA.position, pointB.position) > cutDistance)
            {
                ResetMiddlePoint();
                GameData.LastCutEdges.Add(this.gameObject);
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
