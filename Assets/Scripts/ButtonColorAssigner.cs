using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ButtonColorAssigner : MonoBehaviour
{
    public List<Button> buttons;           // Assign your buttons in Inspector

    void Start()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            int color_index = Mathf.Min(i, GameData.ColorPalette.edgeColors.Count);
            var image = buttons[i].GetComponent<Image>();
            if (image != null)
                image.color = GameData.ColorPalette.edgeColors[i];
        }
    }
}
