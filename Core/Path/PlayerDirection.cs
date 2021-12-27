using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Numerics;
using SharedLib.Extensions;

namespace Core
{
    public class PlayerDirection : IPlayerDirection
    {
        private readonly bool debug = false;

        private readonly ILogger logger;
        private readonly ConfigurableInput input;
        private readonly PlayerReader playerReader;

        private readonly float RADIAN = MathF.PI * 2;

        private const int DefaultIgnoreDistance = 15;

        public DateTime LastSetDirection { get; private set; } = default;

        public PlayerDirection(ILogger logger, ConfigurableInput input, PlayerReader playerReader)
        {
            this.logger = logger;
            this.input = input;
            this.playerReader = playerReader;
        }

        public async ValueTask SetDirection(float desiredDirection, Vector3 point, string source)
        {
            await SetDirection(desiredDirection, point, source, DefaultIgnoreDistance);
        }

        public async ValueTask SetDirection(float desiredDirection, Vector3 point, string source, int ignoreDistance)
        {
            float distance = playerReader.PlayerLocation.DistanceXYTo(point);
            if (distance < ignoreDistance)
            {
                Log("Too close, ignoring direction change.");
                return;
            }

            await input.KeyPressNoDelay(GetDirectionKeyToPress(desiredDirection),
                TurnDuration(desiredDirection),
                debug ? $"SetDirection: {source} -- Desired: {desiredDirection:0.000} - Current: {playerReader.Direction:0.000} - Distance: {distance:0.000}" : string.Empty);

            LastSetDirection = DateTime.Now;
        }

        private float TurnAmount(float desiredDirection)
        {
            var result = (RADIAN + desiredDirection - playerReader.Direction) % RADIAN;
            if (result > MathF.PI) { result = RADIAN - result; }
            return result;
        }

        private int TurnDuration(float desiredDirection)
        {
            return (int)(TurnAmount(desiredDirection) * 1000 / MathF.PI);
        }

        private ConsoleKey GetDirectionKeyToPress(float desiredDirection)
        {
            return (RADIAN + desiredDirection - playerReader.Direction) % RADIAN < MathF.PI
                ? input.TurnLeftKey : input.TurnRightKey;
        }

        private void Log(string text)
        {
            if (debug)
            {
                logger.LogInformation($"{this.GetType().Name}: {text}");
            }
        }
    }
}