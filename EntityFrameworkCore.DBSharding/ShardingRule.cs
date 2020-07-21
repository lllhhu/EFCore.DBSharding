using System;
using System.Text;

namespace EntityFrameworkCore.DBSharding
{
    public class ShardingRule
    {
        public virtual string TableName
        {
            get;
            set;
        }

        public virtual string ShardingKey
        {
            get;
            set;
        }

        public virtual string BuildTableName(string value)
        {
            return TableName;
        }
    }

    public class ModShardingRule : ShardingRule
    {
        public int Mod { get; set; }

        public override string BuildTableName(string value)
        {
            var ruleTableName = TableName;
            var modValue = (MurmurHash2.Hash(Encoding.UTF8.GetBytes(value)) % Mod);
            if (modValue != 0)
            {
                ruleTableName = ruleTableName + "_" + modValue.ToString();
            }

            return ruleTableName;
        }
    }

    public class DateShardingRule : ShardingRule
    {
        public override string BuildTableName(string value)
        {
            var date = DateTime.Parse(value).ToString("yyyyMM");
            return TableName + "_" + date;
        }
    }
}
