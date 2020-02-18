using System;
using System.Collections.Generic;
using System.Text;
using Libs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Drawing;
using System.Linq;

namespace UnitTests.BagReaderTests
{
    [TestClass]
    public class Read
    {
        [TestMethod]
        public void Read2ItemsFromBag()
        {
            // Arrange
            var cells = Enumerable.Range(1, 50).Select(i => new DataFrame(new Point(1, 1), i)).ToList();
            var reader = new Mock<ISquareReader>();
            var bagreader = new BagReader(reader.Object, 20, cells);

            reader.Setup(s => s.Get5Numbers(It.IsAny<DataFrame>(), It.IsAny<SquareReader.Part>())).Returns(0); // itemCount
            reader.Setup(s => s.GetLongAtCell(It.IsAny<DataFrame>())).Returns(0); // itemCount

            reader.Setup(s => s.Get5Numbers(cells[20], SquareReader.Part.Left)).Returns(11); // itemCount
            reader.Setup(s => s.Get5Numbers(cells[20], SquareReader.Part.Right)).Returns(1234); // ItemId
            reader.Setup(s => s.GetLongAtCell(cells[21])).Returns(16 * 3 + 13); // Bag 3, index 13

            reader.Setup(s => s.Get5Numbers(cells[22], SquareReader.Part.Left)).Returns(3); // itemCount
            reader.Setup(s => s.Get5Numbers(cells[22], SquareReader.Part.Right)).Returns(434); // ItemId
            reader.Setup(s => s.GetLongAtCell(cells[23])).Returns(16 * 5 + 7); // Bag 5, index 7

            // Act
            var items = bagreader.Read();

            // Assert
            Assert.AreEqual(2, items.Count);
            var bag0Item13 = items.Where(i => i.Bag == 0 && i.BagIndex == 13).FirstOrDefault();
            Assert.AreEqual(11, bag0Item13.Count);
            Assert.AreEqual(1234, bag0Item13.ItemId);

            var bag1Item7 = items.Where(i => i.Bag == 1 && i.BagIndex == 7).FirstOrDefault();
            Assert.AreEqual(3, bag1Item7.Count);
            Assert.AreEqual(434, bag1Item7.ItemId);
        }

        [TestMethod]
        public void Read2ItemsFromBag_Twice()
        {
            // Arrange
            var cells = Enumerable.Range(1, 50).Select(i => new DataFrame(new Point(1, 1), i)).ToList();
            var reader = new Mock<ISquareReader>();
            var bagreader = new BagReader(reader.Object, 20, cells);

            reader.Setup(s => s.Get5Numbers(It.IsAny<DataFrame>(), It.IsAny<SquareReader.Part>())).Returns(0); // itemCount
            reader.Setup(s => s.GetLongAtCell(It.IsAny<DataFrame>())).Returns(0); // itemCount

            reader.Setup(s => s.Get5Numbers(cells[20], SquareReader.Part.Left)).Returns(11); // itemCount
            reader.Setup(s => s.Get5Numbers(cells[20], SquareReader.Part.Right)).Returns(1234); // ItemId
            reader.Setup(s => s.GetLongAtCell(cells[21])).Returns(16 * 3 + 13); // Bag 3, index 13

            reader.Setup(s => s.Get5Numbers(cells[22], SquareReader.Part.Left)).Returns(3); // itemCount
            reader.Setup(s => s.Get5Numbers(cells[22], SquareReader.Part.Right)).Returns(434); // ItemId
            reader.Setup(s => s.GetLongAtCell(cells[23])).Returns(16 * 5 + 7); // Bag 5, index 7

            // Act
            bagreader.Read();
            var items = bagreader.Read();

            // Assert
            Assert.AreEqual(2, items.Count);
            var bag0Item13 = items.Where(i => i.Bag == 0 && i.BagIndex == 13).FirstOrDefault();
            Assert.AreEqual(11, bag0Item13.Count);
            Assert.AreEqual(1234, bag0Item13.ItemId);

            var bag1Item7 = items.Where(i => i.Bag == 1 && i.BagIndex == 7).FirstOrDefault();
            Assert.AreEqual(3, bag1Item7.Count);
            Assert.AreEqual(434, bag1Item7.ItemId);
        }

        [TestMethod]
        public void Read2ItemsFromBag_ItemRemoved()
        {
            // Arrange
            var cells = Enumerable.Range(1, 50).Select(i => new DataFrame(new Point(1, 1), i)).ToList();
            var reader = new Mock<ISquareReader>();
            var bagreader = new BagReader(reader.Object, 20, cells);

            reader.Setup(s => s.Get5Numbers(It.IsAny<DataFrame>(), It.IsAny<SquareReader.Part>())).Returns(0); // itemCount
            reader.Setup(s => s.GetLongAtCell(It.IsAny<DataFrame>())).Returns(0); // itemCount

            reader.Setup(s => s.Get5Numbers(cells[20], SquareReader.Part.Left)).Returns(11); // itemCount
            reader.Setup(s => s.Get5Numbers(cells[20], SquareReader.Part.Right)).Returns(1234); // ItemId
            reader.Setup(s => s.GetLongAtCell(cells[21])).Returns(16 * 3 + 13); // Bag 3, index 13

            reader.Setup(s => s.Get5Numbers(cells[22], SquareReader.Part.Left)).Returns(3); // itemCount
            reader.Setup(s => s.Get5Numbers(cells[22], SquareReader.Part.Right)).Returns(434); // ItemId
            reader.Setup(s => s.GetLongAtCell(cells[23])).Returns(16 * 5 + 7); // Bag 5, index 7

            // Act
            bagreader.Read();
            var items = bagreader.Read();

            // Assert
            Assert.AreEqual(2, items.Count);
            var bag0Item13 = items.Where(i => i.Bag == 0 && i.BagIndex == 13).FirstOrDefault();
            Assert.AreEqual(11, bag0Item13.Count);
            Assert.AreEqual(1234, bag0Item13.ItemId);

            var bag1Item7 = items.Where(i => i.Bag == 1 && i.BagIndex == 7).FirstOrDefault();
            Assert.AreEqual(3, bag1Item7.Count);
            Assert.AreEqual(434, bag1Item7.ItemId);

            // remove item
            reader.Setup(s => s.Get5Numbers(cells[22], SquareReader.Part.Left)).Returns(0);

            // Act
            bagreader.Read();
            items = bagreader.Read();

            // Assert
            Assert.AreEqual(1, items.Count);
        }
    }
}
