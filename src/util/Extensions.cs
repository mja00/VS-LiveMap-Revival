using System.Reflection;
using livemap.data;
using livemap.layer.marker.options;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace livemap.util;

public static class Extensions {
    private const BindingFlags _flags = BindingFlags.NonPublic | BindingFlags.Instance;

    public static T? GetField<T>(this object obj, string name) where T : class => obj.GetType().GetField(name, _flags)?.GetValue(obj) as T;

    public static void AddIfNotExists<T>(this List<T> list, T value) {
        if (!list.Contains(value)) {
            list.Add(value);
        }
    }

    public static string ToLang(this string key, params object[]? args) => Lang.Get($"livemap:{key}", args);

    public static TextCommandResult CommandError(this string key, params object[]? args) => TextCommandResult.Error($"command.{key}".ToLang(args));

    public static TextCommandResult CommandSuccess(this string key, params object[]? args) => TextCommandResult.Success($"command.{key}".ToLang(args));

    public static Point GetPoint(this IPlayer player) {
        EntityPos pos = player.Entity.Pos;
        return new Point(pos.X, pos.Z);
    }

    public static Point ToPoint(this BlockPos pos) => new(pos.X, pos.Z);

    public static Point ToPoint(this EntityPos pos) => new(Math.Round(pos.X, 1), Math.Round(pos.Z, 1));

    public static Point ToPoint(this Vec3i pos) => new(pos.X, pos.Z);

    public static Vec3i ToVec3i(this EntityPos pos) => new((int)pos.X, (int)pos.Y, (int)pos.Z);

    public static Point Size(this IWorldManagerAPI api) => new(api.MapSizeX, api.MapSizeZ);

    public static void UnregisterCommand(this IChatCommandApi api, string name) => ((ChatCommandApi)api).GetType().GetMethod("UnregisterCommand", _flags)?.Invoke(api, [name]);

    public static void AutoSaveNow(this ICoreServerAPI api) {
        api.Event.RegisterCallback(_ => {
            IChatCommand command = api.ChatCommands.Get("autosavenow");
            command.Execute(new TextCommandCallingArgs {
                LanguageCode = Lang.CurrentLocale,
                Command = command,
                SubCmdCode = "autosavenow",
                Caller = new Caller { Type = EnumCallerType.Console, CallerPrivileges = ["*"], CallerRole = "admin", FromChatGroupId = 0 },
                RawArgs = new CmdArgs("")
            });
        }, 1);
    }

    public static T DeepCopy<T>(this T self) where T : BaseOptions => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(self))!;

    public static Dictionary<string, object> GetHealth(this IPlayer player) {
        EntityBehaviorHealth? health = player.Entity.GetBehavior<EntityBehaviorHealth>();
        return new Dictionary<string, object> { { "cur", health?.Health ?? 15 }, { "max", health?.MaxHealth ?? 15 } };
    }

    public static Dictionary<string, object> GetSatiety(this IPlayer player) {
        EntityBehaviorHunger? satiety = player.Entity.GetBehavior<EntityBehaviorHunger>();
        return new Dictionary<string, object> { { "cur", satiety?.Saturation ?? 1500 }, { "max", satiety?.MaxSaturation ?? 1500 } };
    }

    public static string GetAvatar(this EntityPlayer player) {
        ITreeAttribute appliedParts = (ITreeAttribute)player.WatchedAttributes.GetTreeAttribute("skinConfig")["appliedParts"];
        return $"https://vs.pl3x.net/v1/{appliedParts.GetString("baseskin")}/{appliedParts.GetString("eyecolor")}/{appliedParts.GetString("hairbase")}/{appliedParts.GetString("hairextra")}/{appliedParts.GetString("mustache")}/{appliedParts.GetString("beard")}/{appliedParts.GetString("haircolor")}.png";
    }
}
