using Libs;
using Libs.Actions;
using Libs.GOAP;
using Libs.NpcFinder;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests.GOAP.GoapPlannerTests
{
    [TestClass]
    public class Plan: ILogger,IDisposable
    {
        GoapAction followRouteAction = null;
        GoapAction killMobAction = null;
        GoapAction pullTargetAction = null;
        GoapAction approachTargetAction = null;
        GoapAction findTargetAction = null;
        HashSet<GoapAction> availableActions;
        Mock<ILogger> logger;

       HashSet<KeyValuePair<GoapKey, GoapPreCondition>> goal = new HashSet<KeyValuePair<GoapKey, GoapPreCondition>>();

        [TestInitialize]
        public void TestInitialize()
        {
            logger = new Mock<ILogger>();
            var wowprocess = new WowProcess(logger.Object);

            var playerReader = new PlayerReader(new Mock<ISquareReader>().Object, this);
            var stopMoving = new StopMoving(wowprocess, playerReader, logger.Object);
            var npcNameFinder = new NpcNameFinder(wowprocess, playerReader,logger.Object);
            var stuckDetector = new StuckDetector(playerReader, wowprocess, new Mock<IPlayerDirection>().Object, stopMoving, logger.Object);

            this.followRouteAction = new FollowRouteAction(playerReader, wowprocess, new Mock<IPlayerDirection>().Object, new List<WowPoint>(), stopMoving, npcNameFinder, new Blacklist(playerReader), logger.Object, stuckDetector);

            this.killMobAction = new WarriorCombatAction(wowprocess, playerReader, stopMoving, logger.Object);
            this.pullTargetAction = new PullTargetAction(wowprocess, playerReader, npcNameFinder, stopMoving, logger.Object, this.killMobAction as CombatActionBase);
            this.approachTargetAction = new ApproachTargetAction(wowprocess, playerReader, stopMoving, npcNameFinder, logger.Object, stuckDetector);

            this.availableActions = new HashSet<GoapAction>
            {
                followRouteAction,
                killMobAction,
                pullTargetAction,
                approachTargetAction,
                findTargetAction
            };
        }

        [TestMethod]
        public void NoWorldState_SoNoActions()
        {
            // Arrange
            var worldState = new HashSet<KeyValuePair<GoapKey, object>>();

            // Act
            var result = new GoapPlanner(logger.Object).Plan(this.availableActions, worldState, this.goal);

            // Assert
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void NoTarget_SoFollowRoute()
        {
            // Arrange
            var worldState = new HashSet<KeyValuePair<GoapKey, object>>
            {
                new KeyValuePair<GoapKey, object>(GoapKey.hastarget, false),
                new KeyValuePair<GoapKey, object>(GoapKey.incombat, false),
                new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.incombatrange, false)
            };

            // Act
            var result = new GoapPlanner(logger.Object).Plan(this.availableActions, worldState, this.goal);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(followRouteAction,result.Peek());
        }

        [TestMethod]
        public void HasTargetNotInCombatwithinPullRangeAndNotPulled_SoPull()
        {
            // Arrange
            var worldState = new HashSet<KeyValuePair<GoapKey, object>>
            {
                new KeyValuePair<GoapKey, object>(GoapKey.hastarget, true),
                new KeyValuePair<GoapKey, object>(GoapKey.incombat, false),
                new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, true),
                new KeyValuePair<GoapKey, object>(GoapKey.incombatrange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, false)
            };

            // Act
            var result = new GoapPlanner(logger.Object).Plan(this.availableActions, worldState, this.goal);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(pullTargetAction, result.Peek());
        }

        [TestMethod]
        public void HasTargetNotInCombatwithinPullRangeAndPulled_SoApproach()
        {
            // Arrange
            var worldState = new HashSet<KeyValuePair<GoapKey, object>>
            {
                new KeyValuePair<GoapKey, object>(GoapKey.hastarget, true),
                new KeyValuePair<GoapKey, object>(GoapKey.incombat, false),
                new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, true),
                new KeyValuePair<GoapKey, object>(GoapKey.incombatrange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, true)
            };

            // Act
            var result = new GoapPlanner(logger.Object).Plan(this.availableActions, worldState, this.goal);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(approachTargetAction, result.Peek());
        }

        [TestMethod]
        public void HasTargetButNotInMeleeRange_SoApproach()
        {
            // Arrange
            var worldState = new HashSet<KeyValuePair<GoapKey, object>>
            {
                new KeyValuePair<GoapKey, object>(GoapKey.hastarget, true),
                new KeyValuePair<GoapKey, object>(GoapKey.incombat, true),
                new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, true),
                new KeyValuePair<GoapKey, object>(GoapKey.incombatrange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, false)
            };

            // Act
            var result = new GoapPlanner(logger.Object).Plan(this.availableActions, worldState, this.goal);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(approachTargetAction, result.Peek());
        }

        [TestMethod]
        public void NoTargetButInCombat_SoFindTarget()
        {
            // Arrange
            var worldState = new HashSet<KeyValuePair<GoapKey, object>>
            {
                new KeyValuePair<GoapKey, object>(GoapKey.hastarget, false),
                new KeyValuePair<GoapKey, object>(GoapKey.incombat, true),
                new KeyValuePair<GoapKey, object>(GoapKey.withinpullrange, true),
                new KeyValuePair<GoapKey, object>(GoapKey.incombatrange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, false)
            };

            // Act
            var result = new GoapPlanner(logger.Object).Plan(this.availableActions, worldState, this.goal);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(this.findTargetAction, result.Peek());
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return this;
        }
    }
}
