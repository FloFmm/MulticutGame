using UnityEngine;

public class HamburgerButton : MonoBehaviour
{
    public GameObject buttonContainer;

    public void ToggleMenu()
    {
        buttonContainer.SetActive(!buttonContainer.activeSelf);
    }
}
