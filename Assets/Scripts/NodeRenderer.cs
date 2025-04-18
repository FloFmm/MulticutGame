using UnityEngine;
using System.Collections.Generic;

public class NodeRenderer : MonoBehaviour
{
    public List<Color> availableColors;
    private int connectedComponentId;
    public int ConnectedComponentId
    {
        get => connectedComponentId;
        set
        {
            connectedComponentId = value;
            if (connectedComponentId >= 0 && connectedComponentId < availableColors.Count)
            {
                GetComponent<SpriteRenderer>().color = availableColors[connectedComponentId];
            }
            else
            {
                GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
