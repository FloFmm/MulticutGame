using System.IO;
using UnityEngine;

public static class GraphStorage
{
    public static GraphList LoadGraphs()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("graphs");
        if (jsonFile == null)
        {
            Debug.LogError("Graph file not found in Resources.");
            return new GraphList();  // Return an empty GraphList in case of an error
        }
        return JsonUtility.FromJson<GraphList>(jsonFile.text);
    }
}
