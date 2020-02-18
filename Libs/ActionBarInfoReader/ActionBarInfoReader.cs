//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace Libs
//{
//    public class ActionBarInfoReader
//    {
//        private readonly ISquareReader reader;
//        private readonly List<DataFrame> f;

//        public ActionBarInfoReader(ISquareReader reader, List<DataFrame> frames)
//        {
//            this.reader = reader;
//            this.f = frames;
//        }

//        public ActionBarStatus spellStatus_1 => new ActionBarStatus(reader.GetLongAtCell(f[1]));
//        public ActionBarStatus spellStatus_2 => new ActionBarStatus(reader.GetLongAtCell(f[2]));
//        public ActionBarStatus spellStatus_3 => new ActionBarStatus(reader.GetLongAtCell(f[3]));
//        public ActionBarStatus spellStatus_4 => new ActionBarStatus(reader.GetLongAtCell(f[4]));

//        public ActionBarStatus spellAvailable_1 => new ActionBarStatus(reader.GetLongAtCell(f[6]));
//        public ActionBarStatus spellAvailable_2 => new ActionBarStatus(reader.GetLongAtCell(f[7]));
//        public ActionBarStatus spellAvailable_3 => new ActionBarStatus(reader.GetLongAtCell(f[8]));
//        public ActionBarStatus spellAvailable_4 => new ActionBarStatus(reader.GetLongAtCell(f[9]));

//        public ActionBarStatus notEnoughMana_1 => new ActionBarStatus(reader.GetLongAtCell(f[11]));
//        public ActionBarStatus notEnoughMana_2 => new ActionBarStatus(reader.GetLongAtCell(f[12]));
//        public ActionBarStatus notEnoughMana_3 => new ActionBarStatus(reader.GetLongAtCell(f[13]));
//        public ActionBarStatus notEnoughMana_4 => new ActionBarStatus(reader.GetLongAtCell(f[14]));

//        public ActionBarStatus isUsableActionUsable_1 => new ActionBarStatus(reader.GetLongAtCell(f[16]));
//        public ActionBarStatus isUsableActionUsable_2 => new ActionBarStatus(reader.GetLongAtCell(f[17]));
//        public ActionBarStatus isUsableActionUsable_3 => new ActionBarStatus(reader.GetLongAtCell(f[18]));
//        public ActionBarStatus isUsableActionUsable_4 => new ActionBarStatus(reader.GetLongAtCell(f[19]));
//    }
//}
