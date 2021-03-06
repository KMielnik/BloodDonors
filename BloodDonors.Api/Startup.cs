﻿using System;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BloodDonors.Infrastructure.Services;
using BloodDonors.Core.Repositories;
using BloodDonors.Infrastructure.Mappers;
using BloodDonors.Infrastructure.Repositories;
using BloodDonors.Infrastructure.EntityFramework;
using Microsoft.IdentityModel.Tokens;

namespace BloodDonors.API
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<IDonorRepository, DonorRepository>();
            services.AddScoped<IBloodDonationRepository, BloodDonationRepository>();
            services.AddScoped<IPersonnelRepository, PersonnelRepository>();
            services.AddScoped<IBloodTypeRepository, BloodTypeRepository>();
            services.AddScoped<IDonorService, DonorService>();
            services.AddScoped<IBloodDonationService, BloodDonationService>();
            services.AddScoped<IBloodTypeService, BloodTypeService>();
            services.AddScoped<IPersonnelService, PersonnelService>();
            services.AddScoped<IDataInitializer, DataInitializer>();
            services.AddSingleton<IJwtService, JwtService>();
            services.AddSingleton<IConfiguration>(Configuration);
            services.AddSingleton<IEncrypter, Encrypter>();
            services.AddSingleton(AutoMapperConfig.Initialize());

            services.AddEntityFrameworkSqlServer()
                .AddEntityFrameworkInMemoryDatabase()
                .AddDbContext<BloodDonorsContext>();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if(env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseJwtBearerAuthentication(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Configuration.GetSection("jwt:issuer").Value,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(Configuration.GetSection("jwt:key").Value)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }
            });

            var dataInitializer = app.ApplicationServices.GetService<IDataInitializer>();
            dataInitializer.SeedAsync();
            
            app.UseStatusCodePages("text/plain", "Status code: {0}");
            app.UseMvc();
        }
    }
}
