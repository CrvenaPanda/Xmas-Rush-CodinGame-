using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class CoordTests
    {
        #region FixCoord

        [DataTestMethod]
        [DataRow(2, 2, 3, 2, MoveDir.Right, 2)]
        [DataRow(2, 1, 1, 1, MoveDir.Left, 1)]
        [DataRow(3, 3, 3, 4, MoveDir.Down, 3)]
        [DataRow(2, 2, 2, 1, MoveDir.Up, 2)]
        public void FixCoord_InsideBoard_Item(int x, int y, int expX, int expY, MoveDir dir, int index)
        {
            // Arrange
            var coord = new Coord(x, y);
            var expected = new Coord(expX, expY);

            // Act
            var result = Coord.Fix(coord, dir, index);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow(2, 2, 3, 2, MoveDir.Right, 2)]
        [DataRow(2, 1, 1, 1, MoveDir.Left, 1)]
        [DataRow(3, 3, 3, 4, MoveDir.Down, 3)]
        [DataRow(2, 2, 2, 1, MoveDir.Up, 2)]
        public void FixCoord_InsideBoard_Player(int x, int y, int expX, int expY, MoveDir dir, int index)
        {
            // Arrange
            var coord = new Coord(x, y);
            var expected = new Coord(expX, expY);

            // Act
            var result = Coord.Fix(coord, dir, index, true);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow(6, 2, -1, -1, MoveDir.Right, 2)]
        [DataRow(0, 4, -1, -1, MoveDir.Left, 4)]
        [DataRow(3, 6, -1, -1, MoveDir.Down, 3)]
        public void FixCoord_OnBorder_Item(int x, int y, int expX, int expY, MoveDir dir, int index)
        {
            // Arrange
            var coord = new Coord(x, y);
            var expected = new Coord(expX, expY);

            // Act
            var result = Coord.Fix(coord, dir, index);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow(6, 2, 0, 2, MoveDir.Right, 2)]
        [DataRow(0, 4, 6, 4, MoveDir.Left, 4)]
        [DataRow(3, 6, 3, 0, MoveDir.Down, 3)]
        public void FixCoord_OnBorder_Player(int x, int y, int expX, int expY, MoveDir dir, int index)
        {
            // Arrange
            var coord = new Coord(x, y);
            var expected = new Coord(expX, expY);

            // Act
            var result = Coord.Fix(coord, dir, index, true);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [DataRow(-1, -1, 0, 2, MoveDir.Right, 2)]
        [DataRow(-1, -1, 4, 6, MoveDir.Up, 4)]
        public void FixCoord_Outside_Item(int x, int y, int expX, int expY, MoveDir dir, int index)
        {
            // Arrange
            var coord = new Coord(x, y);
            var expected = new Coord(expX, expY);

            // Act
            var result = Coord.Fix(coord, dir, index);

            // Assert
            Assert.AreEqual(expected, result);
        }

        #endregion

    }
}