using Libs;
using Libs.Actions;
using Libs.GOAP;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;

namespace UnitTests.GOAP.GoapPlannerTests
{
    [TestClass]
    public class Plan
    {
        GoapAction followRouteAction = null;
        GoapAction killMobAction = null;
        GoapAction pullTargetAction = null;
        GoapAction approachTargetAction = null;
        GoapAction findTargetAction = null;
        HashSet<GoapAction> availableActions;

        HashSet<KeyValuePair<GoapKey, GoapPreCondition>> goal = new HashSet<KeyValuePair<GoapKey, GoapPreCondition>>();

        [TestInitialize]
        public void TestInitialize()
        {
            var playerReader = new PlayerReader(new Mock<ISquareReader>().Object);
            var stopMoving = new StopMoving(new WowProcess(), playerReader);
            this.followRouteAction = new FollowRouteAction(playerReader, new WowProcess(), new Mock<IPlayerDirection>().Object, new List<WowPoint>(), stopMoving);

            this.killMobAction = new KillTargetAction(new WowProcess(), playerReader, stopMoving);
            this.pullTargetAction = new PullTargetAction(new WowProcess(), playerReader);
            this.approachTargetAction = new ApproachTargetAction(new WowProcess(), playerReader, stopMoving);

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
            var result = new GoapPlanner().Plan(this.availableActions, worldState, this.goal);

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
                new KeyValuePair<GoapKey, object>(GoapKey.inmeleerange, false)
            };

            // Act
            var result = new GoapPlanner().Plan(this.availableActions, worldState, this.goal);

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
                new KeyValuePair<GoapKey, object>(GoapKey.inmeleerange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, false)
            };

            // Act
            var result = new GoapPlanner().Plan(this.availableActions, worldState, this.goal);

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
                new KeyValuePair<GoapKey, object>(GoapKey.inmeleerange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, true)
            };

            // Act
            var result = new GoapPlanner().Plan(this.availableActions, worldState, this.goal);

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
                new KeyValuePair<GoapKey, object>(GoapKey.inmeleerange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, false)
            };

            // Act
            var result = new GoapPlanner().Plan(this.availableActions, worldState, this.goal);

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
                new KeyValuePair<GoapKey, object>(GoapKey.inmeleerange, false),
                new KeyValuePair<GoapKey, object>(GoapKey.pulled, false)
            };

            // Act
            var result = new GoapPlanner().Plan(this.availableActions, worldState, this.goal);

            // Assert
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(this.findTargetAction, result.Peek());
        }
    }
}
