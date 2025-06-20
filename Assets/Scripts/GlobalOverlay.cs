using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class GlobalOverlay : MonoBehaviour
{
    public static GlobalOverlay Instance;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Start fully black and show overlay
        canvasGroup.alpha = 1;
        gameObject.SetActive(true);

        // Wait 0.3 seconds, then hide instantly
        StartCoroutine(HideAfterDelay(1));

        // Keep listening for scene loads to show again
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    IEnumerator HideAfterDelay(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return null;
        }
        // yield return new WaitForSeconds(delay);
        canvasGroup.alpha = 0;
        gameObject.SetActive(false);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Show overlay instantly on new scene load
        canvasGroup.alpha = 1;
        gameObject.SetActive(true);

        // Then hide after 0.3 seconds
        StartCoroutine(HideAfterDelay(1));
    }
}
