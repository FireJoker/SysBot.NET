using System;
using System.Net.Http;
using DoDo.Open.Sdk.Models.Bots;
using DoDo.Open.Sdk.Models.Events;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Services;
using PKHeX.Core;
using PKHeX.Core.Enhancements;
using SysBot.Base;

namespace SysBot.Pokemon.Dodo
{
    public class PokemonProcessService<TP> : EventProcessService where TP : PKM, new()
    {
        private readonly OpenApiService _openApiService;
        private static readonly string LogIdentity = "DodoBot";
        private readonly string _channelId;
        private string _botDodoSourceId = default!;

        public PokemonProcessService(OpenApiService openApiService, string channelId)
        {
            _openApiService = openApiService;
            _channelId = channelId;
        }


        public override void Connected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Disconnected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Reconnected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Exception(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void PersonalMessageEvent<T>(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyPersonalMessage<T>>> input)
        {
            var eventBody = input.Data.EventBody;

            if (eventBody.MessageBody is MessageBodyText messageBodyText)
            {
                var content = messageBodyText.Content;
                if (content.Contains("取消"))
                {
                    var result = DodoBot<TP>.Info.ClearTrade(ulong.Parse(eventBody.DodoSourceId));
                    DodoBot<TP>.SendPersonalMessage(eventBody.DodoSourceId, eventBody.IslandSourceId, $"{GetClearTradeMessage(result)}");
                }
                else if (content.Contains("位置"))
                {
                    var result = DodoBot<TP>.Info.CheckPosition(ulong.Parse(eventBody.DodoSourceId));
                    DodoBot<TP>.SendPersonalMessage(eventBody.DodoSourceId, eventBody.IslandSourceId, $"{GetQueueCheckResultMessage(result)}");
                }
                else
                {
                    DodoBot<TP>.SendPersonalMessage(eventBody.DodoSourceId, eventBody.IslandSourceId, $"发送位置可查询当前位置\n发送取消可取消排队");
                }
            }
        }

        public override void ChannelMessageEvent<T>(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelMessage<T>>> input)
        {
            var eventBody = input.Data.EventBody;

            if (!string.IsNullOrWhiteSpace(_channelId) && eventBody.ChannelId != _channelId) return;

            if (eventBody.MessageBody is MessageBodyFile messageBodyFile)
            {
                DodoBot<TP>.SetChannelMessageWithdraw(eventBody.MessageId, "");

                if (!ValidFileSize(messageBodyFile.Size ?? 0) || !ValidFileName(messageBodyFile.Name))
                {
                    DodoBot<TP>.SendChannelMessage("非法文件，试试群文件\n或者发送帮助以查看帮助", eventBody.ChannelId);
                    return;
                }
                using var client = new HttpClient();
                var downloadBytes = client.GetByteArrayAsync(messageBodyFile.Url).Result;
                var p = GetPKM(downloadBytes);
                if (p is TP pkm)
                {
                    DodoHelper<TP>.StartTrade(pkm, eventBody.DodoSourceId, eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId);
                }

                return;
            }

            if (eventBody.MessageBody is not MessageBodyText messageBodyText) return;

            var content = messageBodyText.Content;

            LogUtil.LogInfo($"{eventBody.Personal.NickName}({eventBody.DodoSourceId}):{content}", LogIdentity);
            if (_botDodoSourceId == null)
            {
                _botDodoSourceId = _openApiService.GetBotInfo(new GetBotInfoInput()).DodoSourceId;
            }
            if (!content.Contains($"<@!{_botDodoSourceId}>")) return;

            content = content.Substring(content.IndexOf('>') + 1);
            if (content.Trim().StartsWith("trade"))
            {
                content = content.Replace("trade", "");
                DodoHelper<TP>.StartTrade(content, eventBody.DodoSourceId, eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId);
                return;
            }
            else if (content.Trim().StartsWith("clone"))
            {
                DodoHelper<TP>.StartClone(eventBody.DodoSourceId, eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId);
                return;
            }
            else if (content.Trim().Contains("+"))
            {
                DodoHelper<TP>.StartMultiTrade(content.Trim(), eventBody.DodoSourceId, eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId);
                return;
            }
            else if (content.Trim().Contains("队伍"))
            {
                DodoHelper<TP>.StartTeamTrade(content.Replace("队伍", "").Trim(), eventBody.DodoSourceId, eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId);
                return;
            }
            //else if (content.Trim().StartsWith("seed"))
            //{
            //    DodoHelper<TP>.StartDump(eventBody.DodoSourceId, eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId);
            //    return;
            //}
            //else if (content.Trim().StartsWith("dump"))
            //{
            //    DodoHelper<TP>.StartDump(eventBody.DodoSourceId, eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId);
            //    return;
            //}

            var ps = ShowdownTranslator<TP>.Chinese2Showdown(content);
            if (!string.IsNullOrWhiteSpace(ps))
            {
                LogUtil.LogInfo($"收到命令\n{ps}", LogIdentity);
                DodoHelper<TP>.StartTrade(ps, eventBody.DodoSourceId, eventBody.Personal.NickName, eventBody.ChannelId, eventBody.IslandSourceId);
            }
            else if (content.Contains("取消"))
            {
                var result = DodoBot<TP>.Info.ClearTrade(ulong.Parse(eventBody.DodoSourceId));
                DodoBot<TP>.SendChannelAtMessage(ulong.Parse(eventBody.DodoSourceId),
                    $"\n{GetClearTradeMessage(result)}",
                    eventBody.ChannelId);
            }
            else if (content.Contains("位置"))
            {
                var result = DodoBot<TP>.Info.CheckPosition(ulong.Parse(eventBody.DodoSourceId));
                DodoBot<TP>.SendChannelAtMessage(ulong.Parse(eventBody.DodoSourceId),
                    $"\n{GetQueueCheckResultMessage(result)}",
                    eventBody.ChannelId);
            }
            else if (content.Contains("帮助"))
            {
                DodoBot<TP>.SendChannelAtMessage(ulong.Parse(eventBody.DodoSourceId),
                    DisplayHelpInfo(),
                    eventBody.ChannelId);
            }
            else
            {
                DodoBot<TP>.SendChannelAtMessage(ulong.Parse(eventBody.DodoSourceId),
                    $"\n无法识别，请核对官方译名",
                    eventBody.ChannelId);
            }
        }

        public string GetQueueCheckResultMessage(QueueCheckResult<TP> result)
        {
            if (!result.InQueue || result.Detail is null)
                return "你不在队列里";
            var msg = $"你在第{result.Position}位";
            var pk = result.Detail.Trade.TradeData;
            if (pk.Species != 0)
                msg += $"\n{(pk.IsShiny ? "异色" : string.Empty)}{ShowdownTranslator<TP>.GameStringsZh.Species[result.Detail.Trade.TradeData.Species]}{(pk.IsEgg ? "(蛋)" : string.Empty)}准备中";
            return msg;
        }

        private static string GetClearTradeMessage(QueueResultRemove result)
        {
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "正在交换中,无法取消",
                QueueResultRemove.CurrentlyProcessingRemoved => "正在删除，请稍等",
                QueueResultRemove.Removed => "已从队列移除",
                _ => "你不在队列里",
            };
        }

