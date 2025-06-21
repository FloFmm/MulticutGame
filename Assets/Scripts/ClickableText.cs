using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClickableText : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public string[] messages;
    private int currentIndex = 0;

    void Start()
    {
        if (textComponent == null)
            textComponent = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    public void OnTextClicked()
    {
        currentIndex = (currentIndex + 1) % messages.Length;
        UpdateText();
    }

    public void UpdateText()
    {
        textComponent.text = messages[currentIndex];
    }
}
