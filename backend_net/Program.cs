using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/login", async (HttpContext context) =>
{
    var data = await JsonSerializer.DeserializeAsync<Login>(context.Request.Body);
    if (data == null) return Results.BadRequest("Datos inválidos");

    data.Fecha = DateTime.Now;

    var file = "logins.json";
    List<Login> logins = new();
    if (File.Exists(file))
        logins = JsonSerializer.Deserialize<List<Login>>(await File.ReadAllTextAsync(file)) ?? new();

    logins.Add(data);
    await File.WriteAllTextAsync(file, JsonSerializer.Serialize(logins, new JsonSerializerOptions { WriteIndented = true }));
    return Results.Ok(new { mensaje = "Login almacenado correctamente" });
});

app.MapGet("/logins", async (HttpContext context) =>
{
    var password = context.Request.Query["password"];
    if (password != "12345")
        return Results.Unauthorized();

    var file = "logins.json";
    if (!File.Exists(file))
        return Results.NotFound("No hay registros");

    var json = await File.ReadAllTextAsync(file);
    return Results.Content(json, "application/json");
});

app.Run();

record Login
{
    public string? Usuario { get; set; }
    public string? Contraseña { get; set; }
    public DateTime Fecha { get; set; }
}