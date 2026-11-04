using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// The numbers shown on the results page: accuracy (overall and per room), average confidence
/// (overall, when right, when wrong) and average reaction time.
/// Uses the trials of this session (TrialLogger.Session); if there are none (for example when the
/// End scene is played on its own while testing), it reads this participant's rows from trials.csv.
/// </summary>
public class ResultsSummary
{
    public class RoomResult
    {
        public string scene;
        public string label;
        public int total;
        public int correct;
    }

    public bool hasData;
    public float totalSeconds;               // time from the start of the experiment to the end of the questionnaire
    public int total;
    public int correct;
    public float averageReactionSeconds;
    public float averageConfidence;
    public float confidenceWhenRight = -1f;  // -1 = no such trials
    public float confidenceWhenWrong = -1f;
    public List<RoomResult> rooms = new List<RoomResult>
    {
        new RoomResult { scene = "RoomA", label = "Room A - colour" },
        new RoomResult { scene = "RoomB", label = "Room B - position" },
        new RoomResult { scene = "RoomC", label = "Room C - disappearing" },
    };

    public float Accuracy { get { return total > 0 ? (float)correct / total : 0f; } }

    public static ResultsSummary Compute(int participantId)
    {
        List<TrialLogger.TrialRecord> trials = new List<TrialLogger.TrialRecord>(TrialLogger.Session);
        if (trials.Count == 0) trials = ReadFromCsv(participantId);

        ResultsSummary s = new ResultsSummary();
        s.totalSeconds = ExperimentTimer.ElapsedSeconds;
        if (trials.Count == 0) return s;

        s.hasData = true;
        float reaction = 0f, confidence = 0f, confRight = 0f, confWrong = 0f;
        int nRight = 0, nWrong = 0;

        foreach (TrialLogger.TrialRecord t in trials)
        {
            s.total++;
            reaction += t.reactionTimeMs;
            confidence += t.confidence;
            if (t.correct) { s.correct++; confRight += t.confidence; nRight++; }
            else { confWrong += t.confidence; nWrong++; }

            foreach (RoomResult room in s.rooms)
            {
                if (room.scene != t.room) continue;
                room.total++;
                if (t.correct) room.correct++;
            }
        }

        s.averageReactionSeconds = reaction / s.total / 1000f;
        s.averageConfidence = confidence / s.total;
        if (nRight > 0) s.confidenceWhenRight = confRight / nRight;
        if (nWrong > 0) s.confidenceWhenWrong = confWrong / nWrong;
        return s;
    }

    // trials.csv columns: participantId,room,trialIndex,changeType,targetObject,selectedObject,correct,reactionTimeMs,confidence,timestamp
    private static List<TrialLogger.TrialRecord> ReadFromCsv(int participantId)
    {
        List<TrialLogger.TrialRecord> list = new List<TrialLogger.TrialRecord>();
        if (!File.Exists(TrialLogger.FilePath)) return list;

        string[] lines = File.ReadAllLines(TrialLogger.FilePath);
        for (int i = 1; i < lines.Length; i++) // line 0 is the header
        {
            string[] c = lines[i].Split(',');
            if (c.Length < 9) continue;

            int id, correct, reaction, confidence;
            if (!int.TryParse(c[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out id) || id != participantId) continue;
            if (!int.TryParse(c[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out correct)) continue;
            if (!int.TryParse(c[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out reaction)) continue;
            if (!int.TryParse(c[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out confidence)) continue;

            list.Add(new TrialLogger.TrialRecord { room = c[1], correct = correct == 1, reactionTimeMs = reaction, confidence = confidence });
        }
        return list;
    }
}
