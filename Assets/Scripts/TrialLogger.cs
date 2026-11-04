using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Appends one line per trial to trials.csv (shared by all three rooms).
/// Location: Application.persistentDataPath (printed in the Console on the first write).
///   Mac editor : ~/Library/Application Support/[Company]/[Product]/trials.csv
///   Quest      : /sdcard/Android/data/[package]/files/trials.csv  (pull with adb or Meta Quest Developer Hub)
/// </summary>
public static class TrialLogger
{
    private const string Header =
        "participantId,room,trialIndex,changeType,targetObject,selectedObject,correct,reactionTimeMs,confidence,timestamp";

    /// <summary>One finished trial, kept in memory for the results page.</summary>
    public class TrialRecord
    {
        public string room;
        public bool correct;
        public int reactionTimeMs;
        public int confidence;
    }

    /// <summary>Every trial logged since the game started (shown on the results page).</summary>
    public static readonly System.Collections.Generic.List<TrialRecord> Session = new System.Collections.Generic.List<TrialRecord>();

    public static void ClearSession() { Session.Clear(); }

    public static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "trials.csv"); }
    }

    public static void Append(int participantId, string room, int trialIndex, string changeType,
                              string targetObject, string selectedObject, bool correct,
                              int reactionTimeMs, int confidence)
    {
        Session.Add(new TrialRecord { room = room, correct = correct, reactionTimeMs = reactionTimeMs, confidence = confidence });

        bool newFile = !File.Exists(FilePath);
        if (newFile) Debug.Log("TrialLogger: writing to " + FilePath);

        using (StreamWriter writer = new StreamWriter(FilePath, true))
        {
            if (newFile) writer.WriteLine(Header);

            // object names and room names contain no commas, so no quoting is needed
            writer.WriteLine(string.Join(",",
                participantId, room, trialIndex, changeType, targetObject, selectedObject,
                correct ? 1 : 0, reactionTimeMs, confidence,
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
        }
    }
}
