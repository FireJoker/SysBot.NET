using System;
using System.ComponentModel;
using System.Data;
using System.Linq;

namespace SysBot.Pokemon
{
    public class DodoSettings
    {
        private const string Startup = nameof(Startup);
        private const string Roles = nameof(Roles);
        private const string Users = nameof(Users);
        private const string Channels = nameof(Channels);

        public override string ToString() => "Dodo Integration Settings";

        // Startup

        [Category(Startup), Description("接口地址")]
        public string BaseApi { get; set; } = "https://botopen.imdodo.com";

        [Category(Startup), Description("机器人唯一标识")]
        public string ClientId { get; set; } = string.Empty;

        [Category(Startup), Description("机器人鉴权Token")]
        public string Token { get; set; } = string.Empty;

        [Category(Startup), Description("机器人响应频道id")]
        public string ChannelId { get; set; } = string.Empty;

        // Whitelists

        [Category(Roles), Description("Users with this role are allowed to use Giveaway.")]
        public RemoteControlAccessList RoleCanGiveaway { get; set; } = new() { AllowIfEmpty = false };

        [Category(Roles), Description("Users with this role are allowed to use TradeFolder.")]
        public RemoteControlAccessList RoleCanTradeFolder { get; set; } = new() { AllowIfEmpty = false };

        [Category(Roles), Description("Users with this role are allowed to use MultiTrade.")]
        public RemoteControlAccessList RoleCanMultiTrade { get; set; } = new() { AllowIfEmpty = false };

        // Operation

        [Category(Roles), Description("Users with this role are allowed to join the queue with a better position.")]
        public RemoteControlAccessList RoleFavored { get; set; } = new() { AllowIfEmpty = false };

        [Category(Users), Description("Users with these user IDs cannot use the bot.")]
        public RemoteControlAccessList UserBlacklist { get; set; } = new();

        [Category(Channels), Description("Channels with these IDs are the only channels where the bot acknowledges commands.")]
        public RemoteControlAccessList ChannelWhitelist { get; set; } = new();
    }
}