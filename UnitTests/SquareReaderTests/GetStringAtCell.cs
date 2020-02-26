using Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;

namespace UnitTests.SquareReaderTests
{
    [TestClass]
    public class GetStringAtCell
    {
        [TestMethod]
        public void GetStringAtCell_AsExpected()
        {
            // Arrange
            var cell = new DataFrame(new Point(10, 1), 12);

            var text = ((int)'D').ToString() + ((int)'O').ToString() + ((int)'G').ToString();
            var colorLong = long.Parse(text);
            var color = ColorHelper.LongToColour(colorLong);

            var addonReader = new Mock<IAddonReader>();
            addonReader.Setup(s => s.GetColorAt(cell.index)).Returns(color);
            var reader = new SquareReader( addonReader.Object);

            // Act
            var result = reader.GetStringAtCell(cell.index);

            // Assert
            Assert.AreEqual("DOG", result);
        }

    }
}