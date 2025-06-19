using System;
using System.Collections.Generic;
using UnityEngine;  // For Vector2

[Serializable]
public class Entry
{
    public string PlayerName;
    public string PlayerId;
    public DateTime LastUpdated;
    public GraphList GraphHighScoreList;
    public ChallengeHighScoreList ChallengeHighScoreList;
}

[Serializable]
public class LeaderboardEntryList
{
    public DateTime LastUpdated;
    public List<Entry> LeaderboardEntries;
}

