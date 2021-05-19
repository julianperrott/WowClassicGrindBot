using System;
using Core;
using Core.Session;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace BlazorServer.Test
{
    [TestFixture]
    public class GrindSessionTests
    {
        private IBotController _controller;
        private GrindSession _grindSession;
        private IGrindSessionHandler _grindSessionHandler;

        [SetUp]
        public void Setup()
        {
            _controller = Substitute.For<IBotController>();
            _grindSessionHandler = Substitute.For<IGrindSessionHandler>();
            _grindSession = new GrindSession(_controller, _grindSessionHandler);
        }

        [Test]
        public void WhenLevel60_ExpInBotSessionShouldBe0()
        {
            _grindSession.LevelFrom = 60;
            _grindSession.ExpGetInBotSession.Should().Be(0);
            _grindSession.ExperiencePerHour.Should().Be(0);
        }

        [Test]
        public void WhenLevel59ToLevel60_ShouldOnlyConsiderLevel59()
        {
            _grindSession.LevelFrom = 59;
            _grindSession.LevelTo = 60;
            _grindSession.XpFrom = 0;
            _grindSession.SessionStart = new DateTime(1999,1,1,0,0,0);
            _grindSession.SessionEnd = new DateTime(1999,1,1,0,59,0);
            _grindSession.ExpGetInBotSession.Should().Be(209800);
            _grindSession.ExperiencePerHour.Should().Be(213356);
        }

        [Test]
        public void WhenSameLevel_ShouldOnlyConsiderTheDiff()
        {
            _grindSession.LevelFrom = 10;
            _grindSession.LevelTo = 10;
            _grindSession.XpFrom = 0;
            _grindSession.XpTo = 200;
            _grindSession.SessionStart = new DateTime(1999, 1, 1, 0, 0, 0);
            _grindSession.SessionEnd = new DateTime(1999, 1, 1, 0, 7, 0);
            _grindSession.ExpGetInBotSession.Should().Be(200);
            _grindSession.ExperiencePerHour.Should().Be(1714);
        }

        [Test]
        public void WhenDiffLevel_ShouldCalculateCorrectly1()
        {
            _grindSession.LevelFrom = 10;
            _grindSession.LevelTo = 12;
            _grindSession.XpFrom = 0;
            _grindSession.XpTo = 200;
            _grindSession.SessionStart = new DateTime(1999, 1, 1, 0, 0, 0);
            _grindSession.SessionEnd = new DateTime(1999, 1, 1, 0, 7, 0);
            _grindSession.ExpGetInBotSession.Should().Be(16600);
            _grindSession.ExperiencePerHour.Should().Be(142286);
        }

        [Test]
        public void WhenDiffLevel_ShouldCalculateCorrectly2()
        {
            _grindSession.LevelFrom = 50;
            _grindSession.LevelTo = 51;
            _grindSession.XpFrom = 0;
            _grindSession.XpTo = 200;
            _grindSession.SessionStart = new DateTime(1999, 1, 1, 0, 0, 0);
            _grindSession.SessionEnd = new DateTime(1999, 1, 1, 0, 56, 0);
            _grindSession.ExpGetInBotSession.Should().Be(147700);
            _grindSession.ExperiencePerHour.Should().Be(158250);
        }
    }
}