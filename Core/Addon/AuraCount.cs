using System;
using System.Collections.Generic;
using System.Text;

namespace Core
{
    public class AuraCount
    {
        public int Hash { private set; get; }
        public int PlayerDebuff { private set; get; }
        public int PlayerBuff { private set; get; }
        public int TargetDebuff { private set; get; }
        public int TargetBuff { private set; get; }

        public AuraCount(ISquareReader squareReader, int cell)
        {
            Hash = TargetBuff = (int)squareReader.GetLongAtCell(cell);

            // formula
            // playerDebuffCount * 1000000 + playerBuffCount * 10000 + targetDebuffCount * 100 + targetBuffCount

            PlayerDebuff = (int)(TargetBuff / 1000000f);
            TargetBuff -= 1000000 * PlayerDebuff;

            PlayerBuff = (int)(TargetBuff / 10000f);
            TargetBuff -= 10000 * PlayerBuff;

            TargetDebuff = (int)(TargetBuff / 100f);
            TargetBuff -= 100 * TargetDebuff;
        }
    }
}
