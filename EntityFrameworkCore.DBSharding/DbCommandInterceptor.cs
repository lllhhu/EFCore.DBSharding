using Microsoft.EntityFrameworkCore.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace EntityFrameworkCore.DBSharding
{
    public class DbCommandInterceptor : IObserver<KeyValuePair<string, object>>
    {

        protected List<ShardingRule> shardingRules;

        public DbCommandInterceptor(List<ShardingRule> shardingRules)
        {
            this.shardingRules = shardingRules;
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(KeyValuePair<string, object> value)
        {
            if (value.Key == RelationalEventId.CommandExecuting.Name)
            {
                var command = ((CommandEventData)value.Value).Command;

                foreach (var shardingRule in shardingRules)
                {
                    try
                    {
                        if (command.CommandText.IndexOf("" + shardingRule.TableName + "") > -1)
                        {
                            //                        command.CommandText = @"SET NOCOUNT ON;
                            //INSERT INTO[Bizoffer1] ([Id], [GameAccount], [GameId], [GameInfo], [GameName], [GamePassword], [Name], [Price], [Status], [UserId], [UserName])
                            //VALUES(@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10); ";

                            /*SELECT TOP(1) [e].[Id], [e].[GameAccount], [e].[GameId], [e].[GameInfo], [e].[GameName], [e].[GamePassword], [e].[Name], [e].[Price], [e].[Status], [e].[UserId], [e].[UserName]
            FROM[Bizoffer] AS[e]
    WHERE[e].[Id] = @__get_Item_0*/

                            #region 处理insert语句
                            var sqlStr = command.CommandText;
                            int shardingKeyIndex = -1;
                            string sqlContent = string.Empty;
                            //Match ccMatch = Regex.Match(sqlStr, @"[[\s\S]*?] ((?<content>[\s\S]*?))\s\S*?VALUES ", RegexOptions.ExplicitCapture);
                            Match ccMatch = Regex.Match(sqlStr, shardingRule.TableName + @"[`\]][\s\S]*?((?<content>[\s\S]*?))VALUES", RegexOptions.ExplicitCapture | RegexOptions.Multiline);
                            while (ccMatch.Success)
                            {
                                sqlContent = ccMatch.Groups["content"].Value;
                                ccMatch = ccMatch.NextMatch();
                            }

                            if (!string.IsNullOrEmpty(sqlContent))
                            {
                                sqlContent = sqlContent.Trim(' ').TrimStart('(').TrimEnd(')').Replace("`", "").Replace(")", "").Replace("[", "").Replace("]", "").Replace("\r\n", "");
                                var sqlArr = sqlContent.Split(',');

                                for (int i = 0; i < sqlArr.Length; i++)
                                {
                                    var field = sqlArr[i].Trim();
                                    if (field.ToLower() == shardingRule.ShardingKey.ToLower().Trim())
                                    {
                                        shardingKeyIndex = i;
                                        break;
                                    }
                                }
                            }


                            #endregion

                            #region 处理update ,delete,select
                            sqlContent = string.Empty;
                            ccMatch = Regex.Match(sqlStr, @"WHERE(?<content>[\s\S]+)", RegexOptions.ExplicitCapture);
                            while (ccMatch.Success)
                            {
                                sqlContent = ccMatch.Groups["content"].Value;
                                ccMatch = ccMatch.NextMatch();
                            }

                            if (!string.IsNullOrEmpty(sqlContent))
                            {
                                //var sqlArr = sqlContent.Split(',');
                                //string[] sqlArr = System.Text.RegularExpressions.Regex.Split(sqlContent, @"[AND]");
                                string[] sqlArr = Regex.Split(sqlContent, "AND", RegexOptions.IgnoreCase);

                                for (int i = 0; i < sqlArr.Length; i++)
                                {
                                    var field = sqlArr[i].Trim().Split("=")[0];
                                    if (field.ToLower().Contains("[" + shardingRule.ShardingKey.ToLower().Trim() + "]", StringComparison.CurrentCultureIgnoreCase) || field.ToLower().Contains("`" + shardingRule.ShardingKey.ToLower().Trim() + "`", StringComparison.CurrentCultureIgnoreCase))
                                    {
                                        shardingKeyIndex = i;
                                        break;
                                    }
                                }
                            }

                            #endregion

                            //throw (new Exception("test"));

                            if (shardingKeyIndex <= -1) continue;

                            var fieldValue = command.Parameters[shardingKeyIndex].Value.ToString();
                            command.CommandText = sqlStr.Replace(shardingRule.TableName, shardingRule.BuildTableName(fieldValue));

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("EntityFrameworkCore.DBSharding.OnNext SQL:{0} Exception:{1}",command.CommandText,ex.ToString());
                    }
                }


                //var executeMethod = ((CommandEventData)value.Value).ExecuteMethod;
            }
        }
    }
}
