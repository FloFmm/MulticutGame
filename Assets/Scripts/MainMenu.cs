using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void OnClickTutorial()
    {
        GameData.IsTutorial = true;
        SceneManager.LoadScene("LevelSelection");
    }

    public void OnClickLevels()
    {
        GameData.IsTutorial = false;
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
