using UnityEngine;
using UnityEngine.SceneManagement;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;
using GooglePlayGames.BasicApi;
using TMPro;
using GooglePlayGames;
using UnityEngine.SocialPlatforms;
using System.Collections.Generic;
using System.Linq;
public class MainMenu : MonoBehaviour
{
    public TMP_Text soundButtonText;
    void Start()
    {
        // Initialize and activate the Play Games platform
        PlayGamesPlatform.Activate();
        SignInToGooglePlay();
        UploadLeaderboardStats();
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
        SignInToGooglePlay();
        UploadLeaderboardStats();
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

    void SignInToGooglePlay()
    {
        if (!Social.localUser.authenticated)
        {
            Social.localUser.Authenticate((bool success) =>
            {
                if (success)
                {

                    Debug.Log("Signed in to Google Play Games Services");
                }
                else
                {

                    Debug.Log("Failed to sign in to Google Play Games Services");
                }
            });
        }
        else
        {
            Debug.Log("Already signed in to Google Play Games");
        }
    }

    public void UploadLeaderboardStats()
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            int completedGraphs = GameData.GraphHighScoreList.Graphs
                .Count(graph => graph.OptimalCost == graph.BestAchievedCost);
            Social.ReportScore(completedGraphs, "CgkI2bj44u0bEAIQAQ", (bool success) =>
            {
                // Handle success or failure
            });


            foreach (Challenge challenge in GameData.ChallengeList.Challenges)
            {
                int highScore = GameData.GetHighScoreForChallenge(challenge).HighScore;
                Social.ReportScore(highScore, challenge.LeaderBoardId, (bool success) =>
                {
                    // Handle success or failure
                });
            }
        }
    }
}
