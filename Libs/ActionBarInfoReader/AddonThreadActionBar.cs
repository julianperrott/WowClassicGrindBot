//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;

//namespace Libs
//{
//    public class AddonThreadActionBar
//    {
//        public List<DataFrame> frames { get; private set; } = new List<DataFrame>();
//        private readonly IAddonReader addonReader;
//        private readonly ISquareReader squareReader;
//        public ActionBarInfoReader PlayerReader { get; private set; }
//        public bool Active { get; set; } = true;

//        public AddonThreadActionBar(IColorReader colorReader, List<DataFrame> frames)
//        {
//            this.frames = frames;

//            var width = frames.Last().point.X + 1;
//            var height = frames.Max(f => f.point.Y) + 1;
//            this.addonReader = new AddonReader(colorReader, frames, width, height);

//            this.squareReader = new SquareReader(addonReader);
//            this.PlayerReader = new ActionBarInfoReader(squareReader, frames);
//        }

//        public void DoWork()
//        {
//            //while (this.Active)
//            //{
//                addonReader.Refresh();

//                //logger.LogInformation($"X: {PlayerReader.XCoord.ToString("0.00")}, Y: {PlayerReader.YCoord.ToString("0.00")}, Direction: {PlayerReader.Direction.ToString("0.00")}, Zone: {PlayerReader.Zone}, Gold: {PlayerReader.Gold}");

//                //logger.LogInformation($"Enabled: {PlayerReader.ActionBarEnabledAction.value}, NotEnoughMana: {PlayerReader.ActionBarNotEnoughMana.value}, NotOnCooldown: {PlayerReader.ActionBarNotOnCooldown.value}, Charge: {PlayerReader.SpellInRange.Charge}, Rend: {PlayerReader.SpellInRange.Rend}, Shoot gun: {PlayerReader.SpellInRange.ShootGun}");
                
//                //System.Threading.Thread.Sleep(10);
//            //}
//        }
//    }
//}