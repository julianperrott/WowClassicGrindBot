using System;
using System.Collections.Generic;
using System.Text;

namespace Libs
{
    public class ClassConfiguration
    {
        public string ClassName { get; set; } = string.Empty;
        public KeyConfigurations Pull { get; set; } = new KeyConfigurations();
        public KeyConfigurations Combat { get; set; } = new KeyConfigurations();
        public KeyConfigurations Buffs { get; set; } = new KeyConfigurations();
    }
    public class KeyConfigurations
    {
        public List<KeyConfiguration> Sequence { get; set; } = new List<KeyConfiguration>();
    }

    public class KeyConfiguration
    {
        public string Name { get; set; } = string.Empty;
        public bool HasCastBar { get; set; }
        public bool StopBeforeCast { get; set; }
        public ConsoleKey Key { get; set; }
        public int PressDuration { get; set; } = 200;
        public int ShapeShiftForm { get; set; } = 0;
        public bool CastIfAddsVisible { get; set; } = true;
        public int CastIfHealthBelowPercentage { get; set; } = 0;
        public int Cooldown { get; set; } = 0;
        public int ManaRequirement { get; set; } = 0;

        public string Buff { get; set; } = string.Empty;
    }
}
