using UtilsBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UtilsBot.Datenbank;
using UtilsBot.Domain.Contracts;
using UtilsBot.Domain.Models;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("settings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"settings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;
        services.Configure<BotConfig>(context.Configuration.GetSection("BotConfig"));
        var botConfig = context.Configuration.GetSection("BotConfig").Get<BotConfig>();
        if (botConfig is null)
        {
            throw new ArgumentException("No settings.json provided with section BotConfig.");
        }

        var token = botConfig.TestMode ? configuration["DiscordTokenTest"] : configuration["DiscordToken"];
        if (string.IsNullOrWhiteSpace(token))
            throw new Exception("DiscordTokenTest or DiscordToken not set");

        services.Configure<Secrets>(options => { options.DiscordToken = token; });

        services.AddScoped<IBotRepository, BotRepository>();

        services.AddSingleton<DiscordService>();
        services.AddHostedService(provider => provider.GetRequiredService<DiscordService>());
    })
    .Build();


await host.RunAsync();