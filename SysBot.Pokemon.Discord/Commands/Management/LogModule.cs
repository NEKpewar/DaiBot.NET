using Discord;
using Discord.Commands;
using Discord.WebSocket;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.Discord;

public class LogModule : ModuleBase<SocketCommandContext>
{
    private static readonly Dictionary<ulong, ChannelLogger> Channels = [];

    public static void RestoreLogging(DiscordSocketClient discord, DiscordSettings settings)
    {
        foreach (var ch in settings.LoggingChannels)
        {
            if (discord.GetChannel(ch.ID) is ISocketMessageChannel c)
                AddLogChannel(c, ch.ID);
        }

        LogUtil.LogInfo("Se agregó registro a los canales de Discord al iniciar el Bot.", "Discord");
    }

    [Command("logHere")]
    [Summary("Makes the bot log to the channel.")]
    [RequireSudo]
    public async Task AddLogAsync()
    {
        var c = Context.Channel;
        var cid = c.Id;
        if (Channels.TryGetValue(cid, out _))
        {
            await ReplyAsync("⚠️ Ya me estoy registrando aquí.").ConfigureAwait(false);
            return;
        }

        AddLogChannel(c, cid);

        // Add to discord global loggers (saves on program close)
        SysCordSettings.Settings.LoggingChannels.AddIfNew(new[] { GetReference(Context.Channel) });
        await ReplyAsync("✔ ¡Añadida salida de registro a este canal!").ConfigureAwait(false);
    }

    private static void AddLogChannel(ISocketMessageChannel c, ulong cid)
    {
        var logger = new ChannelLogger(cid, c);
        LogUtil.Forwarders.Add(logger);
        Channels.Add(cid, logger);
    }

    [Command("logInfo")]
    [Summary("Dumps the logging settings.")]
    [RequireSudo]
    public async Task DumpLogInfoAsync()
    {
        foreach (var c in Channels)
            await ReplyAsync($"{c.Key} - {c.Value}").ConfigureAwait(false);
    }

    [Command("logClear")]
    [Summary("Clears the logging settings in that specific channel.")]
    [RequireSudo]
    public async Task ClearLogsAsync()
    {
        var id = Context.Channel.Id;
        if (!Channels.TryGetValue(id, out var log))
        {
            await ReplyAsync("⚠️ No hay eco en este canal.").ConfigureAwait(false);
            return;
        }
        LogUtil.Forwarders.Remove(log);
        Channels.Remove(Context.Channel.Id);
        SysCordSettings.Settings.LoggingChannels.RemoveAll(z => z.ID == id);
        await ReplyAsync($"✔ Registro borrado del canal: {Context.Channel.Name}").ConfigureAwait(false);
    }

    [Command("logClearAll")]
    [Summary("Clears all the logging settings.")]
    [RequireSudo]
    public async Task ClearLogsAllAsync()
    {
        foreach (var l in Channels)
        {
            var entry = l.Value;
            await ReplyAsync($"✔ Registro borrado de: {entry.ChannelName} ({entry.ChannelID}!").ConfigureAwait(false);
            LogUtil.Forwarders.Remove(entry);
        }

        LogUtil.Forwarders.RemoveAll(y => Channels.Select(z => z.Value).Contains(y));
        Channels.Clear();
        SysCordSettings.Settings.LoggingChannels.Clear();
        await ReplyAsync("✔ ¡Registro borrado de todos los canales!").ConfigureAwait(false);
    }

    private RemoteControlAccess GetReference(IChannel channel) => new()
    {
        ID = channel.Id,
        Name = channel.Name,
        Comment = $"Añadido por {Context.User.Username} el {DateTime.Now:yyyy.MM.dd-hh:mm:ss}",
    };
}
