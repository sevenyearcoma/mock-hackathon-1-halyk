using System.Text.Json.Serialization;
using ComplianceDoc.Api.Services.Implementations;
using ComplianceDoc.Api.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p =>
        p.WithOrigins("http://localhost:3000", "http://localhost:3001")
         .AllowAnyMethod()
         .AllowAnyHeader()));

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ComplianceDoc Copilot API", Version = "v1" });
});

builder.Services.AddSingleton<IComplianceCheckStore, InMemoryComplianceCheckStore>();
builder.Services.AddScoped<IRiskScoringService, RiskScoringService>();
builder.Services.AddScoped<IClientMessageService, ClientMessageService>();
builder.Services.AddScoped<IComplianceValidationService, ComplianceValidationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.MapControllers();
app.Run();
