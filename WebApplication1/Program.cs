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
using System.Collections.Concurrent;
using System.Data;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();

var app = builder.Build();
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
Directory.CreateDirectory(uploadDir);

var requestHistory = new ConcurrentBag<string>();

app.UseAuthentication();
app.UseAuthorization();

DBManager db = new DBManager();

app.UseExceptionHandler("/error");

app.MapGet("/", () => "Здравствуй, пользователь, загрузи изображение...");

const string DB_PATH = "/home/vitaly/db.sqlite";

void LogRequest(string endpoint) {
    requestHistory.Add($"Request to {endpoint} at {DateTime.UtcNow}");
}

app.MapPost("/upload", async (HttpRequest request) =>
{
    Console.WriteLine("Получен запрос на загрузку файла...");

    if (!request.HasFormContentType)
    {
        Console.WriteLine("Неверный тип содержимого.");
        return Results.BadRequest("Неверный тип содержимого.");
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.FirstOrDefault();
    if (file == null || file.Length == 0)
    {
        Console.WriteLine("Файл не найден в запросе или он пустой.");
        return Results.BadRequest("Файл не найден в запросе.");
    }

    Console.WriteLine($"Получен файл: {file.FileName}, размер: {file.Length} байт.");

    using var memoryStream = new MemoryStream();
    await file.CopyToAsync(memoryStream);
    memoryStream.Seek(0, SeekOrigin.Begin);

    Image image;
    try
    {
        image = Image.Load<Rgba32>(memoryStream);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Ошибка загрузки изображения: " + ex.Message);
        return Results.BadRequest("Ошибка загрузки изображения: " + ex.Message);
    }

    var fileName = Path.Combine(uploadDir, Guid.NewGuid() + ".png");
    await image.SaveAsync(fileName);

    Console.WriteLine($"Изображение сохранено как: {fileName}");

    return Results.Ok(new { message = "Изображение загружено", filename = fileName });
});

app.MapPost("/generate", async (HttpRequest request) => {
    LogRequest("/generate");
    try 
    {
        using var reader = new StreamReader(request.Body);
        var filename = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(filename))
            return Results.BadRequest("Название файла не может быть пустым");

        var filePath = Path.Combine(uploadDir, filename);
        if (!File.Exists(filePath))
            return Results.NotFound("Файл не найден");
        
        Image<Rgba32> image = Image.Load<Rgba32>(filePath);
        string asciiArt = await ASCIIArtCreator.GenerateASCIIArt(image);
        return Results.Ok(new {art = asciiArt});
    }
    catch (Exception ex) 
    {
        return Results.BadRequest("Ошибка генерации ASCII изображения: " + ex.Message);
    }
});

app.MapPost("/delete", async (HttpRequest request) => {
    LogRequest("/delete");
    try 
    {
        using var reader = new StreamReader(request.Body);
        var filename = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(filename))
            return Results.BadRequest("Название файла не может быть пустым");

        var filePath = Path.Combine(uploadDir,filename);
        if (!File.Exists(filePath))
            return Results.NotFound("Файл не найден");
        
        File.Delete(filePath);
        return Results.Ok("Файл успешно удален");
    }
    catch (Exception ex) 
    {
        return Results.BadRequest("Ошибка удаления файла: " + ex.Message);
    }
});

app.MapPost("/login", async (string login, string password, HttpContext context) => {
    if (!db.CheckUser(login, password))
        return Results.Unauthorized();

    LogRequest("/login");
    var claims = new List<Claim> { new Claim(ClaimTypes.Name, login) };
    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
    
    return Results.Ok();
});

app.MapPost("/send_cookie", [Authorize] async (HttpRequest request, HttpContext context) => {
    LogRequest("/send_cookie");
    try {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        Console.WriteLine($"Received contact data: {body}");
        return Results.Ok(new {messege = "Успешная отправка"});
    }
    catch (Exception ex) {
        return Results.BadRequest("Ошибка отправки: " + ex.Message);
    }
});

