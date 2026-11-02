using System.Collections.Generic;
using NUnit.Framework;

namespace PhysicsSandbox.Tests
{
    /// <summary>
    /// Experiment mode has to assign conditions correctly before anyone runs it: these cover
    /// the counterbalancing, the test content, the heaviness trials and the ramp's closed forms.
    /// </summary>
    public class StudyDesignTests
    {
        [Test]
        public void TwelveParticipantsFillEachOrderEqually()
        {
            var counts = new int[Counterbalance.OrderCount];
            for (var id = 1; id <= 12; id++)
                counts[Counterbalance.OrderIndex(id)]++;

            foreach (var count in counts)
                Assert.AreEqual(3, count);
        }

        [Test]
        public void EveryOrderHasBothCuesAndBothSets()
        {
            for (var id = 1; id <= Counterbalance.OrderCount; id++)
            {
                var plan = Counterbalance.Plan(id);
                Assert.AreEqual(2, plan.Length);
                Assert.AreNotEqual(plan[0].CueOn, plan[1].CueOn, "participant sees both cue conditions");
                Assert.AreNotEqual(plan[0].Set, plan[1].Set, "participant never repeats a set");
            }
        }

        [Test]
        public void OrdersCrossCueOrderWithSetPairing()
        {
            var cueOnFirst = 0;
            var cueOnWithA = 0;
            for (var id = 1; id <= Counterbalance.OrderCount; id++)
            {
                var plan = Counterbalance.Plan(id);
                if (plan[0].CueOn)
                    cueOnFirst++;
                if ((plan[0].CueOn && plan[0].Set == ScenarioSet.A) || (plan[1].CueOn && plan[1].Set == ScenarioSet.A))
                    cueOnWithA++;
            }

            Assert.AreEqual(2, cueOnFirst);
            Assert.AreEqual(2, cueOnWithA);
        }

        [Test]
        public void TestsAreParallelSixItemForms()
        {
            Assert.AreEqual(6, StudyDesign.TestA.Length);
            Assert.AreEqual(6, StudyDesign.TestB.Length);
            for (var i = 0; i < 6; i++)
            {
                var a = StudyDesign.TestA[i];
                var b = StudyDesign.TestB[i];
                Assert.AreEqual(a.Options.Length, b.Options.Length, a.Id);
                Assert.AreEqual(a.Id.Substring(2), b.Id.Substring(2), "items pair up by concept");
                Assert.That(a.CorrectIndex, Is.InRange(0, a.Options.Length - 1));
                Assert.That(b.CorrectIndex, Is.InRange(0, b.Options.Length - 1));
            }
        }

        [Test]
        public void ShufflingKeepsTheCorrectOptionCorrect()
        {
            var random = new System.Random(7);
            foreach (var item in StudyDesign.TestA)
            {
                for (var repeat = 0; repeat < 50; repeat++)
                {
                    var options = StudyDesign.ShuffledOptions(item, random, out var correct);
                    Assert.AreEqual(item.Options[item.CorrectIndex], options[correct], item.Id);
                    CollectionAssert.AreEquivalent(item.Options, options);
                }
            }
        }

        [Test]
        public void LeverItemsMatchTheLawOfTheLever()
        {
            Assert.AreEqual("30 cm", StudyDesign.TestA[3].Options[StudyDesign.TestA[3].CorrectIndex]);
            Assert.AreEqual(30f, PhysicsFormulas.BalancingArmM(6f, 10f, 2f), 1e-4f);
            Assert.AreEqual("20 cm", StudyDesign.TestB[3].Options[StudyDesign.TestB[3].CorrectIndex]);
            Assert.AreEqual(20f, PhysicsFormulas.BalancingArmM(8f, 5f, 2f), 1e-4f);
        }

        [Test]
        public void HeavinessTrialsAreBalancedAcrossSides()
        {
            for (var seed = 0; seed < 100; seed++)
            {
                var trials = StudyDesign.HeavinessTrials(new System.Random(seed));
                Assert.AreEqual(6, trials.Count);

                var left = 0;
                var pairs = new HashSet<string>();
                foreach (var trial in trials)
                {
                    Assert.AreNotEqual(trial.LeftKg, trial.RightKg);
                    if (trial.HeavierIndex == 0)
                        left++;
                    pairs.Add(System.Math.Min(trial.LeftKg, trial.RightKg) + "/" + System.Math.Max(trial.LeftKg, trial.RightKg));
                }

                Assert.AreEqual(3, left, "heavier block on the left in half the trials");
                Assert.AreEqual(6, pairs.Count, "each mass pair appears once");
            }
        }

        [Test]
        public void HeavinessPairsDifferInGainSoTheCueCanSeparateThem()
        {
            foreach (var trial in StudyDesign.HeavinessTrials(new System.Random(1)))
                Assert.Greater(System.Math.Abs(PseudoHapticMapping.Gain(trial.LeftKg) - PseudoHapticMapping.Gain(trial.RightKg)), 0.2f);
        }

        [Test]
        public void SlideFormulasMatchClosedForm()
        {
            Assert.IsTrue(PhysicsFormulas.WillSlide(12.5f, 0.125f), "ice on the ramp");
            Assert.IsFalse(PhysicsFormulas.WillSlide(12.5f, 0.275f), "aluminum on the ramp");
            Assert.IsFalse(PhysicsFormulas.WillSlide(12.5f, 0.325f), "steel on the ramp");
            Assert.AreEqual(0f, PhysicsFormulas.SlideAcceleration(15f, 0.5f), 1e-5f, "friction cannot push a block uphill");
            Assert.AreEqual(1.108f, PhysicsFormulas.SlideAcceleration(12.5f, 0.106f), 0.01f);
            Assert.AreEqual(0.3f, PhysicsFormulas.CombinedFriction(0.5f, 0.1f), 1e-5f);
        }

        [Test]
        public void CsvRowsEscapeCommasAndQuotes()
        {
            var row = StudyLog.Row(new object[] { "a,b", "say \"hi\"", 3, 1.5f, true, null });
            Assert.AreEqual("\"a,b\",\"say \"\"hi\"\"\",3,1.5,1,", row);
        }
    }
}
