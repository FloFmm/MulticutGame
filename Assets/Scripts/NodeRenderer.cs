using UnityEngine;
using System.Collections.Generic;

public class NodeRenderer : MonoBehaviour
{
    // public Node node;
    public List<Color> availableColors;
    private SpriteRenderer spriteRenderer;
    // private int lastComponentId = -1;
    private int connectedComponentId = -1;
    public int ConnectedComponentId
    {
        get => connectedComponentId;
        set
        {
            connectedComponentId = value;
            if (connectedComponentId >= 0 && connectedComponentId < availableColors.Count)
            {
                spriteRenderer.color = availableColors[connectedComponentId];
            }
            else
            {
                spriteRenderer.color = Color.green;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        // int currentId = node.ConnectedComponentId;
        // if (currentId != lastComponentId)
        // {
        //     spriteRenderer.color = availableColors[currentId];
        //     lastComponentId = currentId;
        // }
    }
}
