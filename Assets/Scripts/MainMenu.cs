using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void OnClickLevels()
    {
        SceneManager.LoadScene("LevelSelection");
    }

    public void OnClickChallenges()
    {
        Debug.Log("Challenges clicked");
        // Load challenges scene or open a panel
    }

    public void OnClickLeaderboard()
    {
        Debug.Log("Leaderboard clicked");
    }

    public void OnClickSettings()
    {
        Debug.Log("Settings clicked");
    }
}
