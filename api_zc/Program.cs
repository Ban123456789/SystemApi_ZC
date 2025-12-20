using Accura_MES.Interfaces.Services;
using Accura_MES.Middlewares;
using Accura_MES.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//�K�[�Ҧ�controller
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Accura API",
        Version = "v1",
        Description = "GLHF"
    });

    var xmlFile = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    c.IncludeXmlComments(xmlFile);

    // �]�w Header
    c.OperationFilter<AddRequiredHeaderParameter>();
});


// �b�K�[�A�Ȫ��a��A�t�m CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin() // ���\����ӷ�
              .AllowAnyMethod() // ���\���� HTTP ��k
              .AllowAnyHeader() // ���\������Y
              .WithExposedHeaders("content-disposition");
    });
});

// Ū��JWT�t�m
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Key"]);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = false,  //������ Issuer
        ValidateAudience = false,//������ Audience
        ValidateLifetime = false,//������ �\�i�ɶ�
        ValidateIssuerSigningKey = true,
        //ValidIssuer = jwtSettings["Issuer"],
        //ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

builder.Services.AddSingleton<JwtService>();

// ���U�A��: ���o����������|
builder.Services.AddSingleton<IWebHostEnvironmentService, WebHostEnvironmentService>();


// �]�w Serilog�A�N��x��X���ɮ�
Serilog.Log.Logger = new LoggerConfiguration()
    .WriteTo.Console() // �P�ɿ�X��D���x
    .WriteTo.Debug()    // �P�ɿ�X�찻��
    .WriteTo.File("Logs/app.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 90, shared: true) // �C�ѥͦ��s�ɮסA�åu�O�d 90 ��log
    .CreateLogger();

builder.Host.UseSerilog(); // �������ؤ�x�t��



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// use the middleware
app.UseMiddleware<VersionMiddleware>();

// CORS 必须在 UseHttpsRedirection 和 UseAuthentication 之前
app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
