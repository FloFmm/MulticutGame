using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BackButton : MonoBehaviour
{
    public void OnBackButtonClicked()
    {
        // Check the current scene and load the appropriate one
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "LevelSelection")
        {
            // Load the MainMenu scene
            SceneManager.LoadScene("MainMenu");
        }
        else if (currentScene == "GameScene")
        {
            // Load the LevelSelection scene
            if (GameData.SelectedChallenge != null)
            {
                SceneManager.LoadScene("ChallengeSelection");
            }
            else
            {
                // GameData.LoadGraphHighScoreList(); why was that here?
                SceneManager.LoadScene("LevelSelection");
            }
        }
        else 
        {
            // Default: Load MainMenu scene
            SceneManager.LoadScene("MainMenu");
        }
    }
}

