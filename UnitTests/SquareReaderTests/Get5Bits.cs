using Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;

namespace UnitTests.SquareReaderTests
{
    [TestClass]
    public class Get5Bits
    {
        [TestMethod]
        public void Get5Bits_Right()
        {
            // Arrange
            var cell = new DataFrame(new Point(10, 1), 12);
            var addonReader = new Mock<IAddonReader>();
            addonReader.Setup(s => s.GetColorAt(cell.index)).Returns(ColorHelper.LongToColour(16654321));
            var reader = new SquareReader(addonReader.Object);

            // Act
            var result = reader.Get5Numbers(cell.index, SquareReader.Part.Right);

            // Assert
            Assert.AreEqual(54321, result);
        }

        [TestMethod]
        public void Get5Bits_Left()
        {
            // Arrange
            var cell = new DataFrame(new Point(10, 1), 12);
            var addonReader = new Mock<IAddonReader>();
            addonReader.Setup(s => s.GetColorAt(cell.index)).Returns(ColorHelper.LongToColour(16654321));
            var reader = new SquareReader(addonReader.Object);

            // Act
            var result = reader.Get5Numbers(cell.index, SquareReader.Part.Left);

            // Assert
            Assert.AreEqual(166, result);
        }
    }
}