        private static bool ValidFileSize(long size)
        {
            if (typeof(TP) == typeof(PK8) || typeof(TP) == typeof(PB8) || typeof(TP) == typeof(PK9))
            {
                return size == 344;
            }

            if (typeof(TP) == typeof(PA8))
            {
                return size == 376;
            }

            return false;
        }

        private static bool ValidFileName(string fileName)
        {
            return (typeof(TP) == typeof(PK8) && fileName.EndsWith("pk8", StringComparison.OrdinalIgnoreCase)
                    || typeof(TP) == typeof(PB8) && fileName.EndsWith("pb8", StringComparison.OrdinalIgnoreCase)
                    || typeof(TP) == typeof(PA8) && fileName.EndsWith("pa8", StringComparison.OrdinalIgnoreCase)
                    || typeof(TP) == typeof(PK9) && fileName.EndsWith("pk9", StringComparison.OrdinalIgnoreCase));
        }

        private static PKM? GetPKM(byte[] bytes)
        {
            if (typeof(TP) == typeof(PK8)) return new PK8(bytes);
            else if (typeof(TP) == typeof(PB8)) return new PB8(bytes);
            else if (typeof(TP) == typeof(PA8)) return new PA8(bytes);
            else if (typeof(TP) == typeof(PK9)) return new PK9(bytes);
            return null;
        }

        public static string DisplayHelpInfo()
        {
            string HelpInfo = "";
            if (typeof(TP) == typeof(PA8))
            {
                HelpInfo = 
                    "当前版本为宝可梦阿尔宙斯" +
                    "\n支持定制异色/头目/形态/球种/性别/性格/特性/个体值/努力值/" +
                    "\n基础定制格式为：" +
                    "\n异色头目飞梭球爽朗裙儿小姐洗翠的样子**形态**" +
                    "\n异色头目飞梭球40级未知图腾Y**形态**"+
                    "\n飞梭球母六尾阿罗拉的样子**形态**" +
                    "\n多边兽２型，多边兽乙型";
            }
            else if (typeof(TP) == typeof(PK8))
            {
                HelpInfo = 
                    "当前版本为宝可梦剑盾" +
                    "\n支持定制异色/形态/球种/极巨化/性别/性格/特性/持有物/个体值/努力值/技能" +
                    "\n基础定制格式为：" +
                    "\n异色66级友友球超级巨无防守怕寂寞5V0S努力值252生命252攻击4防御母怪力持有客房服务" +
                    "-健美-近身战-十万马力-双倍奉还"+
                    "\n珍钻要用自己抓的宝可梦进行交换";
            }
            else if (typeof(TP) == typeof(PB8))
            {
                HelpInfo = 
                    "当前版本为宝可梦明亮珍珠&晶灿钻石" +
                    "\n支持定制异色/形态/球种/性别/性格/特性/持有物/个体值/努力值/技能" +
                    "\n基础定制格式为：" +
                    "\n异色80级友友球无防守怕寂寞5V0S努力值252生命252攻击4防御母怪力持有客房服务" +
                    "-健美-近身战-十万马力-双倍奉还"+
                    "\n异色卡璞・鳍鳍";
            }
            else if (typeof(TP) == typeof(PK9))
            {
                HelpInfo =
                    "当前版本为宝可梦朱紫" +
                    "\n支持定制异色/形态/球种/性别/性格/特性/持有物/太晶属性/个体值/努力值/技能" +
                    "\n定制示例为：" +
                    "\n异色甜蜜球母仙子伊布太晶妖精内敛性格5V0A**努力值**252生命252特攻妖精皮肤**特性持有**特性膏药" +
                    "-巨声-暗影球-假哭-冥想";

            }
            return HelpInfo;
        }

        public override void MessageReactionEvent(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyMessageReaction>> input)
        {
            // Do nothing
        }
    }
}