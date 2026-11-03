using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace PhysicsSandbox
{
    /// <summary>
    /// Writes one session's data, flushing every row so a crash loses nothing:
    /// responses.csv has one row per answer or task, interactions.csv one row per grab,
    /// and session.txt the run's details and frame-rate summary.
    /// </summary>
    public class StudyLog : IDisposable
    {
        static readonly string[] s_ResponseColumns =
        {
            "timestamp", "participant", "run_type", "group", "block", "cue", "set", "phase", "item", "prompt",
            "choice", "correct_choice", "correct", "rt_ms", "completion_ms", "falls", "outcome", "measures",
        };

        static readonly string[] s_InteractionColumns =
        {
            "timestamp", "participant", "run_type", "block", "cue", "set", "phase", "item", "object", "mass_kg",
            "hold_ms", "max_hand_lift_m", "max_offset_m", "gain", "lag_ms",
        };

        readonly StreamWriter m_Responses;
        readonly StreamWriter m_Interactions;
        readonly StreamWriter m_Session;

        public string Folder { get; }
        public string Prefix { get; }
        public int ResponseRows { get; private set; }
        public int InteractionRows { get; private set; }

        public static string DataFolder => Application.isEditor
            ? Path.GetFullPath(Path.Combine(Application.dataPath, "../StudyData"))
            : Path.Combine(Application.persistentDataPath, "StudyData");

        public static string ParticipantTag(int participantId) => $"P{participantId:00}";

        public static bool HasData(int participantId) =>
            Directory.Exists(DataFolder) && Directory.GetFiles(DataFolder, ParticipantTag(participantId) + "_*").Length > 0;

        public StudyLog(int participantId)
        {
            Folder = DataFolder;
            Directory.CreateDirectory(Folder);
            Prefix = $"{ParticipantTag(participantId)}_{DateTime.Now:yyyyMMdd-HHmmss}";

            m_Responses = Open("responses.csv");
            m_Interactions = Open("interactions.csv");
            m_Session = Open("session.txt");
            m_Responses.WriteLine(string.Join(",", s_ResponseColumns));
            m_Interactions.WriteLine(string.Join(",", s_InteractionColumns));
        }

        StreamWriter Open(string suffix) =>
            new StreamWriter(Path.Combine(Folder, $"{Prefix}_{suffix}")) { AutoFlush = true };

        public void WriteResponse(params object[] values)
        {
            m_Responses.WriteLine(Row(values));
            ResponseRows++;
        }

        public void WriteInteraction(params object[] values)
        {
            m_Interactions.WriteLine(Row(values));
            InteractionRows++;
        }

        public void WriteSession(string line) => m_Session.WriteLine(line);

        public void Dispose()
        {
            m_Responses.Dispose();
            m_Interactions.Dispose();
            m_Session.Dispose();
        }

        public static string Row(IEnumerable<object> values)
        {
            var cells = new List<string>();
            foreach (var value in values)
                cells.Add(Escape(Format(value)));
            return string.Join(",", cells);
        }

        static string Format(object value)
        {
            switch (value)
            {
                case null: return "";
                case float f: return f.ToString("0.###", CultureInfo.InvariantCulture);
                case bool b: return b ? "1" : "0";
                case IFormattable formattable: return formattable.ToString(null, CultureInfo.InvariantCulture);
                default: return value.ToString();
            }
        }

        static string Escape(string cell)
        {
            if (cell.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
                return cell;
            return "\"" + cell.Replace("\"", "\"\"") + "\"";
        }
    }
}
