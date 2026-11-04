using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Appends one line per answered question to questionnaire.csv (same folder as trials.csv).
/// The participantId column matches trials.csv, so each person's answers join their trial rows.
/// </summary>
public static class QuestionnaireLogger
{
    private const string Header = "participantId,questionId,question,answerIndex,answer,timestamp";

    public static string FilePath
    {
        get { return Path.Combine(Application.persistentDataPath, "questionnaire.csv"); }
    }

    public static void Append(int participantId, string questionId, string question, int answerIndex, string answer)
    {
        bool newFile = !File.Exists(FilePath);
        if (newFile) Debug.Log("QuestionnaireLogger: writing to " + FilePath);

        using (StreamWriter writer = new StreamWriter(FilePath, true))
        {
            if (newFile) writer.WriteLine(Header);
            writer.WriteLine(string.Join(",",
                participantId, Escape(questionId), Escape(question), answerIndex, Escape(answer),
                DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
        }
    }

    // question and answer texts may contain commas, so they are wrapped in quotes
    private static string Escape(string value)
    {
        if (value == null) return "";
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
