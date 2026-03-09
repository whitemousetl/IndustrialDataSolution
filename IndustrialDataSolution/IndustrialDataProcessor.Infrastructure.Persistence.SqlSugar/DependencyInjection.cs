using IndustrialDataProcessor.Domain.Repositories;
using IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace IndustrialDataProcessor.Infrastructure.Persistence.SqlSugar;

public static class DependencyInjection
{
    public static IServiceCollection AddPostgreSqlPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("数据库连接字符串未配置");

        services.AddTransient<ISqlSugarClient>(provider =>
        {
            var db = new SqlSugarClient(new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = DbType.PostgreSQL,
                IsAutoCloseConnection = true,
                MoreSettings = new ConnMoreSettings
                {
                    PgSqlIsAutoToLower = false, // 关闭自动转小写
                }
            });

            // 可选：配置 SQL 日志
#if DEBUG
            //db.Aop.OnLogExecuting = (sql, pars) =>
            //{
            //    Console.WriteLine($"[SQL]: {sql}");
            //    Console.WriteLine($"[Params]: {string.Join(", ", pars?.Select(p => $"{p.ParameterName}={p.Value}") ?? Array.Empty<string>())}");
            //};
#endif

            return db;
        });

        // 2. 注入仓库实现
        // 这样外部请求 IWorkstationConfigEntityRepository 时，就会使用这里的具体实现
        services.AddScoped<IWorkstationConfigEntityRepository, WorkstationConfigEntityRepository>();
        services.AddSingleton<IEquipmentDataStorageRepository, EquipmentDataStorageRepository>();

        return services;
    }
}