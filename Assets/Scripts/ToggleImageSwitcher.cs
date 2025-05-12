using UnityEngine;
using UnityEngine.UI;

public class ToggleImageSwitcher : MonoBehaviour
{
    public Image image1; // Assign in Inspector
    public Image image2; // Assign in Inspector

    private Toggle toggle;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        // Initialize the images based on the toggle's starting state
        OnToggleValueChanged(toggle.isOn);
    }

    void OnToggleValueChanged(bool isOn)
    {
        image1.gameObject.SetActive(isOn);
        image2.gameObject.SetActive(!isOn);
        GameData.ScissorIsActive = isOn;
    }
}
