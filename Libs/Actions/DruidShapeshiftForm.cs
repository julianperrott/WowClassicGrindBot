using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Libs.Actions
{
    //public class DruidShapeshiftForm
    //{
    //    protected readonly WowProcess wowProcess;
    //    protected readonly PlayerReader playerReader;
    //    protected ILogger logger;

    //    public DruidShapeshiftForm(WowProcess wowProcess, PlayerReader playerReader, ILogger logger)
    //    {
    //        this.wowProcess = wowProcess;
    //        this.playerReader = playerReader;
    //        this.logger = logger;
    //    }

    //    private static Dictionary<ShapeshiftForm, ConsoleKey> formKeys = new Dictionary<ShapeshiftForm, ConsoleKey>
    //    {
    //        { ShapeshiftForm.None, ConsoleKey.F8},
    //        { ShapeshiftForm.Druid_Bear, ConsoleKey.D4},
    //        { ShapeshiftForm.Druid_Travel, ConsoleKey.D6},
    //    };

    //    public async Task UseShapeshiftForm(ShapeshiftForm desiredForm)
    //    {
    //        if (this.playerReader.Druid_ShapeshiftForm != desiredForm)
    //        {
    //            if (desiredForm != ShapeshiftForm.None)
    //            {
    //                await this.wowProcess.KeyPress(formKeys[ShapeshiftForm.None], 152); // cancelform
    //            }

    //            await this.wowProcess.KeyPress(formKeys[desiredForm], 150); // desiredFrom
    //        }
    //    }
    //}
}
