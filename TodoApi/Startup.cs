using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http;
using TodoApi.Models;
using TodoApi.Repositories;
using TodoApi.Authentication;
using TodoApi.Identity;
using TodoApi.Data;

namespace TodoApi
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
            services.AddControllers();

            AddAuthentication(services);
            AddIdentity(services);
            AddDependencyInjection(services);
            AddDbContext(services);
            AddSwagger(services);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TodoApi v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            SetAuthCookiePolicy(app);

            app.UseAuthentication();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }

        /// <summary>
        /// Add authentication.
        /// </summary>
        /// <param name="services"></param>
        private void AddAuthentication(IServiceCollection services)
        {
            services.AddAuthentication()
                .AddCookie(options =>
                    {
                        options.EventsType = typeof(CustomCookieAuthenticationEvents);
                    })  
                .AddJwtBearer(x =>
                    {
                        x.RequireHttpsMetadata = false;
                        x.SaveToken = true;
                        x.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                            {
                                ValidateIssuerSigningKey = true,
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                IssuerSigningKey = new SymmetricSecurityKey(
                                    Encoding.ASCII.GetBytes(Configuration["JWT:Secret"]))
                            };
                    });
        }

        /// <summary>
        /// Perform dependency injection.
        /// </summary>
        /// <param name="services"></param>
        private void AddDependencyInjection(IServiceCollection services)
        {   
            services.AddScoped<IMyClaim, MyClaim>();
            services.AddScoped<ITodoItemsRepository, TodoItemsRepository>();
            services.AddScoped<CustomCookieAuthenticationEvents>();
            services.AddScoped<IUserManagerWrapper, UserManagerWrapper>();
            services.AddScoped<IRoleManagerWrapper, RoleManagerWrapper>();
            services.AddSingleton<IJwtAuth>(new JwtAuth(Configuration["JWT:Secret"]));
            services.AddSingleton<ICookieAuth>(new CookieAuth());
        }

        /// <summary>
        /// Add DB context.
        /// </summary>
        /// <param name="services"></param>
        private void AddDbContext(IServiceCollection services)
        {
            services.AddDbContext<TodoContext>(opt =>
                opt.UseInMemoryDatabase("TodoList"));
                
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    Configuration.GetConnectionString("DefaultConnection")));
        }

        /// <summary>
        /// Add Identity.
        /// </summary>
        /// <param name="services"></param>
        private void AddIdentity(IServiceCollection services)
        {
            services.AddIdentity<AppUser, IdentityRole>()  
                .AddEntityFrameworkStores<ApplicationDbContext>()  
                .AddDefaultTokenProviders();
        }

        /// <summary>
        /// Set up Swagger.
        /// </summary>
        /// <param name="services"></param>
        private void AddSwagger(IServiceCollection services)
        {
            services.AddSwaggerGen(swagger =>
                {
                    swagger.SwaggerDoc("v1", new OpenApiInfo { Title = "TodoApi", Version = "v1" });

                    // To Enable authorization using Swagger (JWT)    
                    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()  
                    {  
                        Name = "Authorization",  
                        Type = SecuritySchemeType.ApiKey,  
                        Scheme = "Bearer",  
                        BearerFormat = "JWT",  
                        In = ParameterLocation.Header,  
                        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",  
                    });  
                    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement  
                    {  
                        {  
                            new OpenApiSecurityScheme  
                            {  
                                Reference = new OpenApiReference  
                                {  
                                    Type = ReferenceType.SecurityScheme,  
                                    Id = "Bearer"  
                                }  
                            },  
                            new string[] {}    
                        }  
                    });  
                });
        }
    
        /// <summary>
        /// Set the auth cookie policy.
        /// </summary>
        /// <param name="app"></param>
        private void SetAuthCookiePolicy(IApplicationBuilder app)
        {
            app.UseCookiePolicy(new CookiePolicyOptions
                {
                    MinimumSameSitePolicy = SameSiteMode.Lax,
                });
        }
    }
}
