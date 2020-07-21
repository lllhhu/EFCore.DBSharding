using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EntityFrameworkCore.DBSharding
{
    public class CommandListener : IObserver<DiagnosticListener>
    {

        public CommandListener(List<ShardingRule> shardingRules)
        {
            _dbCommandInterceptor = new DbCommandInterceptor(shardingRules);
        }

        private readonly DbCommandInterceptor _dbCommandInterceptor;

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == DbLoggerCategory.Name)
            {
                listener.Subscribe(_dbCommandInterceptor);
            }
        }
    }
}
