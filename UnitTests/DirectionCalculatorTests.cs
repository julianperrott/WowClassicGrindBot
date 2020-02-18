using Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace UnitTests
{
    [TestClass]
    public class DirectionCalculatorTests
    {
        [TestMethod]
        public void Test()
        {
            Test(6.2251, 5.3207, 2.71815);
            Test(6.1449,5.3235,3.77765);
            Test(6.103800000000001,5.421,4.67771);
            Test(6.1362000000000005,5.5543,5.70657);
            Test(6.1803,5.5714,6.15187);
            Test(6.2143999999999995,5.5525,0.21426);
            Test(6.2623,5.4813,1.03814);
            Test(6.24,5.417999999999999,1.73321);

        }

        public void Test(double x, double y, double direction)
        {
            var dc = new DirectionCalculator();
            var target = new WowPoint(6.1899999999999995, 5.4147);

            var calcdirection = dc.CalculateHeading(new WowPoint(x, y), target);
            System.Diagnostics.Debug.WriteLine($"{x.ToString("0.00")},{y.ToString("0.00")} expected {direction.ToString("0.00")} actual: {calcdirection.ToString("0.00")} = {direction-calcdirection}");

        }
    }
}
