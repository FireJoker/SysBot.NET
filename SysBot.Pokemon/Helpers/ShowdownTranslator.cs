using PKHeX.Core;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SysBot.Pokemon
{
    public class ShowdownTranslator<T> where T : PKM
    {
        public static GameStrings GameStringsZh = GameInfo.GetStrings("zh");
        public static GameStrings GameStringsEn = GameInfo.GetStrings("en");
        public static string Chinese2Showdown(string zh)
        {
            string result = "";

            // 添加宝可梦
            int specieNo = GameStringsZh.Species.Skip(1).Select((s, index) => new { Species = s, Index = index + 1 })
                .Where(s => zh.Contains(s.Species)).OrderByDescending(s => s.Species.Length).FirstOrDefault()?.Index ?? -1;

            if (specieNo <= 0) return result;
            zh = zh.Replace(GameStringsZh.Species[specieNo], "");

            // 处理蛋宝可梦
            if (zh.Contains(ShowdownExtensions.eggKeyword))
            {
                result += "Egg ";
                zh = zh.Replace(ShowdownExtensions.eggKeyword, "");

                // Showdown 文本差异，29-尼多兰F，32-尼多朗M，876-爱管侍，
                result += specieNo switch
                {
                    (ushort)Species.NidoranF => "(Nidoran-F)",
                    (ushort)Species.NidoranM => "(Nidoran-M)",
                    (ushort)Species.Indeedee when zh.Contains('母') => "(Indeedee-F)",
                    _ => $"({GameStringsEn.Species[specieNo]})",
                };


                // 识别地区形态
                foreach (var s in ShowdownExtensions.formDict)
                {
                    var formKey = s.Key.EndsWith("形态") ? s.Key : s.Key + "形态";
                    if (!zh.Contains(formKey)) continue;
                    result += $"-{s.Value}";
                    zh = zh.Replace(formKey, "");
                    break;
                }

            }
            // 处理非蛋宝可梦
            else
            {
                // Showdown 文本差异，29-尼多兰F，32-尼多朗M，678-超能妙喵，876-爱管侍，902-幽尾玄鱼, 916-飘香豚
                result += specieNo switch
                {
                    (ushort)Species.NidoranF => "Nidoran-F",
                    (ushort)Species.NidoranM => "Nidoran-M",
                    (ushort)Species.Meowstic when zh.Contains('母') => "Meowstic-F",
                    (ushort)Species.Indeedee when zh.Contains('母') => "Indeedee-F",
                    (ushort)Species.Basculegion when zh.Contains('母') => "Basculegion-F",
                    (ushort)Species.Oinkologne when zh.Contains('母') => "Oinkologne-F",
                    _ => GameStringsEn.Species[specieNo],
                };

                // 识别地区形态
                foreach (var s in ShowdownExtensions.formDict)
                {
                    var formKey = s.Key.EndsWith("形态") ? s.Key : s.Key + "形态";
                    if (!zh.Contains(formKey)) continue;
                    result += $"-{s.Value}";
                    zh = zh.Replace(formKey, "");
                    break;
                }
            }

            // 添加性别
            if (zh.Contains("公"))
            {
                result += " (M)";
                zh = zh.Replace("公", "");
            }
            else if (zh.Contains("母"))
            {
                result += " (F)";
                zh = zh.Replace("母", "");
            }

            // 添加持有物
            foreach (var itemKey in ShowdownExtensions.heldItemKeywords)
            {
                if (!zh.Contains(itemKey)) continue;
                for (int i = 1; i < GameStringsZh.Item.Count; i++)
                {
                    if (GameStringsZh.Item[i].Length == 0) continue;
                    if (!zh.Contains(itemKey + GameStringsZh.Item[i])) continue;
                    result += $" @ {GameStringsEn.Item[i]}";
                    zh = zh.Replace(itemKey + GameStringsZh.Item[i], "");
                    break;
                }
            }

            // 添加等级
            if (Regex.IsMatch(zh, "\\d{1,3}级"))
            {
                string level = Regex.Match(zh, "(\\d{1,3})级").Groups?[1]?.Value ?? "100";
                result += $"\nLevel: {level}";
                zh = Regex.Replace(zh, "\\d{1,3}级", "");
            }

            // 添加超极巨化
            if (typeof(T) == typeof(PK8) && zh.Contains("超极巨"))
            {
                result += "\nGigantamax: Yes";
                zh = zh.Replace("超极巨", "");
            }

            // 添加头目
            if (typeof(T) == typeof(PA8) && zh.Contains("头目"))
            {
                result += "\nAlpha: Yes";
                zh = zh.Replace("头目", "");
            }

            // 添加异色
            foreach (string shinyKey in ShowdownExtensions.shinyTypes.Keys)
            {
                if (zh.Contains(shinyKey))
                {
                    result += ShowdownExtensions.shinyTypes[shinyKey];
                    zh = zh.Replace(shinyKey, "");
                    break;
                }
            }

            // 添加训练家信息
            var lang = ShowdownExtensions.partnerInfo.Keys.FirstOrDefault(zh.Contains);
            if (!string.IsNullOrEmpty(lang))
            {
                result += $"{ShowdownExtensions.partnerInfo[lang]}";
                zh = zh.Replace(lang, "");
            }

            // 添加球种
            for (int i = 1; i < GameStringsZh.balllist.Length; i++)
            {
                if (GameStringsZh.balllist[i].Length == 0) continue;
                if (!zh.Contains(GameStringsZh.balllist[i])) continue;
                var ballStr = GameStringsEn.balllist[i];
                if (typeof(T) == typeof(PA8) && ballStr is "Poké Ball" or "Great Ball" or "Ultra Ball") ballStr = "LA" + ballStr;
                result += $"\nBall: {ballStr}";
                zh = zh.Replace(GameStringsZh.balllist[i], "");
                break;
            }

            // 添加特性
            for (int i = 1; i < GameStringsZh.Ability.Count; i++)
            {
                if (GameStringsZh.Ability[i].Length == 0) continue;
                if (!zh.Contains(GameStringsZh.Ability[i] + "特性")) continue;
                result += $"\nAbility: {GameStringsEn.Ability[i]}";
                zh = zh.Replace(GameStringsZh.Ability[i] + "特性", "");
                break;
            }

            // 添加性格
            for (int i = 0; i < GameStringsZh.Natures.Count; i++)
            {
                if (GameStringsZh.Natures[i].Length == 0) continue;
                if (!zh.Contains(GameStringsZh.Natures[i])) continue;
                result += $"\n{GameStringsEn.Natures[i]} Nature";
                zh = zh.Replace(GameStringsZh.Natures[i], "");
                break;
            }

            // 添加个体值
            foreach (string ivKey in ShowdownExtensions.ivsDict.Keys)
            {
                if (zh.ToUpper().Contains(ivKey))
                {
                    result += "\nIVs: " + ShowdownExtensions.ivsDict[ivKey];
                    zh = Regex.Replace(zh, ivKey, "", RegexOptions.IgnoreCase);
                    break;
                }
            }


            // 添加努力值
            if (zh.Contains("努力值"))
            {
                StringBuilder sb = new();
                sb.Append("\nEVs: ");
                zh = zh.Replace("努力值", "");

                foreach (var stat in ShowdownExtensions.statsDict)
                {
                    string regexPattern = $@"\d{{1,3}}{stat.Key}";
                    if (Regex.IsMatch(zh, regexPattern))
                    {
                        string value = Regex.Match(zh, $@"(\d{{1,3}}){stat.Key}").Groups[1].Value;
                        sb.Append($"{value} {stat.Value} / ");
                        zh = Regex.Replace(zh, regexPattern, "");
                    }
                    else if (Regex.IsMatch(zh, $@"\d{{1,3}}{stat.Value}"))
                    {
                        string value = Regex.Match(zh, $@"(\d{{1,3}}){stat.Value}").Groups?[1]?.Value ?? "";
                        sb.Append($"{value} {stat.Value} / ");
                        zh = Regex.Replace(zh, $@"\d{{1,3}}{stat.Value}", "");
                    }
                }

                if (sb.ToString().EndsWith("/ "))
                {
                    sb.Remove(sb.Length - 2, 2);
                }

                result += sb.ToString();
            }

            // 添加太晶属性
            if (typeof(T) == typeof(PK9))
            {
                for (int i = 0; i < GameStringsZh.Types.Count; i++)
                {
                    if (GameStringsZh.Types[i].Length == 0) continue;
                    if (!zh.Contains("太晶" + GameStringsZh.Types[i])) continue;
                    result += $"\nTera Type: {GameStringsEn.Types[i]}";
                    zh = zh.Replace("太晶" + GameStringsZh.Types[i], "");
                    break;
                }
            }

            // 添加先天证章和后天奖章
            EncounterMovesetGenerator.ResetFilters();
            if (zh.Contains("野生") && typeof(T) == typeof(PK9))
            {
                //为野生宝可梦添加先天证章
                EncounterMovesetGenerator.PriorityList = new EncounterTypeGroup[] {
                    EncounterTypeGroup.Slot,
                    EncounterTypeGroup.Egg,
                    EncounterTypeGroup.Mystery,
                    EncounterTypeGroup.Static,
                    EncounterTypeGroup.Trade
                };

                foreach (string markKey in ShowdownExtensions.marksDict.Keys)
                {
                    if (zh.Contains(markKey))
                    {
                        result += ShowdownExtensions.marksDict[markKey];
                        zh = Regex.Replace(zh, markKey, "", RegexOptions.IgnoreCase);
                        break;
                    }
                }
            }

            // 补充后天获得的全奖章 注意开启Legality=>AllowBatchCommands
            if (typeof(T) == typeof(PK9) && zh.Contains("全奖章"))
            {
                result += "\n.Ribbons=$suggestAll\n.RibbonMarkPartner=True\n.RibbonMarkGourmand=True";
                zh = zh.Replace("全奖章", "");
            }

            // 添加体型大小
            if (zh.Contains("最大"))
            {
                result += "\n.HeightScalar=255\n.WeightScalar=255\n.Scale=255";
                zh = zh.Replace("最大", "");
            }
            else if (zh.Contains("最小"))
            {
                result += "\n.HeightScalar=0\n.WeightScalar=0\n.Scale=0";
                zh = zh.Replace("最小", "");
            }

            // 体型大小并添加证章
            if (typeof(T) == typeof(PK9) && zh.Contains("大个子"))
            {
                result += $"\n.Scale=255\n.RibbonMarkJumbo=True";
                zh = zh.Replace("大个子", "");
            }
            else if (typeof(T) == typeof(PK9) && zh.Contains("小不点"))
            {
                result += $"\n.Scale=0\n.RibbonMarkMini=True";
                zh = zh.Replace("小不点", "");
            }

            //添加全回忆技能
            if (zh.Contains("全技能|全招式"))
            {
                if (typeof(T) == typeof(PK9) || typeof(T) == typeof(PK8))
                {
                    result += "\n.RelearnMoves=$suggestAll";
                }
                else if (typeof(T) == typeof(PA8))
                {
                    result += "\n.MoveMastery=$suggestAll";
                }
                zh = zh.Replace("全技能|全招式", "");
            }


            // 添加技能 原因：PKHeX.Core.ShowdownSet#ParseLines中，若招式数满足4个则不再解析，所以招式文本应放在最后
            for (int moveCount = 0; moveCount < 4; moveCount++)
            {
                var candidateIndex = GameStringsZh.Move.Select((move, index) => new { Move = move, Index = index })
                    .Where(move => move.Move.Length > 0 && zh.Contains("-" + move.Move))
                    .OrderByDescending(move => move.Move.Length).FirstOrDefault()?.Index ?? -1;
                if (candidateIndex < 0) continue;
                result += $"\n-{GameStringsEn.Move[candidateIndex]}";
                zh = zh.Replace("-" + GameStringsZh.Move[candidateIndex], "");
            }

            return result;
        }

        public static bool IsPS(string str) => GameStringsEn.Species.Skip(1).Any(str.Contains);

    }
}