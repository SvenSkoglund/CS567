using System;
using System.Collections.Generic;

namespace PhysicsSandbox
{
    public enum ScenarioSet
    {
        A,
        B,
    }

    public readonly struct StudyBlockPlan
    {
        public readonly bool CueOn;
        public readonly ScenarioSet Set;

        public StudyBlockPlan(bool cueOn, ScenarioSet set)
        {
            CueOn = cueOn;
            Set = set;
        }

        public override string ToString() => $"{(CueOn ? "cue ON" : "cue OFF")} set {Set}";
    }

    /// <summary>
    /// Assigns each participant a block order. The four orders cross cue order with which
    /// scenario set goes with the cue, so twelve participants give three of each.
    /// </summary>
    public static class Counterbalance
    {
        public const int OrderCount = 4;

        /// <summary>IDs from here up are logged as pilot runs, not study data.</summary>
        public const int FirstPilotId = 90;

        static readonly StudyBlockPlan[][] s_Orders =
        {
            new[] { new StudyBlockPlan(true, ScenarioSet.A), new StudyBlockPlan(false, ScenarioSet.B) },
            new[] { new StudyBlockPlan(false, ScenarioSet.A), new StudyBlockPlan(true, ScenarioSet.B) },
            new[] { new StudyBlockPlan(true, ScenarioSet.B), new StudyBlockPlan(false, ScenarioSet.A) },
            new[] { new StudyBlockPlan(false, ScenarioSet.B), new StudyBlockPlan(true, ScenarioSet.A) },
        };

        public static int OrderIndex(int participantId) => ((participantId - 1) % OrderCount + OrderCount) % OrderCount;

        public static StudyBlockPlan[] Plan(int participantId) => s_Orders[OrderIndex(participantId)];

        public static string Describe(int participantId)
        {
            var plan = Plan(participantId);
            return $"{plan[0]}, then {plan[1]}";
        }

        public static bool IsPilot(int participantId) => participantId >= FirstPilotId;
    }

    /// <summary>One multiple-choice item of the misconception test.</summary>
    public class TestItem
    {
        public readonly string Id;
        public readonly string Prompt;
        public readonly string[] Options;
        public readonly int CorrectIndex;

        /// <summary>Numeric options read better in order, so they are not shuffled.</summary>
        public readonly bool KeepOrder;

        public TestItem(string id, string prompt, string[] options, int correctIndex, bool keepOrder = false)
        {
            Id = id;
            Prompt = prompt;
            Options = options;
            CorrectIndex = correctIndex;
            KeepOrder = keepOrder;
        }
    }

    public readonly struct HeavinessTrial
    {
        public readonly float LeftKg;
        public readonly float RightKg;

        public HeavinessTrial(float leftKg, float rightKg)
        {
            LeftKg = leftKg;
            RightKg = rightKg;
        }

        public int HeavierIndex => LeftKg > RightKg ? 0 : 1;
    }

    /// <summary>
    /// The study's fixed content: two parallel six-item tests (four items on the sandbox's
    /// concepts, then two transfer items with materials it does not contain), the
    /// presence-and-comfort items, and the unmarked-block heaviness trials.
    /// </summary>
    public static class StudyDesign
    {
        public const int LikertPoints = 7;

        static readonly (float light, float heavy)[] s_HeavinessPairs =
        {
            (0.5f, 2f), (1f, 4f), (2f, 8f), (0.5f, 4f), (1f, 8f), (0.5f, 8f),
        };

