using Core.GOAP;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class TargetPetTarget : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.01f; }

        private readonly ConfigurableInput input;
        private readonly PlayerReader playerReader;

        public TargetPetTarget(ConfigurableInput input, PlayerReader playerReader)
        {
            this.input = input;
            this.playerReader = playerReader;

            AddPrecondition(GoapKey.dangercombat, true);
            AddPrecondition(GoapKey.hastarget, false);
            AddPrecondition(GoapKey.pethastarget, true);

            AddEffect(GoapKey.hastarget, true);
        }

        public override ValueTask PerformAction()
        {
            input.TapTargetPet();
            input.TapTargetOfTarget();
            if (playerReader.HasTarget && (playerReader.Bits.TargetIsDead || playerReader.TargetGuid == playerReader.PetGuid))
            {
                input.TapClearTarget();
            }

            return ValueTask.CompletedTask;
        }
    }
}
