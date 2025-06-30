using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ClickableText : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public TextMeshProUGUI slideNumber;
    public Image nextImage;

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
        if (messages.Length == 1)
        {
            slideNumber.gameObject.SetActive(false);
            nextImage.gameObject.SetActive(false);
        }
        textComponent.text = messages[currentIndex];
        if (messages.Length > 1)
            slideNumber.text = $"{currentIndex+1}/{messages.Length}";
    }
}
