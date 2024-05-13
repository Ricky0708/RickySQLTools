using iCat.DB.Client.Extension.Web;
using iCat.DB.Client.Factory.Implements;
using iCat.DB.Client.Factory.Interfaces;
using iCat.SQLTools.enums;
using iCat.SQLTools.Forms;
using iCat.SQLTools.Models;
using iCat.SQLTools.Repositories.Implements;
using iCat.SQLTools.Repositories.Interfaces;
using iCat.SQLTools.Services.Implements;
using iCat.SQLTools.Services.Interfaces;
using iCat.SQLTools.Shareds;
using iCat.Worker.Implements;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace iCat.SQLTools
{
    internal static class Program
    {
        public static IServiceProvider? provider { get; private set; }

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var host = CreateHostBuilder().Build();
            provider = host.Services;

            var dbClientProvider = provider.GetRequiredService<iCat.SQLTools.Services.Interfaces.IDBProvider>();

            var task = new IntervalTask(dbClientProvider, 5000, new Worker.Models.IntervalTaskOption
            {
                IsExecuteWhenStart = true,
                RetryInterval = 1000,
                RetryTimes = 5
            });
            task.Start();
            Application.Run(provider.GetRequiredService<MainForm>());
        }

        /// <summary>
        /// Create a host builder to build the service provider
        /// </summary>
        /// <returns></returns>
        static IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IDBClientProvider>(s => s.GetRequiredService<IDBProvider>());
                    services.AddSingleton<IDBProvider, DBProvider>();
                    services.AddSingleton<ISettingConfigService, SettingConfigService>();
                    services.AddSingleton<SettingConfig>(s =>
                    {
                        var data = Task.Run(async () => await s.GetRequiredService<IFileService>().ReadStringFileAsync("settingConfig.json")).Result;
                        return JsonUtil.Deserialize<SettingConfig>(data)!;
                    });
                    services.AddSingleton<IFileService>(s => new FileService("Config", Path.Combine(Application.StartupPath, "Configs")));
                    services.AddSingleton<MainForm>();
                    services.AddKeyedScoped<Form, frmConfigSettingDlg>(nameof(frmConfigSettingDlg));
                    services.AddKeyedScoped<Form, frmTables>(nameof(frmTables));


                    services.AddScoped<ISchemaService, SchemaService>(s =>
                    {
                        var connType = s.GetRequiredService<SettingConfig>();
                        return new SchemaService(connType.ConnectionSetting.ConnectionType, s);
                    });
                    services.AddScoped<ISchemaRepository, MSSQLSchemaRepository>();
                    services.AddScoped<ISchemaRepository, MySQLSchemaRepository>();

                    services.AddScoped<IDBClientFactory, DBClientFactory>();

                });
        }
    }
}
