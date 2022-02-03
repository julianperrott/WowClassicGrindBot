using Core.GOAP;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

namespace Core.Goals
{
    public partial class WalkToCorpseGoal : GoapGoal, IRouteProvider
    {
        public override float CostOfPerformingAction { get => 1f; }

        private readonly ILogger logger;
        private readonly Wait wait;
        private readonly ConfigurableInput input;

        private readonly AddonReader addonReader;
        private readonly PlayerReader playerReader;
        private readonly Navigation navigation;
        private readonly StopMoving stopMoving;

        public List<Vector3> Deaths { get; private init; } = new();

        private readonly Random random = new();

        #region IRouteProvider

        public DateTime LastActive => navigation.LastActive;

        public List<Vector3> PathingRoute()
        {
            return navigation.TotalRoute;
        }

        public bool HasNext()
        {
            return navigation.HasNext();
        }

        public Vector3 NextPoint()
        {
            return navigation.NextPoint();
        }

        #endregion

        public WalkToCorpseGoal(ILogger logger, ConfigurableInput input, Wait wait, AddonReader addonReader, Navigation navigation, StopMoving stopMoving)
        {
            this.logger = logger;
            this.wait = wait;
            this.input = input;

            this.addonReader = addonReader;
            this.playerReader = addonReader.PlayerReader;
            this.stopMoving = stopMoving;

            this.navigation = navigation;

            AddPrecondition(GoapKey.isdead, true);
        }

        public override void OnActionEvent(object sender, ActionEventArgs e)
        {
            if (e.Key == GoapKey.resume)
            {
                navigation.ResetStuckParameters();
            }
        }

        public override ValueTask OnEnter()
        {
            playerReader.ZCoord = 0;
            addonReader.PlayerDied();

            wait.While(() => playerReader.CorpseLocation == Vector3.Zero);
            Log($"Player teleported to the graveyard!");

            var corpseLocation = playerReader.CorpseLocation;
            Log($"Corpse location is {corpseLocation}");

            Deaths.Add(corpseLocation);

            navigation.SetWayPoints(new List<Vector3>() { corpseLocation });

            return base.OnEnter();
        }

        public override ValueTask OnExit()
        {
            navigation.Stop();

            return base.OnExit();
        }

        public override async ValueTask PerformAction()
        {
            if (!playerReader.Bits.IsCorpseInRange)
            {
                await navigation.Update();
            }
            else
            {
                stopMoving.Stop();
                navigation.ResetStuckParameters();
            }

            RandomJump();

            wait.Update(1);
        }

        private void RandomJump()
        {
            if (input.ClassConfig.Jump.MillisecondsSinceLastClick > random.Next(5000, 7000))
            {
                input.TapJump("Random jump");
            }
        }

        private void Log(string text)
        {
            logger.LogInformation($"[{nameof(WalkToCorpseGoal)}]: {text}");
        }
    }
}