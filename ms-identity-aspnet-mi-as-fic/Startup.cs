using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.IdentityModel.Logging;
using MiFicExamples.Data;
using MiFicExamples.Models;


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
            //var miClientId = Configuration["AzureAd:ClientCredentials:ManagedIdentityClientId"];

            //var credential = new ChainedTokenCredential(
            //    new ManagedIdentityCredential(clientId: miClientId));

            //string[] scopes = { "https://graph.microsoft.com/.default" };

            //var graphClient = new GraphServiceClient(credential, scopes);

            services.AddOptions();
            var initialScopes = Configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<List<string>>();

            services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                    .AddMicrosoftIdentityWebApp(Configuration.GetSection("AzureAd"))
                    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                    .AddMicrosoftGraph(Configuration.GetSection("DownstreamApis:MicrosoftGraph"))
                    .AddInMemoryTokenCaches();

            //services.AddSingleton<IConfidentialClientApplication>(sp =>
            //{
            //    return ConfidentialClientApplicationBuilder
            //        .Create(Configuration["AzureAd:ClientId"])
            //        .WithAuthority("https://login.microsoftonline.com/9590e3dc-70c9-460a-83f9-d6f5dde741b5/v2.0", false)
            //        .WithClientAssertion(async (AssertionRequestOptions _) =>
            //        {
            //            var managedIdentityCredential = new ManagedIdentityCredential(Configuration["AzureAd:ClientCredentials:ManagedIdentityClientId"]);

            //            // fetch Managed Identity token for the specified audience
            //            var tokenRequestContext = new Azure.Core.TokenRequestContext(["api://AzureADTokenExchange/.default"]);
            //            var accessToken = await managedIdentityCredential.GetTokenAsync(tokenRequestContext);
            //            return accessToken.Token;
            //        })
            //        .WithTenantId(Configuration["AzureAd:TenantId"])
            //        .WithCacheOptions(CacheOptions.EnableSharedCacheOptions)
            //        .Build();
            //});

            services.AddTransient<CommentsContext>();
            services.Configure<AzureStorageConfig>(Configuration.GetSection("AzureStorageConfig"));

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
