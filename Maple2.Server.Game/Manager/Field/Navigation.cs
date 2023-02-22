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

    private readonly ConcurrentDictionary<(int, int), Shape> shapeCache = new();

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
        if (!TryFindPosition(shape, ToPosition(origin), distance, out Position? position)) {
            return null;
        }

        Agent agent = mesh.placeAgent(shape, (Position) position);
        context.addAgent(agent);
        return agent;
    }

    public Vector3 UpdateAgent(Agent agent, Vector3 vector) {
        Position position = mesh.positionNear3DPoint((int) vector.X, (int) vector.Y, (int) vector.Z, horizontalRange: 500, verticalRange: 50);
        if (!mesh.positionIsValid(position)) {
            Logger.Error("Failed to find valid position from {Source} => {Position}", vector, position);
            return vector;
        }

        agent.moveTo(position);
        return FromPosition(position);
    }

    public void RemoveAgent(Agent agent) {
        context.removeAgent(agent);
    }

    public (Vector3 Start, Vector3 End) FindPath(Agent agent, Vector3 origin, int maxDistance, int maxHeight = 1) {
        Position source = agent.findClosestUnobstructedPosition(context, 50);
        if (!mesh.positionIsValid(source)) {
            return default;
        }

        Position destination = mesh.generateRandomPositionLocally(ToPosition(origin), maxDistance);
        agent.moveTo(source);
        using Path? path = agent.findShortestPathTo(context, destination);
        if (path == null || path.size() <= 1) {
            return default;
        }

        Position start = path.position(0);
        Position end = path.position(1);
        int startZ = mesh.heightAtPosition(start);
        int endZ = mesh.heightAtPosition(end);
        if (Math.Abs(startZ - endZ) > maxHeight) {
            return default;
        }

        // if (Math.Abs(startZ - endZ) > 1) {
        //     Logger.Information("Path result:");
        //     for (int i = 0; i < path.size(); i++) {
        //         Position position = path.position(i);
        //         Logger.Information("> {Position}|{HeightAtPosition}, {ConnectionIndex}", position, mesh.heightAtPosition(position), path.connectionIndex(i));
        //     }
        // }

        // Only keep first section of path, the rest will be re-generated when needed.
        return (FromPosition(start), FromPosition(end));
    }

    private bool TryFindPosition(Shape? shape, Position origin, int distance, [NotNullWhen(true)] out Position? position) {
        position = origin;
        if (!mesh.positionIsValid(origin)) {
            return false;
        }

        if (distance > 1) {
            position = mesh.generateRandomPositionLocally((Position) position, distance);
            position = mesh.findClosestUnobstructedPosition(shape, context, (Position) position, distance);
        }
        if (!mesh.positionIsValid((Position) position)) {
            return false;
        }

        return true;
    }

    public Position ToPosition(Vector3 vector) {
        return mesh.positionNear3DPoint((int) vector.X, (int) vector.Y, (int) vector.Z, horizontalRange: 25, verticalRange: 5);
    }

    public Vector3 FromPosition(Position position) {
        if (!mesh.positionIsValid(position)) {
            return default;
        }

        float z = mesh.heightAtPositionF(position);
        return new Vector3(position.X, position.Y, z);
    }

    private Shape GetShape(int width, int height) {
        if (shapeCache.TryGetValue((width, height), out Shape? shape)) {
            return shape;
        }

        // TODO: Using a small width for now to prevent Npcs from getting stuck
        int halfWidth = 5; // Math.Max(width / 2, 1);
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
        shapeCache.TryAdd((width, height), shape);
        return shape;
    }

    public void Dispose() {
        mesh.Dispose();
    }
}
