using System;
using System.Collections.Generic;
using UnityEngine;  // For Vector2

[Serializable]
public class Challenge
{
    public string Name;
    public string CreatedAt;
    public int TimeLimit;
    public int TimePerLevel;
    public float MinDifficulty;
    public float MaxDifficulty;
    public int LevelCount;
}

[Serializable]
public class ChallengeHighScore
{
    public string ChallengeName;
    public string ChallengeCreatedAt;
    public int HighScore = 0;
    public int HighScoreTime = 0;
}

[Serializable]
public class ChallengeList
{
    public List<Challenge> Challenges = new();
}

[Serializable]
public class ChallengeHighScoreList
{
    public List<ChallengeHighScore> ChallengeHighScores = new();
}
