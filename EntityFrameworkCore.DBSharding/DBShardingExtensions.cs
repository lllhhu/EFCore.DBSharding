using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EntityFrameworkCore.DBSharding
{
    public static class DBShardingExtensions
    {
        public static IServiceCollection UseDBSharding(this IServiceCollection services, List<ShardingRule> shardingRules)
        {
            DiagnosticListener.AllListeners.Subscribe(new CommandListener(shardingRules));
            return services;
        }
    }
}
