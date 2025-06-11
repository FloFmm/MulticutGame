using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Settings/Color Palette")]
public class ColorPalette : ScriptableObject
{
    public List<Color> edgeColors;
    public List<Color> nodeColors;
    [HideInInspector] public Color lowRemainingTimeColor;
    [HideInInspector] public Color invalidSolutionColor;
    [HideInInspector] public Color optimalSolutionColor;
    [HideInInspector] public Color LevelCompleteTextColor;
    [HideInInspector] public Color LevelCompleteButtonColor;
    [HideInInspector] public Color normalTextColor = Color.white;

    private void OnValidate()
    {
        if (edgeColors != null && edgeColors.Count > 0)
        {
            invalidSolutionColor = edgeColors[edgeColors.Count - 1];
            lowRemainingTimeColor = edgeColors[edgeColors.Count - 1];
            optimalSolutionColor = edgeColors[0];
            LevelCompleteTextColor = edgeColors[0];
            LevelCompleteButtonColor = edgeColors[0];
        }
    }
}
