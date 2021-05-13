using System;
using Core;
using Core.Session;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace BlazorServer.Test
{
    [TestFixture]
    public class GrindingSessionTests
    {
        private IBotController _controller;
        private GrindingSession _grindingSession;
        private IGrindingSessionHandler _grindingSessionHandler;

        [SetUp]
        public void Setup()
        {
            _controller = Substitute.For<IBotController>();
            _grindingSessionHandler = Substitute.For<IGrindingSessionHandler>();
            _grindingSession = new GrindingSession(_controller, _grindingSessionHandler);
        }

        [Test]
        public void WhenLevel60_ExpInBotSessionShouldBe0()
        {
            _grindingSession.LevelFrom = 60;
            _grindingSession.ExpGetInBotSession.Should().Be(0);
            _grindingSession.ExperiencePerHour.Should().Be(0);
        }

        [Test]
        public void WhenLevel59ToLevel60_ShouldOnlyConsiderLevel59()
        {
            _grindingSession.LevelFrom = 59;
            _grindingSession.LevelTo = 60;
            _grindingSession.XpFrom = 0;
            _grindingSession.SessionStart = new DateTime(1999,1,1,0,0,0);
            _grindingSession.SessionEnd = new DateTime(1999,1,1,0,59,0);
            _grindingSession.ExpGetInBotSession.Should().Be(209800);
            _grindingSession.ExperiencePerHour.Should().Be(213356);
        }

        [Test]
        public void WhenSameLevel_ShouldOnlyConsiderTheDiff()
        {
            _grindingSession.LevelFrom = 10;
            _grindingSession.LevelTo = 10;
            _grindingSession.XpFrom = 0;
            _grindingSession.XpTo = 200;
            _grindingSession.SessionStart = new DateTime(1999, 1, 1, 0, 0, 0);
            _grindingSession.SessionEnd = new DateTime(1999, 1, 1, 0, 7, 0);
            _grindingSession.ExpGetInBotSession.Should().Be(200);
            _grindingSession.ExperiencePerHour.Should().Be(1714);
        }

        [Test]
        public void WhenDiffLevel_ShouldCalculateCorrectly1()
        {
            _grindingSession.LevelFrom = 10;
            _grindingSession.LevelTo = 12;
            _grindingSession.XpFrom = 0;
            _grindingSession.XpTo = 200;
            _grindingSession.SessionStart = new DateTime(1999, 1, 1, 0, 0, 0);
            _grindingSession.SessionEnd = new DateTime(1999, 1, 1, 0, 7, 0);
            _grindingSession.ExpGetInBotSession.Should().Be(16600);
            _grindingSession.ExperiencePerHour.Should().Be(142286);
        }

        [Test]
        public void WhenDiffLevel_ShouldCalculateCorrectly2()
        {
            _grindingSession.LevelFrom = 50;
            _grindingSession.LevelTo = 51;
            _grindingSession.XpFrom = 0;
            _grindingSession.XpTo = 200;
            _grindingSession.SessionStart = new DateTime(1999, 1, 1, 0, 0, 0);
            _grindingSession.SessionEnd = new DateTime(1999, 1, 1, 0, 56, 0);
            _grindingSession.ExpGetInBotSession.Should().Be(147700);
            _grindingSession.ExperiencePerHour.Should().Be(158250);
        }
    }
}