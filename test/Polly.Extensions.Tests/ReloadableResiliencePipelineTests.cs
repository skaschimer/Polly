using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly.DependencyInjection;
using Polly.Registry;

namespace Polly.Extensions.Tests;

public class ReloadableResiliencePipelineTests
{
    private static readonly ResiliencePropertyKey<string> TagKey = new("tests.tag");

    [InlineData(null)]
    [InlineData("custom-name")]
    [InlineData("")]
    [Theory]
    public void AddResiliencePipeline_EnsureReloadable(string? name)
    {
        var reloadableConfig = new ReloadableConfiguration();
        reloadableConfig.Reload(new() { { "tag", "initial-tag" } });
        var builder = new ConfigurationBuilder().Add(reloadableConfig);

        var services = new ServiceCollection();

        if (name == null)
        {
            services.Configure<ReloadableStrategyOptions>(builder.Build());
        }
        else
        {
            services.Configure<ReloadableStrategyOptions>(name, builder.Build());
        }

        services.AddResiliencePipeline("my-pipeline", (builder, context) =>
        {
            var options = context.GetOptions<ReloadableStrategyOptions>(name);
            context.EnableReloads<ReloadableStrategyOptions>(name);

            builder.AddStrategy(_ => new ReloadableStrategy(options.Tag), new ReloadableStrategyOptions());
        });

        var serviceProvider = services.BuildServiceProvider();
        var pipeline = serviceProvider.GetRequiredService<ResiliencePipelineProvider<string>>().GetPipeline("my-pipeline");
        var context = ResilienceContextPool.Shared.Get();

        // initial
        pipeline.Execute(_ => "dummy", context);
        context.Properties.GetValue(TagKey, string.Empty).Should().Be("initial-tag");

        // reloads
        for (int i = 0; i < 10; i++)
        {
            reloadableConfig.Reload(new() { { "tag", $"reload-{i}" } });
            pipeline.Execute(_ => "dummy", context);
            context.Properties.GetValue(TagKey, string.Empty).Should().Be($"reload-{i}");
        }
    }

    public class ReloadableStrategy : ResilienceStrategy
    {
        public ReloadableStrategy(string tag) => Tag = tag;

        public string Tag { get; }

        protected override ValueTask<Outcome<TResult>> ExecuteCore<TResult, TState>(
            Func<ResilienceContext, TState, ValueTask<Outcome<TResult>>> callback,
            ResilienceContext context,
            TState state)
        {
            context.Properties.Set(TagKey, Tag);
            return callback(context, state);
        }
    }

    public class ReloadableStrategyOptions : ResilienceStrategyOptions
    {
        public string Tag { get; set; } = string.Empty;
    }

    private class ReloadableConfiguration : ConfigurationProvider, IConfigurationSource
    {
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return this;
        }

        public void Reload(Dictionary<string, string?> data)
        {
            Data = new Dictionary<string, string?>(data, StringComparer.OrdinalIgnoreCase);
            OnReload();
        }
    }
}