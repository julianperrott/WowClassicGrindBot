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
    public class GetIntAtCell
    {
        [TestMethod]
        public void GetIntAtCell_AsExpected()
        {
            // Arrange
            var cell = new DataFrame(new Point(10, 1), 12);
            var addonReader = new Mock<IAddonReader>();
            addonReader.Setup(s=>s.GetColorAt(cell)).Returns(Color.FromArgb(110, 89, 57));
            var reader = new SquareReader(addonReader.Object);

            // Act
            var result = reader.GetLongAtCell(cell);

            // Assert
            Assert.AreEqual((110 * 65536) + (89 * 256) + 57, result);
        }
    }
}
