using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class PathHelperTests
    {
        [TestMethod]
        public void MargePaths_NormalCase()
        {
            // Arrange
            var first = new string[] { "1", "2", "3" };
            var second = new string[] { "4", "5" };
            var expected = new string[] { "1", "2", "3", "4", "5"};

            // Act
            var result = Path.MergePaths(first, second);

            // Assert
            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MargePaths_SecondNull()
        {
            // Arrange
            var first = new string[] { "1", "2", "3" };
            string[] second = null;

            // Act
            var result = Path.MergePaths(first, second);

            // Assert
            CollectionAssert.AreEqual(first, result);
        }

        [TestMethod]
        public void MargePaths_FirstLarge()
        {
            // Arrange
            var first = new string[] { "1", "2", "3","4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19" };
            var second = new string[] { "20", "21" };
            var expected = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };

            // Act
            var result = Path.MergePaths(first, second);

            // Assert
            CollectionAssert.AreEqual(expected, result);
        }

        [TestMethod]
        public void MargePaths_FirstMax()
        {
            // Arrange
            var first = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };
            var second = new string[] { "21", "22" };
            var expected = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "20" };

            // Act
            var result = Path.MergePaths(first, second);

            // Assert
            CollectionAssert.AreEqual(expected, result);
        }
    }
}