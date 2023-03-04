using System.Numerics;
using Maple2.Tools.Collision;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maple2.Server.Tests.Tools.Collision;

[TestClass]
public class HoleHoleCircleTest {
    [TestMethod]
    public void ContainsTest() {
        var origin = new Vector2(1, 1);
        const float innerRadius = 2;
        const float outerRadius = 4;
        var circle = new HoleCircle(origin, innerRadius, outerRadius);

        var insidePoint = new Vector2(3.5f, 3.5f);
        Assert.IsTrue(circle.Contains(insidePoint));
        var outerEdgePoint = new Vector2(5, 1);
        Assert.IsTrue(circle.Contains(outerEdgePoint));

        var innerEdgePoint = new Vector2(3, 1);
        Assert.IsFalse(circle.Contains(innerEdgePoint));
        var holePoint = new Vector2(1.5f, 1.5f);
        Assert.IsFalse(circle.Contains(holePoint));
        var outsidePoint = new Vector2(5.5f, 2);
        Assert.IsFalse(circle.Contains(outsidePoint));
    }

    [TestMethod]
    public void RectangleIntersectsTest() {
        var circle = new HoleCircle(new Vector2(1, 1), 2, 4);
        {
            var rectangle = new Rectangle(new Vector2(1, 2), 4, 6, 0);
            Assert.IsTrue(circle.Intersects(rectangle));
        }
        {
            var rectangle = new Rectangle(new Vector2(4, 4), 2, 2, 0);
            Assert.IsFalse(circle.Intersects(rectangle));
        }
        {
            // Rectangle contained within hole.
            var rectangle = new Rectangle(new Vector2(1, 1), 1, 1, 0);
            Assert.IsFalse(circle.Intersects(rectangle));
        }
    }

    [TestMethod]
    public void TrapezoidIntersectsTest() {
        var circle = new HoleCircle(new Vector2(1, 1), 2, 4);
        {
            var trapezoid = new Trapezoid(new Vector2(1, 2), 4, 6, 3, 0);
            Assert.IsTrue(circle.Intersects(trapezoid));
        }
        {
            var trapezoid = new Trapezoid(new Vector2(4, 4), 4, 6, 3, 0);
            Assert.IsFalse(circle.Intersects(trapezoid));
        }
        {
            // Trapezoid contained within hole.
            var trapezoid = new Trapezoid(new Vector2(1, 1), 1, 2, 1, 0);
            Assert.IsFalse(circle.Intersects(trapezoid));
        }
    }

    [TestMethod]
    public void CircleIntersectsTest() {
        var circle = new HoleCircle(new Vector2(1, 1), 2, 4);
        {
            var other = new Circle(new Vector2(3, 3), 3);
            Assert.IsTrue(circle.Intersects(other));
        }
        {
            var other = new Circle(new Vector2(8, 8), 3);
            Assert.IsFalse(circle.Intersects(other));
        }
        {
            // Circle contained within hole
            var other = new Circle(new Vector2(1, 1), 1.9f);
            Assert.IsFalse(circle.Intersects(other));
        }
    }

    [TestMethod]
    public void HoleCircleIntersectsTest() {
        var circle = new HoleCircle(new Vector2(1, 1), 2, 4);
        {
            var other = new HoleCircle(new Vector2(1, 2), 2, 4);
            Assert.IsTrue(circle.Intersects(other));
        }
        {
            var other = new HoleCircle(new Vector2(8, 8), 2, 3);
            Assert.IsFalse(circle.Intersects(other));
        }
        {
            // HoleCircle contained within hole.
            var other = new HoleCircle(new Vector2(1, 1), 1, 1.9f);
            Assert.IsFalse(circle.Intersects(other));
        }
    }
}
