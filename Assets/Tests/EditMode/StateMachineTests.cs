using System.Collections.Generic;
using NUnit.Framework;
using TheLostDungeon.Enemies;

namespace TheLostDungeon.Tests
{
    /// <summary>Covers the brief's "State transitions" test area.</summary>
    public class StateMachineTests
    {
        /// <summary>A fake state that records what happened to it.</summary>
        private class RecordingState : IEnemyState
        {
            private readonly List<string> log;
            public RecordingState(string name, List<string> log) { Name = name; this.log = log; }
            public string Name { get; }
            public int Ticks { get; private set; }
            public void Enter() => log.Add($"enter {Name}");
            public void Tick(float deltaTime) => Ticks++;
            public void Exit() => log.Add($"exit {Name}");
        }

        [Test]
        public void ChangeState_ExitsOldStateBeforeEnteringNew()
        {
            var log = new List<string>();
            var patrol = new RecordingState("Patrol", log);
            var chase = new RecordingState("Chase", log);
            var machine = new StateMachine();

            machine.ChangeState(patrol);
            machine.ChangeState(chase);

            CollectionAssert.AreEqual(new[] { "enter Patrol", "exit Patrol", "enter Chase" }, log);
            Assert.AreSame(chase, machine.Current);
        }

        [Test]
        public void ChangingToSameState_DoesNothing()
        {
            var log = new List<string>();
            var attack = new RecordingState("Attack", log);
            var machine = new StateMachine();

            machine.ChangeState(attack);
            machine.ChangeState(attack);

            Assert.AreEqual(1, log.Count, "Attack must not be exited and re-entered");
        }

        [Test]
        public void Tick_OnlyRunsCurrentState()
        {
            var log = new List<string>();
            var patrol = new RecordingState("Patrol", log);
            var chase = new RecordingState("Chase", log);
            var machine = new StateMachine();

            machine.ChangeState(patrol);
            machine.Tick(0.1f);
            machine.ChangeState(chase);
            machine.Tick(0.1f);
            machine.Tick(0.1f);

            Assert.AreEqual(1, patrol.Ticks);
            Assert.AreEqual(2, chase.Ticks);
        }

        [Test]
        public void Changed_ReportsPreviousAndNext()
        {
            var log = new List<string>();
            var patrol = new RecordingState("Patrol", log);
            var dead = new RecordingState("Dead", log);
            var machine = new StateMachine();
            string reported = null;
            machine.Changed += (previous, next) => reported = $"{previous?.Name ?? "none"} -> {next.Name}";

            machine.ChangeState(patrol);
            machine.ChangeState(dead);

            Assert.AreEqual("Patrol -> Dead", reported);
        }
    }
}
