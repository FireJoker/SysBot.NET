using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PKHeX.Core;
using System.Linq;
using System.Threading.Tasks;
using SysBot.Base;
using System.Collections.Generic;

namespace SysBot.Pokemon.Discord
{
    [Summary("Queues new Link Code trades")]
    public class TradeModule<T> : ModuleBase<SocketCommandContext> where T : PKM, new()
    {
        private static TradeQueueInfo<T> Info => SysCord<T>.Runner.Hub.Queues.Info;

        [Command("tradeList")]
        [Alias("tl")]
        [Summary("Prints the users in the trade queues.")]
        [RequireSudo]
        public async Task GetTradeListAsync()
        {
            string msg = Info.GetTradeList(PokeRoutineType.LinkTrade);
            var embed = new EmbedBuilder();
            embed.AddField(x =>
            {
                x.Name = "Pending Trades";
                x.Value = msg;
                x.IsInline = false;
            });
            await ReplyAsync("These are the users who are currently waiting:", embed: embed.Build()).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the provided Pokémon file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttach([Summary("Trade Code")] int code)
        {
            var sig = Context.User.GetFavor();
            await TradeAsyncAttach(code, sig, Context.User).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
        {
            content = ReusableActions.StripCodeBlock(content);
            var set = new ShowdownSet(content);
            var template = AutoLegalityWrapper.GetTemplate(set);
            if (set.InvalidLines.Count != 0)
            {
                var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                await ReplyAsync(msg).ConfigureAwait(false);
                return;
            }

            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                bool pla = typeof(T) == typeof(PA8);

                if (!pla && pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                    TradeExtensions<T>.EggTrade(pkm, template);

                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
                bool memes = Info.Hub.Config.Trade.Memes && await TradeAdditionsModule<T>.TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false);
                if (memes)
                    return;

                if (pkm is not T pk || !la.Valid)
                {
                    var reason = result == "Timeout" ? $"That {spec} set took too long to generate." : $"I wasn't able to create a {spec} from that set.";
                    var imsg = $"Oops! {reason}";
                    if (result == "Failed")
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                    await ReplyAsync(imsg).ConfigureAwait(false);
                    return;
                }
                pk.ResetPartyStats();

                var isAutoOT = content.Contains("OT: AutoOT");
                var sig = Context.User.GetFavor();
                await AddTradeToQueueAsync(code, Context.User.Username, pk, isAutoOT, sig, Context.User).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(TradeModule<T>));
                var msg = $"Oops! An unexpected problem happened with this Showdown Set:\n```{string.Join("\n", set.GetSetLines())}```";
                await ReplyAsync(msg).ConfigureAwait(false);
            }
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsync([Summary("Showdown Set")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsync(code, content).ConfigureAwait(false);
        }

        [Command("trade")]
        [Alias("t")]
        [Summary("Makes the bot trade you the attached file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesTrade))]
        public async Task TradeAsyncAttach()
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncAttach(code).ConfigureAwait(false);
        }

        [Command("teamtrade")]
        [Alias("tt", "team", "mt")]
        [Summary("Makes the bot trade you the provided multiple Pokémon file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesMultiTrade))]
        public async Task TeamTradeAsyncAttach([Summary("Trade Code")] int code)
        {
            var sig = Context.User.GetFavor();
            await TeamTradeAsyncAttach(code, sig, Context.User).ConfigureAwait(false);
        }

        [Command("teamtrade")]
        [Alias("tt", "team")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided multiple Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesMultiTrade))]
        public async Task TeamTradeAsync([Summary("Trade Code")] int code, [Summary("Showdown Set")][Remainder] string content)
        {
            var TeamList = content.Split("\n\n").ToList();

            List<T> tradeList = new();
            List<bool> isAutoOTList = new();

            foreach(var team in TeamList)
            {
                var teamset = ReusableActions.StripCodeBlock(team);
                var set = new ShowdownSet(teamset);
                var template = AutoLegalityWrapper.GetTemplate(set);
                if (set.InvalidLines.Count != 0)
                {
                    var msg = $"Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                    await ReplyAsync(msg).ConfigureAwait(false);
                    break;
                }

                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);
                bool pla = typeof(T) == typeof(PA8);

                if (!pla && pkm.Nickname.ToLower() == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                    TradeExtensions<T>.EggTrade(pkm, template);

                var la = new LegalityAnalysis(pkm);
                var spec = GameInfo.Strings.Species[template.Species];
                pkm = EntityConverter.ConvertToType(pkm, typeof(T), out _) ?? pkm;
                bool memes = Info.Hub.Config.Trade.Memes && await TradeAdditionsModule<T>.TrollAsync(Context, pkm is not T || !la.Valid, pkm).ConfigureAwait(false);
                if (memes)
                    break;

                if (pkm is not T pk || !la.Valid)
                {
                    var reason = result == "Timeout" ? $"That {spec} set took too long to generate." : $"I wasn't able to create a {spec} from that set.";
                    var imsg = $"Oops! {reason}";
                    if (result == "Failed")
                        imsg += $"\n{AutoLegalityWrapper.GetLegalizationHint(template, sav, pkm)}";
                    await ReplyAsync(imsg).ConfigureAwait(false);
                    break;
                }

                pk.ResetPartyStats();
                isAutoOTList.Add(teamset.Contains("OT: AutoOT"));
                tradeList.Add(pk);
            }

            if (tradeList.Count == 0)
            {
                await ReplyAsync("None of the provided Showdown Set is valid").ConfigureAwait(false);
                return;
            }
            var sig = Context.User.GetFavor();
            await AddTradeToQueueAsync(code, Context.User.Username, tradeList, isAutoOTList, sig, Context.User).ConfigureAwait(false);
        }

        [Command("teamtrade")]
        [Alias("tt", "team")]
        [Summary("Makes the bot trade you a Pokémon converted from the provided multiple Showdown Set.")]
        [RequireQueueRole(nameof(DiscordManager.RolesMultiTrade))]
        public async Task TeamTradeAsync([Summary("Showdown Set")][Remainder] string content)
        {
            var code = Info.GetRandomTradeCode();
            await TeamTradeAsync(code, content).ConfigureAwait(false);
        }

        [Command("teamtrade")]
        [Alias("tt", "team")]
        [Summary("Makes the bot trade you the multiple attached file.")]
        [RequireQueueRole(nameof(DiscordManager.RolesMultiTrade))]
        public async Task TeamTradeAsyncAttach()
        {
            var code = Info.GetRandomTradeCode();
            await TeamTradeAsyncAttach(code).ConfigureAwait(false);
        }

        [Command("banTrade")]
        [Alias("bt")]
        [RequireSudo]
        public async Task BanTradeAsync([Summary("Online ID")] ulong nnid, string comment)
        {
            SysCordSettings.HubConfig.TradeAbuse.BannedIDs.AddIfNew(new[] { GetReference(nnid, comment) });
            await ReplyAsync("Done.").ConfigureAwait(false);
        }

        private RemoteControlAccess GetReference(ulong id, string comment) => new()
        {
            ID = id,
            Name = id.ToString(),
            Comment = $"Added by {Context.User.Username} on {DateTime.Now:yyyy.MM.dd-hh:mm:ss} ({comment})",
        };

        [Command("tradeUser")]
        [Alias("tu", "tradeOther")]
        [Summary("Makes the bot trade the mentioned user the attached file.")]
        [RequireSudo]
        public async Task TradeAsyncAttachUser([Summary("Trade Code")] int code, [Remainder]string _)
        {
            if (Context.Message.MentionedUsers.Count > 1)
            {
                await ReplyAsync("Too many mentions. Queue one user at a time.").ConfigureAwait(false);
                return;
            }

            if (Context.Message.MentionedUsers.Count == 0)
            {
                await ReplyAsync("A user must be mentioned in order to do this.").ConfigureAwait(false);
                return;
            }

            var usr = Context.Message.MentionedUsers.ElementAt(0);
            var sig = usr.GetFavor();
            await TradeAsyncAttach(code, sig, usr).ConfigureAwait(false);
        }

        [Command("tradeUser")]
        [Alias("tu", "tradeOther")]
        [Summary("Makes the bot trade the mentioned user the attached file.")]
        [RequireSudo]
        public async Task TradeAsyncAttachUser([Remainder] string _)
        {
            var code = Info.GetRandomTradeCode();
            await TradeAsyncAttachUser(code, _).ConfigureAwait(false);
        }

        private async Task TradeAsyncAttach(int code, RequestSignificance sig, SocketUser usr)
        {
            var attachment = Context.Message.Attachments.FirstOrDefault();
            if (attachment == default)
            {
                await ReplyAsync("No attachment provided!").ConfigureAwait(false);
                return;
            }

            var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
            var pk = GetRequest(att);
            if (pk == null)
            {
                await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
                return;
            }

            await AddTradeToQueueAsync(code, usr.Username, pk, false, sig, usr).ConfigureAwait(false);
        }

        private async Task TeamTradeAsyncAttach(int code, RequestSignificance sig, SocketUser usr)
        {
            var attachments = Context.Message.Attachments;
            if (attachments == default)
            {
                await ReplyAsync("No attachment provided!").ConfigureAwait(false);
                return;
            }

            List<T> attList = new();
            List<bool> isAutoOTList = new();
            foreach (var attachment in attachments)
            {
                var att = await NetUtil.DownloadPKMAsync(attachment).ConfigureAwait(false);
                var pk = GetRequest(att);
                if (pk == null)
                    await ReplyAsync("Attachment provided is not compatible with this module!").ConfigureAwait(false);
                else
                {
                    attList.Add(pk);
                    isAutoOTList.Add(false);
                }
            }

            await AddTradeToQueueAsync(code, usr.Username, attList, isAutoOTList, sig, usr).ConfigureAwait(false);
        }

        private static T? GetRequest(Download<PKM> dl)
        {
            if (!dl.Success)
                return null;
            return dl.Data switch
            {
                null => null,
                T pk => pk,
                _ => EntityConverter.ConvertToType(dl.Data, typeof(T), out _) as T,
            };
        }

        private async Task AddTradeToQueueAsync(int code, string trainerName, T pk, bool isAutoOT, RequestSignificance sig, SocketUser usr)
        {
            if (!pk.CanBeTraded())
            {
                await ReplyAsync("Provided Pokémon content is blocked from trading!").ConfigureAwait(false);
                return;
            }

            var la = new LegalityAnalysis(pk);
            if (!la.Valid)
            {
                await ReplyAsync($"{typeof(T).Name} attachment is not legal, and cannot be traded!").ConfigureAwait(false);
                return;
            }

            await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, pk, isAutoOT, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr).ConfigureAwait(false);
        }

        private async Task AddTradeToQueueAsync(int code, string trainerName, List<T> pks, List<bool> OTs, RequestSignificance sig, SocketUser usr)
        {
            List<T> tradeList = new();
            List<bool> isAutoOTList = new();

            for (var i = 0; i < pks.Count; i++)
            {
                var pk = pks[i];
                var isOT = OTs[i];
                var la = new LegalityAnalysis(pk);
                if (!pk.CanBeTraded())
                {
                    await ReplyAsync($"Provided {i + 1} Pokémon content is blocked from trading!").ConfigureAwait(false);
                }
                else if (!la.Valid)
                {
                    await ReplyAsync($"{typeof(T).Name} attachment is not legal, and cannot be traded!").ConfigureAwait(false);
                }
                else
                {
                    tradeList.Add(pk);
                    isAutoOTList.Add(isOT);
                }
            }

            if (tradeList.Count == 0)
            {
                await ReplyAsync($" None of Provided Pokémon will be trade!").ConfigureAwait(false);
            }
            else if (tradeList.Count == 1)
            {
                var trade = tradeList.FirstOrDefault();
                var isAutoOT = isAutoOTList.FirstOrDefault();
                await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, trade, isAutoOT, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr).ConfigureAwait(false);
            }
            else if(tradeList.Count > 1)
                await QueueHelper<T>.AddToQueueAsync(Context, code, trainerName, sig, tradeList, isAutoOTList, PokeRoutineType.LinkTrade, PokeTradeType.Specific, usr).ConfigureAwait(false);
        }
    }
}
