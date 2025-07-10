using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NextButton : MonoBehaviour
{
    public TextMeshProUGUI slideNumber;
    public int currentIndex = 0;
    public int slideCount = 0;


    void Start()
    {
        UpdateText();
    }

    public void OnClicked()
    {
        currentIndex = (currentIndex + 1) % slideCount;
        UpdateText();
    }

    public void UpdateText()
    {
        slideNumber.text = $"{currentIndex + 1}/{slideCount}";
    }
}
