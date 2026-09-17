using System;
using System.Collections.Generic;
using ForbiddenTechnologyPack.Core;

namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    internal static class ProtoMatterInterferenceManager {
        private sealed class SourceState {
            internal SourceState(int worldId, int x, int y, int radius) {
                WorldId = worldId;
                X = x;
                Y = y;
                Radius = radius;
            }

            internal int WorldId { get; private set; }
            internal int X { get; private set; }
            internal int Y { get; private set; }
            internal int Radius { get; private set; }
        }

        private static readonly HashSet<ForbiddenTechDevice> devices =
            new HashSet<ForbiddenTechDevice>();
        private static readonly Dictionary<string, SourceState> sources =
            new Dictionary<string, SourceState>(StringComparer.Ordinal);

        internal static void Register(ForbiddenTechDevice device) {
            if (device == null || !devices.Add(device)) {
                return;
            }

            foreach (var pair in sources) {
                device.SetInterferenceSource(pair.Key, IsAffected(device, pair.Value));
            }
        }

        internal static void Unregister(ForbiddenTechDevice device) {
            if (device != null) {
                devices.Remove(device);
            }
        }

        internal static void ApplySource(string sourceId, int sourceCell, int radius) {
            if (string.IsNullOrEmpty(sourceId) || radius < 0 || !Grid.IsValidCell(sourceCell)) {
                return;
            }

            RemoveSource(sourceId);

            var position = Grid.CellToXY(sourceCell);
            var source = new SourceState(Grid.WorldIdx[sourceCell], position.x, position.y, radius);
            sources[sourceId] = source;

            foreach (var device in devices) {
                if (device != null && IsAffected(device, source)) {
                    device.SetInterferenceSource(sourceId, true);
                }
            }
        }

        internal static void RemoveSource(string sourceId) {
            if (string.IsNullOrEmpty(sourceId) || !sources.Remove(sourceId)) {
                return;
            }

            foreach (var device in devices) {
                if (device != null) {
                    device.SetInterferenceSource(sourceId, false);
                }
            }
        }

        private static bool IsAffected(ForbiddenTechDevice device, SourceState source) {
            var cell = Grid.PosToCell(device);
            if (!Grid.IsValidCell(cell) || Grid.WorldIdx[cell] != source.WorldId) {
                return false;
            }

            var position = Grid.CellToXY(cell);
            return ProtoMatterInterferencePolicy.IsInsideRadius(
                source.X, source.Y, position.x, position.y, source.Radius);
        }
    }
}
