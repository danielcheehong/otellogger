using Serilog;
using Serilog.Sinks.Splunk;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;


var builder = WebApplication.CreateBuilder(args);

// Setup EF Core with SQL Server and Oracle

// Add Serilog for logging
// Enrich the logs with TraceId, SpanId, and ParentId from the current activity.
// This is useful for correlating logs with distributed tracing systems like OpenTelemetry.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("TraceId", () => System.Diagnostics.Activity.Current?.TraceId.ToString())
    .Enrich.WithProperty("SpanId", () => System.Diagnostics.Activity.Current?.SpanId.ToString())
    .Enrich.WithProperty("ParentId", () => System.Diagnostics.Activity.Current?.ParentId)
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.EventCollector(
        splunkHost: "https://localhost:8088",
        eventCollectorToken: "test-hec-token",
        sourceType: "Banking-Doc-Audit-Logs",
        messageHandler: new HttpClientHandler 
        {
            ServerCertificateCustomValidationCallback = (msg, cert, chain, errors) => true
        })
    .CreateLogger();

builder.Host.UseSerilog();

// Add OTEL instrumentation support.


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var docClassifications = new[]
{
    "Internal", "Strictly Confidential", "Public", "Classified", "PII Restricted"
}; 
 
app.MapGet("/AuditDocumentLog", () =>
{
    var auditLogs =  Enumerable.Range(0,4).Select(index =>
        
        new AuditLog
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            "John Doe",
            $"Document-{index}",
            "PDF",
            docClassifications[Random.Shared.Next(docClassifications.Length)]
        ))
        .ToArray();
        
        Log.Information("List latest audited downloaded CID Document: {AuditLog}", auditLogs);

    return auditLogs;
})
.WithName("GetDcomumentLog");

app.MapPost("/AuditDocumentLog", () =>
{   
    Log.Information("Posted a request for document log: {AuditLog}");
    return HttpStatusCode.OK;
})
.WithName("AddDcomumentLog");

app.Run();

// Create a record to represent a audit log for downloaded document. Include the date, person downloaded the document, document name, document type, and the summary of the document..


record AuditLog(DateOnly Date, string Person, string DocumentName, string DocumentType, string Summary);
