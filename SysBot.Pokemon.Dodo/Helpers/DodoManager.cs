using System;
using System.Collections.Generic;
using System.Linq;

namespace SysBot.Pokemon.Dodo
{
    public class DodoManager
    {
        public readonly DodoSettings Config;
        public ulong Owner { get; internal set; }

        public RemoteControlAccessList BlacklistedUsers => Config.UserBlacklist;
        public RemoteControlAccessList WhitelistedChannels => Config.ChannelWhitelist;

        public RemoteControlAccessList SudoRoles => Config.RoleSudo;
        public RemoteControlAccessList FavoredRoles => Config.RoleFavored;
        public RemoteControlAccessList RoleTradeFolder => Config.RoleCanTradeFolder;
        public RemoteControlAccessList RoleMultiTrade => Config.RoleCanMultiTrade;
        public RemoteControlAccessList RoleTeamTrade => Config.RoleCanTeamTrade;

        public RemoteControlAccessList RoleTradeBin => Config.RoleCanTradeBin;

        public RequestSignificance GetSignificance(IEnumerable<string> roles)
        {
            var result = RequestSignificance.None;
            foreach (var r in roles)
            {
                if (SudoRoles.Contains(r))
                    result = RequestSignificance.Favored;
                if (FavoredRoles.Contains(r))
                    result = RequestSignificance.Favored;
            }
            return result;
        }

        public DodoManager(DodoSettings cfg) => Config = cfg;

        public bool GetHasRoleAccess(string type, IEnumerable<string> roles)
        {
            var set = GetSet(type);
            return (set.AllowIfEmpty && set.List.Count == 0) || roles.Any(set.Contains);
        }

        private RemoteControlAccessList GetSet(string type) => type switch
        {
            nameof(RoleTradeFolder) => RoleTradeFolder,
            nameof(RoleMultiTrade) => RoleMultiTrade,
            nameof(RoleTeamTrade) => RoleTeamTrade,
            nameof(RoleTradeBin) => RoleTradeBin,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }
}