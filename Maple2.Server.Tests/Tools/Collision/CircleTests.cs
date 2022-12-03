using System;
using System.Numerics;
using Maple2.Tools.Collision;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maple2.Server.Tests.Tools.Collision;

[TestClass]
public class CircleTests {
    [TestMethod]
    public void ContainsTest() {
        var origin = new Vector2(1, 1);
        const float radius = 3;
        var circle = new Circle(origin, radius);

        var insidePoint = new Vector2(1.5f, 1.5f);
        Assert.IsTrue(circle.Contains(insidePoint));
        var edgePoint = new Vector2(1, 4);
        Assert.IsTrue(circle.Contains(edgePoint));
        var outsidePoint = new Vector2(4.5f, 1.5f);
        Assert.IsFalse(circle.Contains(outsidePoint));
    }

    [TestMethod]
    public void RectangleIntersectsTest() {
        var circle = new Circle(new Vector2(1, 1), 3);
        {
            var rectangle = new Rectangle(new Vector2(1, 2), 4, 6, 0);
            Assert.IsTrue(circle.Intersects(rectangle));
        }
        {
            var rectangle = new Rectangle(new Vector2(4, 4), 2, 2, 0);
            Console.WriteLine(string.Join(",", rectangle.Points));
            Assert.IsFalse(circle.Intersects(rectangle));
        }
    }

    [TestMethod]
    public void TrapezoidIntersectsTest() {
        var circle = new Circle(new Vector2(1, 1), 3);
        {
            var trapezoid = new Trapezoid(new Vector2(1, 2), 4, 6, 3, 0);
            Assert.IsTrue(circle.Intersects(trapezoid));
        }
        {
            var trapezoid = new Trapezoid(new Vector2(4, 4), 4, 6, 3, 0);
            Assert.IsFalse(circle.Intersects(trapezoid));
        }
    }

    [TestMethod]
    public void CircleIntersectsTest() {
        var circle = new Circle(new Vector2(1, 1), 3);
        {
            var other = new Circle(new Vector2(3, 3), 3);
            Assert.IsTrue(circle.Intersects(other));
        }
        {
            var other = new Circle(new Vector2(7, 7), 3);
            Assert.IsFalse(circle.Intersects(other));
        }
    }

    [TestMethod]
    public void HoleCircleIntersectsTest() {
        var circle = new Circle(new Vector2(1, 1), 2);
        {
            var holeCircle = new HoleCircle(new Vector2(1, 2), 2, 4);
            Assert.IsTrue(circle.Intersects(holeCircle));
        }
        {
            var holeCircle = new HoleCircle(new Vector2(7, 7), 2, 4);
            Assert.IsFalse(circle.Intersects(holeCircle));
        }
        {
            // Circle contained within hole.
            var holeCircle = new HoleCircle(new Vector2(1, 1), 3, 5);
            Assert.IsFalse(circle.Intersects(holeCircle));
        }
    }
}