app.MapPost("/register", async (HttpRequest request) => {
    LogRequest("/register");
    try {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        Console.WriteLine($"Received body: {body}");
        var userData = JsonSerializer.Deserialize<UserRegistration>(body);

        if (userData == null || string.IsNullOrWhiteSpace(userData.Username) || string.IsNullOrWhiteSpace(userData.Password)) {
            return Results.BadRequest("Имя пользователя и пароль не могут быть пустыми");
        }
        if (db.UserExists(userData.Username)) {
            return Results.BadRequest("Такой пользователь уже существует");
        }

        bool success = db.RegisterUser(userData.Username, userData.Password);
        if (!success) {
            return Results.BadRequest("Регистрация провалена");
        }
        return Results.Ok("Пользователь успешно зарегистрирован");
    }
    catch (Exception ex) {
        Console.WriteLine("Ошибка регистрации: " + ex.Message);
        return Results.Problem("Ошибка регистрации: " + ex.Message);
    }
});

app.MapPost("/change_password", async (HttpRequest request) => { 
    LogRequest("/change_password");
    try {
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var changePasswordRequest = JsonSerializer.Deserialize<ChangePasswordRequest>(body);

        if (changePasswordRequest == null || 
            string.IsNullOrWhiteSpace(changePasswordRequest.newPassword) || 
            string.IsNullOrWhiteSpace(changePasswordRequest.Login) || 
            string.IsNullOrWhiteSpace(changePasswordRequest.oldPassword)) 
        {
            return Results.BadRequest("Все поля должны быть заполнены.");
        }

        var db = new DBManager();
        if (!db.ConnectToDB(DB_PATH)) {
            return Results.Problem("Не удалось подключиться к базе данных.", statusCode: 500);
        }

        if (!db.CheckUser(changePasswordRequest.Login, changePasswordRequest.oldPassword)) {
            return Results.BadRequest("Неверное имя пользователя или старый пароль.");
        }

        bool success = db.ChangePassword(changePasswordRequest.Login, db.HashPassword(changePasswordRequest.newPassword));
        db.Disconnect();

        if (success) {
            return Results.Ok("Пароль успешно изменен");
        } else {
            return Results.BadRequest("Не удалось изменить пароль");
        }
    } catch (Exception ex) {
        return Results.BadRequest("Ошибка изменения пароля: " + ex.Message);
    }
});

app.MapGet("/clear_history", async (HttpRequest request) => {
    await Task.Run(() => requestHistory.Clear());
    LogRequest("/clear_history");
    return Results.Ok("История пуста");
});


if(!db.ConnectToDB(DB_PATH)) {
    Console.WriteLine("Ошибка подключения в базе данных: " + DB_PATH);
    Console.WriteLine("Выключение!");
    return;
}
app.MapPost("/test", (HttpContext context) => {
    return Results.Ok("Тест успешен");
});

app.MapGet("/history", [Authorize] () => Results.Ok(requestHistory));

app.Run();
db.Disconnect();

public class ASCIIArtCreator
{
    public static Task<string> GenerateASCIIArt(Image<Rgba32> image) {
        const string chars = "@#S%?*+;:. ";
        StringBuilder asciiArt = new StringBuilder();

        if (image.Width <= 1 || image.Height <= 1)
            return Task.FromResult("Изображение слишком мало для генерации ASCII art.");

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
        return Task.FromResult(asciiArt.ToString());
    }
    public static string DeleteFile(string filePath) 
    {
         if (string.IsNullOrEmpty(filePath))
        {
            return "Имя файла не может быть пустым";
        }
        if (!File.Exists(filePath))
        {
            return "Файл не найден";
        }
        File.Delete(filePath);
        return "Файл успешно удален";
    }
}

public class ChangePasswordRequest {
    public required string Login { get; set; }
    public required string newPassword { get; set; }
    public required string oldPassword { get; set; }
}

public class UserRegistration {
    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }
}