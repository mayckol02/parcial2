using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

string connectionString = Environment.GetEnvironmentVariable("AzureStorageConnectionString") 
    ?? builder.Configuration.GetConnectionString("AzureStorageConnectionString") 
    ?? throw new Exception("No se encontró la cadena de conexión de Azure Storage.");

var containerName = "logins";
var blobName = "logins.json";

app.MapPost("/login", async (Login login) =>
{
    login.Fecha = DateTime.Now;

    var blobServiceClient = new BlobServiceClient(connectionString);
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    await containerClient.CreateIfNotExistsAsync();
    var blobClient = containerClient.GetBlobClient(blobName);

    List<Login> logins = new();
    if (await blobClient.ExistsAsync())
    {
        var download = await blobClient.DownloadContentAsync();
        logins = JsonSerializer.Deserialize<List<Login>>(download.Value.Content.ToString()) ?? new();
    }

    logins.Add(login);
    var json = JsonSerializer.Serialize(logins, new JsonSerializerOptions { WriteIndented = true });
    using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
    await blobClient.UploadAsync(ms, overwrite: true);

    return Results.Ok(new { mensaje = "Login almacenado correctamente en Azure Blob Storage" });
});

app.MapGet("/logins", async (HttpContext context) =>
{
    var password = context.Request.Query["password"];
    if (password != "12345")
        return Results.Unauthorized();

    var blobServiceClient = new BlobServiceClient(connectionString);
    var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
    var blobClient = containerClient.GetBlobClient(blobName);

    if (!await blobClient.ExistsAsync())
        return Results.NotFound("No hay registros.");

    var download = await blobClient.DownloadContentAsync();
    return Results.Content(download.Value.Content.ToString(), "application/json");
});

app.Run();

record Login
{
    public string? Usuario { get; set; }
    public string? Contraseña { get; set; }
    public DateTime Fecha { get; set; }
}