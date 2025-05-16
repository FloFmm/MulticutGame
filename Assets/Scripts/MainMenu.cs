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
        SceneManager.LoadScene("ChallengeSelection");
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
