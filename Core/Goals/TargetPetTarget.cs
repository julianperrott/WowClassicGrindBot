using Core.GOAP;
using System.Threading.Tasks;

namespace Core.Goals
{
    public class TargetPetTarget : GoapGoal
    {
        public override float CostOfPerformingAction { get => 4.01f; }
        public override bool Repeatable => false;

        private readonly ConfigurableInput input;
        private readonly PlayerReader playerReader;

        public TargetPetTarget(ConfigurableInput input, PlayerReader playerReader)
        {
            this.input = input;
            this.playerReader = playerReader;

            AddPrecondition(GoapKey.incombat, true);
            AddPrecondition(GoapKey.hastarget, false);
            AddPrecondition(GoapKey.pethastarget, true);

            AddEffect(GoapKey.hastarget, true);
        }

        public override async Task PerformAction()
        {
            await input.TapTargetPet();
            await input.TapTargetOfTarget();
            if (playerReader.HasTarget && playerReader.Bits.TargetIsDead)
            {
                await input.TapClearTarget();
            }
        }
    }
}