        public static readonly TestItem[] TestA =
        {
            new TestItem("A1_volume", "A 1 kg block of pine and a 1 kg block of aluminum sit on a table. Which block is larger?",
                new[] { "The pine block", "The aluminum block", "They are the same size" }, 0),
            new TestItem("A2_float", "A large pine beam weighs 20 kg. A small steel nail weighs 5 grams. Which one floats in water?",
                new[] { "Only the pine beam", "Only the steel nail", "Both float", "Neither floats" }, 0),
            new TestItem("A3_ramp", "An ice block and a much heavier steel block of the same size are let go at the top of the same ramp. Which one slides farther?",
                new[] { "The ice block", "The steel block", "They slide the same distance" }, 0),
            new TestItem("A4_lever", "A 6 kg load sits 10 cm to the left of a lever's pivot. How far to the right of the pivot does a 2 kg load balance it?",
                new[] { "10 cm", "20 cm", "30 cm", "60 cm" }, 2, true),
            new TestItem("A5_transfer", "You hold a 1 kg bag of feathers, then a 1 kg bar of gold. Which one pulls down harder on your hand?",
                new[] { "The feathers", "The gold", "They pull the same" }, 2),
            new TestItem("A6_transfer", "Olive oil is less dense than water. You pour some into a glass of water. Where does the oil end up?",
                new[] { "It floats on top", "It sinks to the bottom", "It spreads evenly through the water" }, 0),
        };

        public static readonly TestItem[] TestB =
        {
            new TestItem("B1_volume", "A 1 kg block of ice and a 1 kg block of steel sit on a table. Which block is larger?",
                new[] { "The ice block", "The steel block", "They are the same size" }, 0),
            new TestItem("B2_float", "A huge iceberg weighs 1000 tonnes. A small aluminum cube weighs 2 grams. Which one floats in water?",
                new[] { "Only the iceberg", "Only the aluminum cube", "Both float", "Neither floats" }, 0),
            new TestItem("B3_ramp", "An ice block and a heavier aluminum block of the same size are let go at the top of the same ramp. Which one slides farther?",
                new[] { "The ice block", "The aluminum block", "They slide the same distance" }, 0),
            new TestItem("B4_lever", "An 8 kg load sits 5 cm to the left of a lever's pivot. How far to the right of the pivot does a 2 kg load balance it?",
                new[] { "5 cm", "10 cm", "20 cm", "40 cm" }, 2, true),
            new TestItem("B5_transfer", "You put 1 kg of foam on a scale, then 1 kg of lead. Which one gives the higher reading?",
                new[] { "The foam", "The lead", "The readings are the same" }, 2),
            new TestItem("B6_transfer", "Honey is denser than water. You pour some into a glass of water. Where does the honey end up?",
                new[] { "It sinks to the bottom", "It floats on top", "It spreads evenly through the water" }, 0),
        };

        /// <summary>The first four are averaged into the presence score; the last is comfort.</summary>
        public static readonly string[] PresenceItems =
        {
            "I felt as if I was really standing at the workbench.",
            "The blocks felt like real objects with their own weight.",
            "I could tell how heavy an object was by handling it.",
            "Moving the objects felt natural.",
            "I felt comfortable during this part (no dizziness or nausea).",
        };

        public static TestItem[] Test(ScenarioSet set) => set == ScenarioSet.A ? TestA : TestB;

        /// <summary>Same options in a new order, and the index of the correct one within it.</summary>
        public static string[] ShuffledOptions(TestItem item, Random random, out int correctIndex)
        {
            var options = (string[])item.Options.Clone();
            correctIndex = item.CorrectIndex;
            if (item.KeepOrder)
                return options;

            for (var i = options.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (options[i], options[j]) = (options[j], options[i]);
                if (correctIndex == i)
                    correctIndex = j;
                else if (correctIndex == j)
                    correctIndex = i;
            }

            return options;
        }

        /// <summary>
        /// One trial per mass pair in random order. The heavier block is on the left in
        /// exactly half, so always answering one side scores chance.
        /// </summary>
        public static List<HeavinessTrial> HeavinessTrials(Random random)
        {
            var count = s_HeavinessPairs.Length;
            var pairOrder = new int[count];
            var heavyLeft = new bool[count];
            for (var i = 0; i < count; i++)
            {
                pairOrder[i] = i;
                heavyLeft[i] = i < count / 2;
            }

            Shuffle(pairOrder, random);
            Shuffle(heavyLeft, random);

            var trials = new List<HeavinessTrial>(count);
            for (var i = 0; i < count; i++)
            {
                var (light, heavy) = s_HeavinessPairs[pairOrder[i]];
                trials.Add(heavyLeft[i] ? new HeavinessTrial(heavy, light) : new HeavinessTrial(light, heavy));
            }

            return trials;
        }

        static void Shuffle<T>(T[] values, Random random)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
