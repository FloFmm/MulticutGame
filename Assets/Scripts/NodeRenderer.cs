using UnityEngine;
using System.Collections.Generic;

public class NodeRenderer : MonoBehaviour
{
    // public Node node;
    private SpriteRenderer spriteRenderer;
    private int connectedComponentId = -1;
    public int ConnectedComponentId
    {
        get => connectedComponentId;
        set
        {
            connectedComponentId = value;
            if (connectedComponentId >= 0 && connectedComponentId < GameData.ColorPalette.nodeColors.Count)
            {
                spriteRenderer.color = GameData.ColorPalette.nodeColors[connectedComponentId];
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
}
