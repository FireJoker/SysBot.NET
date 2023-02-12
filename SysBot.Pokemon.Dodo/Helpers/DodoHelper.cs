using System;
using System.Collections.Generic;
using System.Linq;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Dodo
{
    public class DodoHelper<T> where T : PKM, new()
    {
        public static void StartTrade(string ps, string dodoId, string nickName, string channelId, string islandSourceId)
        {
            var _ = CheckAndGetPkm(ps, dodoId, out var msg, out var pkm);
            if (!_)
            {
                DodoBot<T>.SendChannelMessage(msg, channelId);
                return;
            }

            var isForeign = ps.Contains("Language: Italian");
            var isAutoOT = ps.Contains("OT: AutoOT");
            StartTradeWithoutCheck(pkm, dodoId, nickName, channelId, islandSourceId, isAutoOT);
        }

        public static void StartTrade(T pkm, string dodoId, string nickName, string channelId, string islandSourceId)
        {
            var _ = CheckPkm(pkm, dodoId, out var msg);
            if (!_)
            {
                DodoBot<T>.SendChannelMessage(msg, channelId);
                return;
            }

            StartTradeWithoutCheck(pkm, dodoId, nickName, channelId, islandSourceId);
        }

        public static void StartMultiTrade(string chinesePsRaw, string dodoId, string nickName, string channelId, string islandSourceId)
        {
            var PSList = chinesePsRaw.Split('+').ToList();
            var MaxPkmsPerTrade = DodoBot<T>.Info.Hub.Config.Trade.MaxPkmsPerTrade;
            if (MaxPkmsPerTrade <= 1)
            {
                DodoBot<T>.SendChannelMessage("请联系群主将trade/MaxPkmsPerTrade配置改为大于1", channelId);
                return;
            }
            else if (PSList.Count > MaxPkmsPerTrade)
            {
                DodoBot<T>.SendChannelMessage($"批量交换宝可梦数量应小于等于{MaxPkmsPerTrade}", channelId);
                return;
            }

            List<string> msgs = new();
            List<T> tradeList = new();
            List<bool> isAutoOTList = new();

            int invalidCount = 0;
            for (var i = 0; i < PSList.Count; i++)
            {
                var ps = ShowdownTranslator<T>.Chinese2Showdown(PSList[i]);
                var _ = CheckAndGetPkm(ps, dodoId, out var msg, out var pkm);
                if (!_)
                {
                    LogUtil.LogInfo($"批量第{i + 1}只宝可梦有问题:{msg}", nameof(DodoHelper<T>));
                    invalidCount++;
                }
                else
                {
                    LogUtil.LogInfo($"批量第{i + 1}只:\n{ps}", nameof(DodoHelper<T>));
                    isAutoOTList.Add(ps.Contains("OT: AutoOT"));
                    tradeList.Add(pkm);
                }
            }

            if (invalidCount == PSList.Count)
            {
                DodoBot<T>.SendChannelMessage("期望交换的宝可梦全都不合法", channelId);
                return;
            }
            else if (invalidCount != 0)
            {
                DodoBot<T>.SendChannelMessage($"期望交换的{PSList.Count}只宝可梦中，有{invalidCount}只不合法，仅交换合法的{tradeList.Count}只", channelId);
            }

            var code = DodoBot<T>.Info.GetRandomTradeCode();
            var __ = AddToTradeQueue(tradeList, code, ulong.Parse(dodoId), nickName, channelId, islandSourceId, isAutoOTList,
                PokeRoutineType.LinkTrade, out string message);
            DodoBot<T>.SendChannelMessage(message, channelId);
        }

        public static void StartTeamTrade(string TeamPokePaste, string dodoId, string nickName, string channelId, string islandSourceId)
        {
            var TeamList = TeamPokePaste.Split("\n\n").ToList();

            List<T> tradeList = new();
            List<bool> isAutoOTList = new();
            List<string> msgs = new();

            int invalidCount = 0;
            for (var i = 0; i < TeamList.Count; i++)
            {
                var ps = TeamList[i];
                var _ = CheckAndGetPkm(ps, dodoId, out var msg, out var pkm);
                if (!_)
                {
                    LogUtil.LogInfo($"批量第{i + 1}只宝可梦有问题:{msg}", nameof(DodoHelper<T>));
                    invalidCount++;
                }
                else
                {
                    LogUtil.LogInfo($"批量第{i + 1}只:\n{ps}", nameof(DodoHelper<T>));
                    isAutoOTList.Add(true);
                    tradeList.Add(pkm);
                }
            }
            if (invalidCount == TeamList.Count)
            {
                DodoBot<T>.SendChannelMessage("一个都不合法，换个屁", channelId);
                return;
            }
            else if (invalidCount != 0)
            {
                DodoBot<T>.SendChannelMessage($"期望交换的{TeamList.Count}只宝可梦中，有{invalidCount}只不合法，仅交换合法的{tradeList.Count}只", channelId);
            }

            var code = DodoBot<T>.Info.GetRandomTradeCode();
            var __ = AddToTradeQueue(tradeList, code, ulong.Parse(dodoId), nickName, channelId, islandSourceId, isAutoOTList,
                PokeRoutineType.LinkTrade, out string message);
            DodoBot<T>.SendChannelMessage(message, channelId);
        }

        public static void StartTradeWithoutCheck(T pkm, string dodoId, string nickName, string channelId, string islandSourceId, bool isAutoOT = false)
        {
            var code = DodoBot<T>.Info.GetRandomTradeCode();
            var __ = AddToTradeQueue(pkm, code, ulong.Parse(dodoId), nickName, channelId, islandSourceId, isAutoOT,
                PokeRoutineType.LinkTrade, out string message);
            DodoBot<T>.SendChannelMessage(message, channelId);
            DodoBot<T>.SendPersonalMessage(dodoId, islandSourceId, $"连接密码：{code:0000 0000}");
        }

        public static void StartClone(string dodoId, string nickName, string channelId, string islandSourceId)
        {
            var code = DodoBot<T>.Info.GetRandomTradeCode();
            var __ = AddToTradeQueue(new T(), code, ulong.Parse(dodoId), nickName, channelId, islandSourceId, false,
                PokeRoutineType.Clone, out string message);
            DodoBot<T>.SendChannelMessage(message, channelId); 
        }

        public static void StartDump(string dodoId, string nickName, string channelId, string islandSourceId)
        {
            var code = DodoBot<T>.Info.GetRandomTradeCode();
            var __ = AddToTradeQueue(new T(), code, ulong.Parse(dodoId), nickName, channelId, islandSourceId, false,
                PokeRoutineType.Dump, out string message);
            DodoBot<T>.SendChannelMessage(message, channelId);
        }

        public static bool CheckPkm(T pkm, string username, out string msg)
        {
            if (!DodoBot<T>.Info.GetCanQueue())
            {
                msg = "对不起, 我不再接受队列请求!";
                return false;
            }
            try
            {
                if (!pkm.CanBeTraded())
                {
                    msg = $"取消派送, <@!{username}>: 官方禁止该宝可梦交易!";
                    return false;
                }

                if (pkm is T pk)
                {
                    var valid = new LegalityAnalysis(pkm).Valid;
                    if (valid)
                    {
                        msg =
                            $"<@!{username}> - 已加入等待队列. 如果你选宝可梦的速度太慢，你的派送请求将被取消!";
                        return true;
                    }
                }

                var reason = "我没办法创造非法宝可梦";
                msg = $"<@!{username}>: {reason}";
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(DodoBot<T>));
                msg = $"取消派送, <@!{username}>: 发生了一个错误";
            }

            return false;
        }

        public static bool CheckAndGetPkm(string setstring, string username, out string msg, out T outPkm)
        {
            outPkm = new T();
            if (!DodoBot<T>.Info.GetCanQueue())
            {
                msg = "对不起, 我不再接受队列请求!";
                return false;
            }

            var set = ShowdownUtil.ConvertToShowdown(setstring);
            if (set == null)
            {
                msg = $"<@!{username}>: 宝可梦昵称为空.";
                return false;
            }

            var template = AutoLegalityWrapper.GetTemplate(set);
            if (template.Species < 1)
            {
                msg =
                    $"<@!{username}>: 请使用正确的Showdown Set代码";
                return false;
            }

            if (set.InvalidLines.Count != 0)
            {
                msg =
                    $"<@!{username}>: 非法的Showdown Set代码:\n{string.Join("\n", set.InvalidLines)}";
                return false;
            }

            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);

                var nickname = pkm.Nickname.ToLower();
                if (nickname == "egg" && Breeding.CanHatchAsEgg(pkm.Species))
                    TradeExtensions<T>.EggTrade(pkm, template);

                if (!pkm.CanBeTraded())
                {
                    msg = $"<@!{username}>: 官方禁止该宝可梦交易!";
                    return false;
                }

                if (pkm is T pk)
                {
                    var valid = new LegalityAnalysis(pkm).Valid;
                    if (valid)
                    {
                        outPkm = pk;

                        msg =
                            $"<@!{username}> - 已加入等待队列. 如果你选宝可梦的速度太慢，你的派送请求将被取消!";
                        return true;
                    }
                }

                var reason = result == "Timeout"
                    ? "宝可梦创造超时"
                    : "我没办法创造非法宝可梦";
                msg = $"<@!{username}>: {reason}";
            }
            catch (Exception ex)
            {
                LogUtil.LogSafe(ex, nameof(DodoBot<T>));
                msg = $"<@!{username}>: 发生了一个错误,取消交换";
            }

            return false;
        }

        private static bool AddToTradeQueue(T pk, int code, ulong userId, string name, string channelId, string islandSourceId, bool isAutoOT,
            PokeRoutineType type, out string msg)
        {
            return AddToTradeQueue(new List<T> { pk }, code, userId, name, channelId, islandSourceId, new List<bool> { isAutoOT }, type, out msg);
        }

        private static bool AddToTradeQueue(List<T> pks, int code, ulong userId, string name, string channelId, string islandSourceId, List<bool> isAutoOTList,
            PokeRoutineType type, out string msg)
        {
            if (pks == null || pks.Count == 0)
            {
                msg = $"宝可梦数据为空";
                return false;
            }
            T pk = pks.First();
            
            var trainer = new PokeTradeTrainerInfo(name, userId);
            var notifier = new DodoTradeNotifier<T>(pk, trainer, code, name, userId.ToString(), channelId, islandSourceId);
            var tt = type == PokeRoutineType.SeedCheck ? PokeTradeType.Seed : 
                (type == PokeRoutineType.Clone ? PokeTradeType.Clone :
                (type == PokeRoutineType.Dump ? PokeTradeType.Dump :
                (type == PokeRoutineType.EtumrepDump ? PokeTradeType.EtumrepDump :
                PokeTradeType.Specific)));

            var detail =
                new PokeTradeDetail<T>(pk, trainer, notifier, tt, code, true);
            
            detail.Context.Add("自ID", isAutoOTList);
            if (pks.Count > 0)
            {
                detail.Context.Add("批量", pks);
            }

            var trade = new TradeEntry<T>(detail, userId, type, name);

            var added = DodoBot<T>.Info.AddToTradeQueue(trade, userId, false);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = $"<@!{userId}> \n你已经在队列中\n@或私信我并发送位置可查询当前位置";
                return false;
            }

            var position = DodoBot<T>.Info.CheckPosition(userId, type);
            msg = $"<@!{userId}>\n你在第{position.Position}位";

            var botct = DodoBot<T>.Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = DodoBot<T>.Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                //连接密码：{detail.Code:0000 0000}";

                msg += $"\n需等待约{eta:F1}分钟\n{(pk.IsShiny ? "异色" : string.Empty)}{ShowdownTranslator<T>.GameStringsZh.Species[trade.Trade.TradeData.Species]}{(pk.IsEgg ? "(蛋)" : string.Empty)}准备中\n@或私信我并发送位置可查询当前位置\n@或私信我并发送取消可取消排队";
            }
            else
            {
                msg += $"\n{(pk.IsShiny ? "异色" : string.Empty)}{ShowdownTranslator<T>.GameStringsZh.Species[trade.Trade.TradeData.Species]}{(pk.IsEgg ? "(蛋)" : string.Empty)}准备完成";

            }
            return true;
        }
    }
}