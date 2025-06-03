using Microsoft.IdentityModel.Logging;
using MiFicExamples.Auth;
using MiFicExamples.Auth.Configuration;
using MiFicExamples.Storage;
using MiFicExamples.Storage.Configuration;
using MiFicExamples.Vault.Configuration;


namespace MiFicExamples
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<ICredentialFactory, CredentialFactory>();
            services.AddScoped<IBlobStorageClient, BlobStorageClient>();

            services.AddScoped<AuthConfig>();
            services.AddScoped<AzureStorageConfig>();
            services.AddScoped<KeyVaultConfig>();

            services.AddRazorPages();

            services.AddControllersWithViews();

            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole(); // Adds console logs visible in Azure Log Stream
                logging.AddAzureWebAppDiagnostics(); // Enables logs for Azure Web Apps
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                IdentityModelEventSource.ShowPII = true;
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
