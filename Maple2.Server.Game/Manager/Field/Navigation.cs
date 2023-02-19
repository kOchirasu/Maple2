using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Maple2.Model.Metadata;
using Maple2.PathEngine;
using Maple2.PathEngine.Interface;
using Maple2.PathEngine.Types;
using Maple2.Server.Game.Util;
using Serilog;

namespace Maple2.Server.Game.Manager.Field;

public sealed class Navigation : IDisposable {
    private static readonly ILogger Logger = Log.Logger.ForContext<Navigation>();

    // ErrorHandler must be initialized here to retain ownership.
    private static readonly LogErrorHandler ErrorHandler = new(Logger);
    private static readonly PathEngine.PathEngine PathEngine = new(ErrorHandler);

    public readonly string Name;
    private readonly Mesh mesh;
    private readonly CollisionContext context;

    private readonly ConcurrentDictionary<(int, int), Shape> ShapeCache = new();

    public Navigation(string name, byte[]? data = null) {
        Name = name;
        if (data == null) {
            Logger.Error("No navigation mesh for: {XBlock}", name);
            mesh = PathEngine.buildMeshFromContent(Array.Empty<IFaceVertexMesh>());
        } else {
            mesh = PathEngine.loadMeshFromBuffer(FileFormat.tok, data);
        }
        context = mesh.newContext();
    }

    public Agent? AddAgent(NpcMetadata npc, Vector3 origin, int distance = 1) {
        // Using radius for width for now
        Shape shape = GetShape((int) npc.Property.Capsule.Radius, (int) npc.Property.Capsule.Height);
        if (!TryFindPosition(shape, origin, distance, out Position? position)) {
            return null;
        }

        Agent agent = mesh.placeAgent(shape, (Position) position);
        context.addAgent(agent);
        return agent;
    }

    public void RemoveAgent(Agent agent) {
        context.removeAgent(agent);
    }

    public Vector3 ResolvePosition(Position position) {
        if (!mesh.positionIsValid(position)) {
            return default;
        }

        float z = mesh.heightAtPositionF(position);
        return new Vector3(position.X, position.Y, z);
    }

    private bool TryFindPosition(Shape shape, Vector3 origin, int distance, [NotNullWhen(true)] out Position? position) {
        Position point = mesh.positionNear3DPoint((int) origin.X, (int) origin.Y, (int) origin.Z, horizontalRange: 50, verticalRange: 50);
        if (distance > 1) {
            point = mesh.generateRandomPositionLocally(point, distance);
            point = mesh.findClosestUnobstructedPosition(shape, context, point, distance);
        }
        if (!mesh.positionIsValid(point)) {
            position = null;
            return false;
        }

        position = point;
        return true;
    }

    private Shape GetShape(int width, int height) {
        if (ShapeCache.TryGetValue((width, height), out Shape? shape)) {
            return shape;
        }

        int halfWidth = Math.Max(width / 2, 1);
        int halfHeight = Math.Max(height / 2, 1);
        List<Point> vertices = new() {
            new Point(-halfWidth, -halfHeight),
            new Point(-halfWidth, halfHeight),
            new Point(halfWidth, halfHeight),
            new Point(halfWidth, -halfHeight),
        };

        shape = PathEngine.newShape(vertices);
        mesh.generateUnobstructedSpaceFor(shape, true);
        mesh.generatePathfindPreprocessFor(shape);
        ShapeCache.TryAdd((width, height), shape);
        return shape;
    }

    public void Dispose() {
        mesh.Dispose();
    }
}
