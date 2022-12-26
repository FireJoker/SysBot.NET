using PKHeX.Core;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.WinForms;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Pokemon.Dodo;
using SysBot.Pokemon.QQ;

namespace SysBot.Pokemon
{
    /// <summary>
    /// Bot Environment implementation with Integrations added.
    /// </summary>
    public class PokeBotRunnerImpl<T> : PokeBotRunner<T> where T : PKM, new()
    {
        public PokeBotRunnerImpl(PokeTradeHub<T> hub, BotFactory<T> fac) : base(hub, fac)
        {
        }

        public PokeBotRunnerImpl(PokeTradeHubConfig config, BotFactory<T> fac) : base(config, fac)
        {
        }

        private DodoBot<T>? Dodo;
        private MiraiQQBot<T>? QQ;

        protected override void AddIntegrations()
        {
            AddDiscordBot(Hub.Config.Discord.Token);
            AddDodoBot(Hub.Config.Dodo);
            AddQQBot(Hub.Config.QQ);
        }

        private void AddDiscordBot(string apiToken)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
                return;
            var bot = new SysCord<T>(this);
            Task.Run(() => bot.MainAsync(apiToken, CancellationToken.None));
        }

        private void AddQQBot(QQSettings config)
        {
            if (string.IsNullOrWhiteSpace(config.VerifyKey) || string.IsNullOrWhiteSpace(config.Address)) return;
            if (string.IsNullOrWhiteSpace(config.QQ) || string.IsNullOrWhiteSpace(config.GroupId)) return;
            if (QQ != null) return;
            //add qq bot
            QQ = new MiraiQQBot<T>(config, Hub);
        }

        private void AddDodoBot(DodoSettings config)
        {
            if (string.IsNullOrWhiteSpace(config.BaseApi) || string.IsNullOrWhiteSpace(config.ClientId) || string.IsNullOrWhiteSpace(config.Token)) return;
            if (Dodo != null) return;
            Dodo = new DodoBot<T>(config, Hub);
        }
    }
}