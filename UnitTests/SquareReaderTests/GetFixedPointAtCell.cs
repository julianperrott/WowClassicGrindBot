using Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace UnitTests.SquareReaderTests
{
    [TestClass]
    public class GetFixedPointAtCell
    {
        [TestMethod]
        public void GetFixedPointAtCell_AsExpected()
        {
            // Arrange
            var cell = new DataFrame(new Point(10, 1), 12);
            var addonReader = new Mock<IAddonReader>();
            addonReader.Setup(s => s.GetColorAt(cell)).Returns(Color.FromArgb(110, 89, 57));
            var reader = new SquareReader( addonReader.Object);
            var longValue = (110 * 65536) + (89 * 256) + 57;

            // Act
            var result = reader.GetFixedPointAtCell(cell);

            // Assert
            Assert.AreEqual(((double)longValue) / 100000, result);
        }

        [TestMethod]
        public void GetFixedPointAtCell_MaxValue()
        {
            // Arrange
            var cell = new DataFrame(new Point(10, 1), 12);
            var addonReader = new Mock<IAddonReader>();
            addonReader.Setup(s => s.GetColorAt(cell)).Returns(Color.FromArgb(255, 255, 255));
            var reader = new SquareReader( addonReader.Object);
            var longValue = (255 * 65536) + (255 * 256) + 255;

            // Act
            var result = reader.GetFixedPointAtCell(cell);

            // Assert
            Assert.AreEqual(((double)longValue) / 100000, result);
        }
    }
}
