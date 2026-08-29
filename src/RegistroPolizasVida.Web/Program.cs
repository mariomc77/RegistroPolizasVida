using System.Text.Json.Serialization;
using RegistroPolizasVida.Application.BackgroundQueue;
using RegistroPolizasVida.Application.Common;
using RegistroPolizasVida.Application.Notifications;
using RegistroPolizasVida.Application.Parsing;
using RegistroPolizasVida.Application.Services;
using RegistroPolizasVida.Application.Validation;
using RegistroPolizasVida.Application.Zip;
using RegistroPolizasVida.DataAccess;
using RegistroPolizasVida.Web.BackgroundServices;
using RegistroPolizasVida.Web.Hubs;
using RegistroPolizasVida.Web.Logging;
using RegistroPolizasVida.Web.Notifications;

var builder = WebApplication.CreateBuilder(args);

// --- MVC + API --------------------------------------------------------------
builder.Services.AddControllersWithViews()
    .AddJsonOptions(opciones =>
    {
        // Las entidades de seguimiento (LoteCarga -> ArchivoLote -> ErrorProcesamiento,
        // cada una con su referencia de "vuelta" al padre) forman un grafo con ciclos;
        // en vez de mantener DTOs paralelos para cada respuesta, se le pide al
        // serializador que simplemente no vuelva a bajar por una referencia ya vista.
        opciones.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        opciones.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

        opciones.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddSignalR();

// --- Capa de acceso a datos (SQL Server vía EF Core) -------------------------
builder.Services.AgregarAccesoDatos(builder.Configuration);

// --- Capa de aplicación: validadores, parser, orquestador del pipeline -------
builder.Services.AddSingleton<IZipBatchExtractor, ZipBatchExtractor>();
builder.Services.AddSingleton<IXsdBatchValidator, XsdBatchValidator>();     // el esquema se compila una sola vez
builder.Services.AddSingleton<IPolizaXmlParser, PolizaXmlParser>();
builder.Services.AddSingleton<IPolizaBusinessRuleValidator, PolizaBusinessRuleValidator>();
builder.Services.AddScoped<IBatchProcessingService, BatchProcessingService>();
builder.Services.AddScoped<ILoteCargaAppService, LoteCargaAppService>();

// Adaptadores hacia infraestructura concreta (SignalR, Microsoft.Extensions.Logging).
builder.Services.AddScoped<IProcesamientoNotifier, SignalRProcesamientoNotifier>();
builder.Services.AddSingleton(typeof(IPipelineLogger<>), typeof(MicrosoftPipelineLogger<>));

// --- Cola de tareas en segundo plano (productor/consumidor) ------------------
builder.Services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
builder.Services.AddHostedService<QueuedHostedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers();

app.MapHub<ProcesamientoHub>("/hubs/procesamiento");

app.Run();