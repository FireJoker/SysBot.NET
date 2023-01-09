using PKHeX.Core;
using SysBot.Base;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace SysBot.Pokemon.Dodo
{
    public class DodoTradeNotifier<T> : IPokeTradeNotifier<T> where T : PKM, new()
    {
        private T Data { get; }
        private PokeTradeTrainerInfo Info { get; }
        private int Code { get; }
        private string Username { get; }

        private string ChannelId { get; }

        public DodoTradeNotifier(T data, PokeTradeTrainerInfo info, int code, string username, string channelId)
        {
            Data = data;
            Info = info;
            Code = code;
            Username = username;
            ChannelId = channelId;
            LogUtil.LogText($"Created trade details for {Username} - {Code}");
        }

        public Action<PokeRoutineExecutor<T>> OnFinish { private get; set; }

        public void TradeInitialize(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var receive = Data.Species == 0 ? string.Empty : $" ({Data.Nickname})";
            var msg =
                $"@{info.Trainer.TrainerName} (ID: {info.ID}): Initializing trade{receive} with you. Please be ready.";
            msg += $" Your trade code is: {info.Code:0000 0000}";
            LogUtil.LogText(msg);
            var text = $"\n{(Data.IsShiny ? "异色" : string.Empty)}{ShowdownTranslator<T>.GameStringsZh.Species[Data.Species]}{(Data.IsEgg ? "(蛋)" : string.Empty)}准备完成\n请提前准备好\n密码见私信";
            DodoBot<T>.SendChannelAtMessage(info.Trainer.ID, text, ChannelId);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(),
                $"准备交换:{ShowdownTranslator<T>.GameStringsZh.Species[Data.Species]}\n连接密码:{info.Code:0000 0000}\n我的名字:{routine.InGameName}");
        }

        public void TradeSearching(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info)
        {
            var name = Info.TrainerName;
            var trainer = string.IsNullOrEmpty(name) ? string.Empty : $", @{name}";
            var message = $"I'm waiting for you{trainer}! My IGN is {routine.InGameName}.";
            message += $" Your trade code is: {info.Code:0000 0000}";
            LogUtil.LogText(message);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), 
                $"我在等你了\n连接密码:{info.Code:0000 0000}\n我的名字:{routine.InGameName}");
        }

        public void TradeCanceled(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeResult msg)
        {
            OnFinish?.Invoke(routine);
            var line = $"@{info.Trainer.TrainerName}: Trade canceled, {msg}";
            LogUtil.LogText(line);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), "交换取消");
        }

        public void TradeFinished(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, T result)
        {
            OnFinish?.Invoke(routine);
            var tradedToUser = Data.Species;
            var message = $"@{info.Trainer.TrainerName}: " + (tradedToUser != 0
                ? $"Trade finished. Enjoy your {(Species)tradedToUser}!"
                : "Trade finished!");
            LogUtil.LogText(message);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), "交换完成");
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
                DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(),
                    $"找到初训家：{OT}\nTID(表ID)：{TID}\nSID(里ID)：{SID}\n等待交换宝可梦");
            }
        }

        public void SendNotification(PokeRoutineExecutor<T> routine, PokeTradeDetail<T> info, PokeTradeSummary message)
        {
            var msg = message.Summary;
            if (message.Details.Count > 0)
                msg += ", " + string.Join(", ", message.Details.Select(z => $"{z.Heading}: {z.Detail}"));
            LogUtil.LogText(msg);
            DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(),msg);
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
                    $"\n6位表ID:{result.TrainerID7}" +
                    $"\n4位里ID:{result.TrainerSID7}";
                DodoBot<T>.SendPersonalMessage(info.Trainer.ID.ToString(), text);
            }
        }
    }
}