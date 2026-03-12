using FilmesAPI.Configuration;
using FilmesAPI.Data;
using FilmesAPI.Models;
using FilmesAPI.Services;
using FilmesAPI.Workers;
using FlimesAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);


JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


var connectionString = builder.Configuration.GetConnectionString("FilmeConnection");

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseLazyLoadingProxies().UseMySql(connectionString,
    ServerVersion.AutoDetect(connectionString)));

builder.Services.AddIdentity<Usuario, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<PortuguesIdentityErrorDescriber>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSettings:SecretKey"])), // Certifique-se que essa chave existe no appsettings.json
        ValidateAudience = false, // não validar audience por enquanto (ambiente dev)
        ValidateIssuer = false,   // não validar issuer por enquanto (ambiente dev)
        ClockSkew = TimeSpan.Zero,

    };
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CinemaService>();
builder.Services.AddScoped<EnderecoService>();
builder.Services.AddScoped<FilmeService>();
builder.Services.AddScoped<SessaoService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHttpClient<TmdbService>();
builder.Services.AddScoped<RabbitMqService>();
builder.Services.AddHostedService<EmailWorker>();

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
    //options.SerializerSettings.DateFormatString = "dd-MM-yyyy HH:mm";
});

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FilmesAPI", Version = "v1" });
    c.EnableAnnotations();

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT desta maneira: Bearer {seu token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("wasm", policy =>
    {
        policy.WithOrigins(
                "https://localhost:7015",
                "http://localhost:5500",
                "http://localhost:5501",
                "http://127.0.0.1:5500",
                "http://moovcine.vercel.app",
                "https://moovcine.vercel.app",
                "https://moovcine.site",
                "https://www.moovcine.site",
                "http://localhost:8081",
                "http://127.0.0.1:8081",
                "https://localhost:8081",
                "https://127.0.0.1:8081"
               )
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseRouting();
app.UseCors("wasm");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var configuration = services.GetRequiredService<IConfiguration>(); // <--- PEGAR CONFIGURAÇÃO
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        db.Database.Migrate();

        var userManager = services.GetRequiredService<UserManager<Usuario>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync("admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("admin"));
        }

        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser == null)
        {
            adminUser = new Usuario
            {
                UserName = "admin",
                Email = "admin@moovcine.com",
                DataNascimento = DateTime.Now,
                NomeCompleto = "Administrador do Sistema"
            };
            string adminPassword = configuration["AdminPassword"] ?? "SenhaPadraoInsegura123!";

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "admin");
                Console.WriteLine("Admin criado com sucesso");
            }
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erro ao migrar ou criar admin.");
    }
}


app.UseSwagger();
app.UseSwaggerUI();



// app.UseHttpsRedirection(); <--- evitando erro de SSL no container



app.Run();