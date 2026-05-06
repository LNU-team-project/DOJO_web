using Microsoft.Extensions.DependencyInjection;
using WebOptimizer;

namespace DOJO2.Presentation.Configuration;

public static class WebOptimizerBundles
{
    public static IServiceCollection AddProjectBundles(this IServiceCollection services)
    {
        services.AddWebOptimizer(pipeline =>
        {
            pipeline.AddCssBundle(
                "/css/vendor.bundle.min.css",
                "/lib/bootstrap/dist/css/bootstrap.min.css");

            pipeline.AddCssBundle(
                "/css/site.bundle.min.css",
                "/css/site.css");

            pipeline.AddCssBundle(
                "/css/dashboard.bundle.min.css",
                "/css/todo.css",
                "/css/statistics.css",
                "/css/rooms.css",
                "/css/hero-widget.css");

            pipeline.AddJavaScriptBundle(
                "/js/vendor.bundle.min.js",
                "/lib/jquery/dist/jquery.min.js",
                "/lib/bootstrap/dist/js/bootstrap.bundle.min.js");

            pipeline.AddJavaScriptBundle(
                "/js/site.bundle.min.js",
                "/js/site.js");

            pipeline.AddJavaScriptBundle(
                "/js/dashboard.bundle.min.js",
                "/lib/microsoft/signalr/dist/browser/signalr.min.js",
                "/js/dashboard.js",
                "/js/todo.js",
                "/js/plan.js",
                "/js/schedule.js",
                "/js/friends.js",
                "/js/rooms.js",
                "/js/profile.js",
                "/js/mini-calendar.js",
                "/js/pomodoro.js",
                "/js/statistics.js",
                "/js/hero-widget.js",
                "/js/leaderboard.js");
        });

        return services;
    }
}

