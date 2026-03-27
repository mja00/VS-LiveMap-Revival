using livemap.command.argument;
using livemap.util;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common.Database;

namespace livemap.command.subcommand;

public class ApothemRenderCmd(LiveMap server) : AbstractCommand(server,
    ["apothemrender", "radiusrender", "rangerender"],
    argParsers: [
        new ApothemArgParser("apothem"),
        new CenterPositionArgParser("center", server.Sapi)
    ]) {
    public override TextCommandResult Execute(TextCommandCallingArgs args) {
        IWorldManagerAPI world = _server.Sapi.WorldManager;
        int mapX = world.MapSizeX;
        int mapZ = world.MapSizeZ;

        int apothem = (int)args[0];
        if (apothem < 0 || apothem > Math.Max(mapX, mapZ)) {
            return "apothemrender.out-of-bounds".CommandError();
        }

        Vec2i? blockPos = args[1] as Vec2i;
        if (blockPos == null) {
            if (args.Caller.Player == null) {
                return "apothemrender.missing-center-or-player".CommandError();
            }

            EntityPos pos = args.Caller.Player.Entity.Pos;
            blockPos = new Vec2i((int)pos.X, (int)pos.Z);
        }

        new Thread(_ => {
            if (_server.RenderTaskManager == null) {
                return;
            }

            // queue up all existing chunks within range
            Vec2i min = new(Math.Max(0, blockPos.X - apothem) >> 9, Math.Max(0, blockPos.Y - apothem) >> 9);
            Vec2i max = new(Math.Min(mapX, blockPos.X + apothem) >> 9, Math.Min(mapZ, blockPos.Y + apothem) >> 9);
            foreach (ChunkPos regionPos in _server.RenderTaskManager.ChunkLoader.GetAllMapRegionPositions()) {
                if (min != null && (regionPos.X < min.X || regionPos.Z < min.Y)) {
                    continue;
                }

                if (max != null && (regionPos.X > max.X || regionPos.Z > max.Y)) {
                    continue;
                }

                _server.RenderTaskManager.Queue(regionPos.X, regionPos.Z);
            }

            // trigger autosave to process queue
            _server.Sapi.AutoSaveNow();
        }).Start();

        return "apothemrender.started".CommandSuccess();
    }
}
