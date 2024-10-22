using Microsoft.AspNetCore.Localization;
using System.Globalization;
using YukiPastebin.Hubs;
using YukiPastebin.Models;

namespace YukiPastebin {
    public class Program {
        public static void Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddSingleton<MessageHub>();
            builder.Services.AddSingleton<Storage>();

            builder.Services.AddRazorPages().AddViewLocalization();
            builder.Services.AddSignalR();
            builder.Services.AddControllers();

            builder.WebHost.ConfigureKestrel(options => {
                options.Limits.MaxRequestBodySize = 1024L * 1024 * 1024 * 1024;
            });

            var app = builder.Build();

            var supportedCultures = new List<CultureInfo> {
                new("en-US"),
                new( "zh-CN" ),
            };
            var options = new RequestLocalizationOptions {
                DefaultRequestCulture = new RequestCulture(supportedCultures[0]),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };
            app.UseRequestLocalization(options);

            app.UsePathBase("/pastebin");

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment()) {
                app.UseExceptionHandler("/Error");
            }
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.MapHub<MessageHub>("/MessageHub");

            app.MapDefaultControllerRoute();

            app.Run();
        }
    }
}