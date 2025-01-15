using static System.Drawing.Image;
using System.IO;
using System.Drawing.Text;
using System.Security.Principal;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Reflection.Metadata.Ecma335;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using System.Data.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();

var app = builder.Build();
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
Directory.CreateDirectory(uploadDir);

app.UseAuthentication();
app.UseAuthorization();

DBManager db = new DBManager();

app.UseExceptionHandler("/error");

app.MapGet("/", () => "Hello User! Upload your image...");

app.MapPost("/upload", async (HttpRequest request) => {
    try 
    {
        var memoryStream = new MemoryStream();
        await request.Body.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        Image image = Image.Load<Rgba32>(memoryStream);

        var fileName = Path.Combine(uploadDir, Guid.NewGuid() + ".png");
        await image.SaveAsync(fileName);

        return Results.Ok(new {message = "Image uploaded", filename = fileName});
    }
    catch (Exception ex) {
        return Results.BadRequest("Wrong image: " + ex.Message);
    }
});

app.MapPost("/generate", async (HttpRequest request) => {
    try 
    {
        using var reader = new StreamReader(request.Body);
        var filename = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(filename))
            return Results.BadRequest("Filename cannot be empty");

        var filePath = Path.Combine(uploadDir, filename);
        if (!File.Exists(filePath))
            return Results.NotFound("File not found");
        
        Image<Rgba32> image = Image.Load<Rgba32>(filePath);
        string asciiArt = await ASCIIArtCreator.GenerateASCIIArt(image);
        return Results.Ok(new {art = asciiArt});
    }
    catch (Exception ex) 
    {
        return Results.BadRequest("Error generating ASCII art: " + ex.Message);
    }
});

app.MapPost("/delete", async (HttpRequest request) => {
    try 
    {
        using var reader = new StreamReader(request.Body);
        var filename = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(filename))
            return Results.BadRequest("Filename cannot be empty");

        var filePath = Path.Combine(uploadDir,filename);
        if (!File.Exists(filePath))
            return Results.NotFound("File not found");
        
        File.Delete(filePath);
        return Results.Ok("File deleted successfully");
    }
    catch (Exception ex) 
    {
        return Results.BadRequest("Error deleting file: " + ex.Message);
    }
});

app.MapPost("/login", async (string login, string password, HttpContext context) => {if (!db.CheckUser(login,password))
        return Results.Unauthorized();

        var claims = new List<Claim> {new Claim(ClaimTypes.Name, login)};
        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
        return Results.Ok();
});

const string DB_PATH = "/mnt/c/Users/Виталий/db.sqlite";
if(!db.ConnectToDB(DB_PATH)) {
    Console.WriteLine("Failed to connect to db" + DB_PATH);
    Console.WriteLine("Shutdown!");
    return;
}
app.MapPost("/test", (HttpContext context) => {
    return Results.Ok("Test successful");
});

app.Run();
db.Disconnect();

public class ASCIIArtCreator
{
    public static async Task<string> GenerateASCIIArt(Image<Rgba32> image) {
        const string chars = "@#S%?*+;:. ";
        StringBuilder asciiArt = new StringBuilder();

        if (image.Width < 1 || image.Height < 1)
            return "Image is too small to generate ASCII art.";

        for (int y = 0; y < image.Height; y += 2) 
        {
            for (int x = 0; x < image.Width; x++) 
            {
                var pixel = image[x,y];
                var brightness = (pixel.R + pixel.G + pixel.B) / 3.0;
                int index = (int)(brightness / 255 * (chars.Length - 1));
                asciiArt.Append(chars[index]);
            }
            asciiArt.AppendLine();
        }
        return asciiArt.ToString();
    }
    public static string DeleteFile(string filePath) 
    {
         if (string.IsNullOrEmpty(filePath))
        {
            return "Filename cannot be empty";
        }
        if (!File.Exists(filePath))
        {
            return "File not found";
        }
        File.Delete(filePath);
        return "File deleted successfully";
    }
}