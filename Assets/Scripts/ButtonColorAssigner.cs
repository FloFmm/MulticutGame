using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonColorAssigner : MonoBehaviour
{
    public List<Button> buttons;           // Assign your 5 buttons in Inspector

    void Start()
    {
        // Safety check: avoid index out of range
        int count = Mathf.Min(buttons.Count, GameData.ColorPalette.edgeColors.Count);

        for (int i = 0; i < count; i++)
        {
            ColorBlock cb = buttons[i].colors;  // Get button's current color settings
            cb.normalColor = GameData.ColorPalette.edgeColors[i]; // Assign palette color to normalColor
            buttons[i].colors = cb;              // Apply back to the button
        }
    }
}
