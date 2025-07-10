using UnityEngine;
using UnityEngine.SceneManagement;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using GooglePlayGames.BasicApi;
using TMPro;
public class MainMenu : MonoBehaviour
{
    public TMP_Text soundButtonText;
    void Start()
    {
        // Initialize and activate the Play Games platform
        PlayGamesPlatform.Activate();
    }

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

        SceneManager.LoadScene("Leaderboard");
    }

    public void OnClickSettings()
    {
        GameData.SoundIsOn = !GameData.SoundIsOn;
        if (GameData.SoundIsOn)
            soundButtonText.text = "SOUND ON";
        else
            soundButtonText.text = "SOUND OFF";
    }
}
