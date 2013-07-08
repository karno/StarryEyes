using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StarryEyes.Albireo.Data;

namespace StarryEyes.Albireo.Test
{
    [TestClass]
    public class AVLTreeTest
    {
        [TestMethod]
        public void AddTest()
        {
            // add elements
            var tree = new AVLTree<int>();
            tree.Add(1);
            tree.Add(2);
            tree.Add(3);
            Assert.AreEqual(tree.Select(i => i.ToString()).JoinString(","), "1,2,3");
        }

        [TestMethod]
        public void AddDistinctTest()
        {
            // add elements
            var tree = new AVLTree<int>();
            tree.Add(1);
            tree.Add(1);
            tree.Add(1);
            Assert.AreEqual(tree.Select(i => i.ToString()).JoinString(","), "1");
        }

        [TestMethod]
        public void RemoveTest()
        {
            // add elements
            var tree = new AVLTree<int>();
            tree.Add(1);
            tree.Add(2);
            tree.Add(3);
            tree.Remove(3);
            tree.Remove(2);
            Assert.AreEqual(tree.Select(i => i.ToString()).JoinString(","), "1");
        }

        [TestMethod]
        public void InversionTest()
        {
            // add elements
            var tree = new AVLTree<int>(Comparer<int>.Create((i, j) => j - i));
            tree.Add(1);
            tree.Add(2);
            tree.Add(3);
            Assert.AreEqual(tree.Select(i => i.ToString()).JoinString(","), "3,2,1");
        }
    }
}
