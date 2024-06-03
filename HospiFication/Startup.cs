using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HospiFication.Models;   // пространство имен моделей
using Microsoft.EntityFrameworkCore; // пространство имен EntityFramework
using Microsoft.AspNetCore.Authentication.Cookies;


namespace HospiFication
{
    public class Startup
    {
        public static string file;
        public static string[] lines = new string[3];
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            string connection = "Server = (localdb)\\mssqllocaldb;" +
                    "Database = TryHospificationMagTestThinkLast; " +
                    "Trusted_Connection = true";
            services.AddDbContext<BaseContext>(options =>
                              options.UseSqlServer(connection));
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = new Microsoft.AspNetCore.Http.PathString("/Home/Login");
                    options.AccessDeniedPath = new Microsoft.AspNetCore.Http.PathString("/Home/Login");
                });
            services.AddMvc();
            file = @"J:\Учёба\Магистратура\2 курс\2 семестр\Дипломный проект\Программа\HospiFicationWithSyncNew\LoginAndPassForNotification.txt";
            lines = System.IO.File.ReadAllLines(file);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {


            app.UseDeveloperExceptionPage();
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Login}/{id?}");
            });
        }
    }
}
