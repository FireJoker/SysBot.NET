using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Pokemon.Dodo
{
    public class DodoReminderHelper<T> where T : PKM, new()
    {
        private readonly string UserId;
        private readonly PokeTradeHubConfig Config;

        private List<string> NotifyPings = new List<string>();
        private object _sync = new object();

        public DodoReminderHelper(string userid, PokeTradeHubConfig config) 
        { 
            UserId = userid;
            Config = config;
        }

        public void Remind(string userid)
        {
            lock (_sync)
            {
                NotifyPings.Add($"{userid}");
                CheckReminderSend(userid);
            }
        }

        private void CheckReminderSend(string userid)
        {
            try
            {
                if (NotifyPings.Count >= Config.Queues.ReminderQueueCountStart)
                {
                    string msg = $" 注意，你当前在{Config.Queues.ReminderAtPosition}位。\n请提前做好准备！确保游戏已经联网！";
                    DodoBot<T>.SendPersonalMessage(userid, msg);
                    NotifyPings.Clear();
                }
            } 
            catch (Exception e) 
            {
                LogUtil.LogError(e.Message + "\n" + e.StackTrace, nameof(DodoReminderHelper<T>));
            }
        }
    }
}
