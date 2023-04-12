using System;
using GreenPipes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Play.Common.Identity;
using Play.Common.MassTransit;
using Play.Common.MongoDB;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service;

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
        services
        .AddMongo()
        .AddMongoRepository<InventoryItem>("InventoryItems")
        .AddMongoRepository<CatalogItem>("CatalogItems")
        .AddMassTransitWithRabbitMQ(retryConfig =>
        {
            retryConfig.Interval(3, TimeSpan.FromSeconds(5));
            retryConfig.Ignore<UnknownItemException>();
        })
        .AddJwtBearerAuthentication()
        ;

        // RetryPolicies(services);

        services.AddControllers();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Play.Inventory.Service", Version = "v1" });
        });
    }


    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Play.Inventory.Service v1"));
            app.UseCors(cfg =>
            {
                cfg.WithOrigins(Configuration["AllowedOrigin"]).AllowAnyHeader().AllowAnyMethod();
            });
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }

    // private static void RetryPolicies(IServiceCollection services)
    // {
    //     Random jitter = new Random();
    //     services
    //     .AddHttpClient<CatalogClient>(c =>
    //     {
    //         c.BaseAddress = new Uri("https://localhost:5001");
    //     })
    //     .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().WaitAndRetryAsync(
    //             2,
    //             retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(jitter.Next(0, 999)),
    //             onRetry: (outcome, timespan, retryAttempt) =>
    //             {
    //                 Console.WriteLine($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryAttempt}");
    //             })
    //     )
    //     .AddTransientHttpErrorPolicy(builder => builder.Or<TimeoutRejectedException>().CircuitBreakerAsync(3, TimeSpan.FromSeconds(15)))
    //     .AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(3));
    // }
}
