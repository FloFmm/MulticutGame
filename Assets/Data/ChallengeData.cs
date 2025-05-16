using System;
using System.Collections.Generic;
using UnityEngine;  // For Vector2

[Serializable]
public class Challenge
{
    public string Name;
    public int TimeLimit;
    public bool AddRemainingTime;
    public float MinDifficulty;
    public float MaxDifficulty;
    public int HighScore;
    public int LevelCount;
}

[Serializable]
public class ChallengeList
{
    public List<Challenge> Challenges = new();
}
