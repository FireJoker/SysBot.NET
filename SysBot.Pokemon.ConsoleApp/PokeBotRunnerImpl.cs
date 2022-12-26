using PKHeX.Core;
using SysBot.Pokemon.Discord;
using System.Threading;
using System.Threading.Tasks;
using SysBot.Pokemon.QQ;
using SysBot.Pokemon.Dodo;

namespace SysBot.Pokemon.ConsoleApp
{
    /// <summary>
    /// Bot Environment implementation with Integrations added.
    /// </summary>
    public class PokeBotRunnerImpl<T> : PokeBotRunner<T> where T : PKM, new()
    {
        public PokeBotRunnerImpl(PokeTradeHub<T> hub, BotFactory<T> fac) : base(hub, fac) { }
        public PokeBotRunnerImpl(PokeTradeHubConfig config, BotFactory<T> fac) : base(config, fac) { }

        private DodoBot<T>? Dodo;
        private MiraiQQBot<T>? QQ;

        protected override void AddIntegrations()
        {
            AddDiscordBot(Hub.Config.Discord);
            AddDodoBot(Hub.Config.Dodo); 
            AddQQBot(Hub.Config.QQ);

        }

        private void AddDiscordBot(DiscordSettings config)
        {
            var token = config.Token;
            if (string.IsNullOrWhiteSpace(token))
                return;

            var bot = new SysCord<T>(this);
            Task.Run(() => bot.MainAsync(token, CancellationToken.None), CancellationToken.None);
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
