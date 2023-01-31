using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace SysBot.Pokemon.Dodo
{
    public class DodoReminderHelper<T> where T : PKM, new()
    {
        private readonly string UserId;
        private readonly string IslandId;
        private readonly PokeTradeHubConfig Config;

        private readonly List<string> NotifyPings = new List<string>();
        private readonly object _sync = new object();

        public DodoReminderHelper(string userid, string islandid, PokeTradeHubConfig config) 
        { 
            UserId = userid;
            IslandId = islandid;
            Config = config;
        }

        public void Remind(string userid, string islandiid)
        {
            lock (_sync)
            {
                NotifyPings.Add($"{userid}");
                CheckReminderSend(userid, islandiid);
            }
        }

        private void CheckReminderSend(string userid, string islandid)
        {
            try
            {
                if (NotifyPings.Count >= Config.Queues.ReminderQueueCountStart)
                {
                    string msg = $" 注意，你当前在{Config.Queues.ReminderAtPosition}位。\n请提前做好准备！确保游戏已经联网！";
                    DodoBot<T>.SendPersonalMessage(userid, islandid, msg);
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
