using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace SysBot.Pokemon.Dodo
{
    public class DodoTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        public string IdentifierLocator => "Dodo";
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private string Username { get; }
        private string UserId { get; }
        private string ChannelId { get; }
        private string IslandSourceId { get; }

        public Action<PokeRoutineExecutor<T>>? OnFinish { private get; set; }
        public int QueueSizeEntry { get; set; }
        public bool ReminderSent { get; set; } = false;
        public DodoReminderHelper<T>? ReminderHelper { get; set; } = null;

        public DodoTradeNotifier(T data, PokeTradeTrainerInfo info, int code, string username, string userid, string channelId, string islandSourceId)
        {
            Data = data;
            Info = info;
            Code = code;
            Username = username;
            UserId = userid;
            ChannelId = channelId;
            IslandSourceId = islandSourceId;
            LogUtil.LogText($"Created trade details for {Username} - {Code}");
        }

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            var msg =
                $"@{info.Trainer.TrainerName} (ID: {info.ID}): Initializing trade{receive} with you. Please be ready.";
            msg += $" Your trade code is: {info.Code:0000 0000}";
            LogUtil.LogText(msg);
            var text = $"\n{(Data.IsShiny ? "异色" : string.Empty)}{ShowdownTranslator<T>.GameStringsZh.Species[Data.Species]}{(Data.IsEgg ? "(蛋)" : string.Empty)}准备完成\n请提前准备好\n密码见私信";

            List<T> tradeList = (List<T>)info.Context.GetValueOrDefault("MultiTrade", new List<T>());
            if (tradeList.Count > 1)
            {
                text = $"\n批量派送{tradeList.Count}只宝可梦";
            }

            DodoBot<T>.SendChannelAtMessage(info.Trainer.ID, text, ChannelId);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), IslandSourceId,
                $"准备交换:{ShowdownTranslator<T>.GameStringsZh.Species[Data.Species]}\n连接密码:{info.Code:0000 0000}\n我的名字:{routine.InGameName}");
        }

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", @{name}";
            var message = $"I'm waiting for you{trainer}! My IGN is {routine.InGameName}.";
            message += $" Your trade code is: {info.Code:0000 0000}";
            LogUtil.LogText(message);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), IslandSourceId, $"我在等你了\n连接密码:{info.Code:0000 0000}\n我的名字:{routine.InGameName}");
        }

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            var line = $"@{info.Trainer.TrainerName}: Trade canceled, {msg}";
            LogUtil.LogText(line);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), IslandSourceId, "交换取消");
        }

        public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            OnFinish?.Invoke(routine);
            var tradedToUser = Data.Species;
            var message = $"@{info.Trainer.TrainerName}: " + (tradedToUser != 0
                ? $"Trade finished. Enjoy your {(Species)tradedToUser}!"
                : "Trade finished!");
            LogUtil.LogText(message);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), IslandSourceId, "交换完成");
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string message)
        {
            LogUtil.LogText(message);
            if (message.Contains("Found Link Trade partner:"))
            {
                var splitTotal = message.Split(' ');
                var OT = splitTotal[4];
                var TID = splitTotal[6];
                var SID = splitTotal[8].Split('.')[0];
                DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), IslandSourceId, $"找到初训家：{OT}\nTID(表ID)：{TID}\nSID(里ID)：{SID}\n等待交换宝可梦");
            }
            else if (message.StartsWith("批量"))
            {
                DodoBot<T>.SendChannelMessage(message, ChannelId);
            }
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            LogUtil.LogText(msg);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), IslandSourceId, msg);
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result, string message)
        {
            var msg = $"Details for {result.FileName}: " + message;
            LogUtil.LogText(msg);
            if (result.Species != 0 && info.Type == PokeTradeType.Dump)
            {
                var text =
                    $"训练家:{result.OT_Name}" +
                    $"\n训练家性别:{result.OT_Gender}" +
                    $"\n训练家语言:{result.Language}" +
                    $"\n6位表ID:{result.SID16}" +
                    $"\n4位里ID:{result.SID16}";
                DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), IslandSourceId, text);
            }
        }

        public void SendReminder(int position, string message)
        {
            if (ReminderSent)
                return;
            ReminderSent = true;
            ReminderHelper?.Remind(UserId, IslandSourceId);
        }

        public void SendEtumrepEmbed(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, IReadOnlyList<PA8> pkms) { }
        public void SendIncompleteEtumrepEmbed(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, string msg, IReadOnlyList<PA8> pkms) { }
    }
}