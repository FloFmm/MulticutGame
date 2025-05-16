using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Settings/Color Palette")]
public class ColorPalette : ScriptableObject
{
    public List<Color> edgeColors;
    public List<Color> nodeColors;
}
