using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
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

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            var initialScopes = Configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<List<string>>();

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                    .AddMicrosoftGraph(Configuration.GetSection("DownstreamApis:MicrosoftGraph"))
                    .AddInMemoryTokenCaches();

            services.AddScoped<IClientAssertionCredentialFactory, ClientAssertionCredentialFactory>();
            services.AddScoped<IBlobStorageClient, BlobStorageClient>();

            services.AddScoped<AuthConfig>();
            services.AddScoped<BlobStorageConfig>();
            services.AddScoped<KeyVaultConfig>();

            services.AddAuthorization(options =>
            {
                // By default, all incoming requests will be authorized according to the default policy
                options.FallbackPolicy = options.DefaultPolicy;
            });
            services.AddRazorPages()
                .AddMvcOptions(options => { })
                .AddMicrosoftIdentityUI();

            services.AddControllersWithViews()
                    .AddMicrosoftIdentityUI();

            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole(); // Adds console logs visible in Azure Log Stream
                logging.AddAzureWebAppDiagnostics(); // Enables logs for Azure Web Apps
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
            });
        }
    }
}
