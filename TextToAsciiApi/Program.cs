using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TextToAsciiApi.Models;
using TextToAsciiApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<AsciiGenerationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Policy1",
        policy =>
        {
            policy.WithOrigins("https://text-to-ascii-nine.vercel.app")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Policy1");

app.MapPost("/api/generate", async (
    [FromBody] GenerationRequest request,
    AsciiGenerationService asciiGenerationService,
    HttpContext httpContext) =>
{
    if (string.IsNullOrWhiteSpace(request.Value) || request.Value.Any(c => char.IsDigit(c)))
        return Results.BadRequest("Invalid input value. Input should not be empty or contain digits.");

    var response = await asciiGenerationService.GenerateArt(request);

    if (string.IsNullOrEmpty(response.Art))
        return Results.NotFound("Unable to generate ASCII art for the given input");

    return Results.Ok(response);
});

app.Run();
