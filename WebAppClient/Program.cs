using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Net;
using SixLabors.ImageSharp;


var handler = new HttpClientHandler { CookieContainer = new CookieContainer() };
HttpClient client = new HttpClient(handler);

List<string> requestHistory = new List<string>();

void DisplayRequestHistory() {
  Console.WriteLine("История запросов: ");
  if (requestHistory.Count == 0) {
    Console.WriteLine("Нет запросов.");
    Console.WriteLine();
  }
  else {
    foreach (var entry in requestHistory) {
      Console.WriteLine(entry);
    }
  }
}

async Task LoginOnServer(string? username, string? password, HttpClient client) {
  if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
    Console.WriteLine("Логин и/или пароль не могут быть пустыми");
    return ;
  }
  string request = "/login?login=" + username + "&password=" + password;
  var response = await client.PostAsync(request, null);
  if (response.IsSuccessStatusCode) {
    Console.WriteLine("Авторизация прошла успешно"); 
    var cookies = response.Headers.GetValues("Set-Cookie");
    foreach (var cookie in cookies) {
      client.DefaultRequestHeaders.Add("Cookie", cookie);
    }
    Console.WriteLine("Куки установлены");
  }
  else {
    Console.WriteLine("Авторизация провалена" + await response.Content.ReadAsStringAsync());
  }
}

async Task<bool> RegistrationOnServer(string username, string password) {
  if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
    return false;
  }
  var content = new StringContent(JsonSerializer.Serialize(new { username, password }), Encoding.UTF8, "application/json");
  try {
    var response = await client.PostAsync("/register", content);
      if (response.IsSuccessStatusCode) {
        Console.WriteLine("Регистрация прошла успешно");
        return true; 
      } 
      else
      {
        Console.WriteLine("Регистрация провалена: " + await response.Content.ReadAsStringAsync());
        return false;
      }
  }
  catch (HttpRequestException ex) 
  {
    Console.WriteLine($"Ошибка при выполнении запроса: {ex.Message}");
    return false;
  }
}

async Task ClearHistory() {
    string request = "/clear_history";
    var response = await client.DeleteAsync(request);
    requestHistory.Clear();
    if (response.IsSuccessStatusCode) {
        Console.WriteLine(await response.Content.ReadAsStringAsync());
        requestHistory.Add("История запросов очищена на сервере");
    } else {
        Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}

async Task ChangePassword(string username)
{
    Console.Write("Введите текущий пароль: ");
    string? oldPassword = Console.ReadLine();
    Console.Write("Введите новый пароль: ");
    string? newPassword = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(newPassword))
    {
        Console.WriteLine("Новый пароль не может быть пустым.");
        return;
    }

    if (oldPassword == newPassword) {
        Console.WriteLine("Новый пароль должен отличаться от старого.");
        return;
    }

    var changePasswordRequest = new { Login = username, newPassword = newPassword, oldPassword = oldPassword };
    var content = new StringContent(JsonSerializer.Serialize(changePasswordRequest), Encoding.UTF8, "application/json");

    var response = await client.PostAsync("/change_password", content);
    if (response.IsSuccessStatusCode)
    {
        Console.WriteLine("Пароль успешно изменен.");
    }
    else
    {
        Console.WriteLine("Ошибка смены пароля: " + await response.Content.ReadAsStringAsync());
    }
}

async Task UploadImage(string path)
{
    try
    {
        if (string.IsNullOrEmpty(path))
        {
            Console.WriteLine("Ошибка! Путь не может быть пустым.");
            return;
        }

        using var client = new HttpClient();
        using var content = new MultipartFormDataContent();
        await using var stream = File.OpenRead(path);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", Path.GetFileName(path));

        Console.WriteLine($"Отправка файла {path} на сервер...");

        var response = await client.PostAsync("http://localhost:5097/upload", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        requestHistory.Add($"Загрузил изображение: {path} - Отве: {responseContent}");
        Console.WriteLine($"Ответ сервера: {responseContent}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ошибка при загрузке изображения: {ex.Message}");
    }
}

async Task GenerateASCIIArt(string filename) {
  var content = new StringContent(filename, Encoding.UTF8, "application/json");
  var response = await client.PostAsync("/generate", content);
  if (response.IsSuccessStatusCode)
    {
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var artResponse = JsonSerializer.Deserialize<ArtResponse>(jsonResponse);

        if (artResponse != null)
        {
            string formattedArt = artResponse.art.Replace("\\n", "\n");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine("Сгенерированный ASCII Art:");
            Console.WriteLine(new string('-', 50));
            Console.WriteLine(formattedArt);
            Console.WriteLine(new string('-', 50));
        }
        else
        {
            Console.WriteLine("Ошибка парсинга ответа.");
        }

        requestHistory.Add($"Сгенерировал ASCII Art для файла: {filename}");
    }
    else
    {
        Console.WriteLine("Ошибка генерации ASCII Art: " + await response.Content.ReadAsStringAsync());
    }
}

async Task DeleteFile(string filename) {
    var content = new StringContent(filename, Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/delete", content);
    if (response.IsSuccessStatusCode) {
        requestHistory.Add($"Удалил файл: {filename}");
        Console.WriteLine("Файл успешно удален: " + await response.Content.ReadAsStringAsync());
    } else {
        Console.WriteLine("Ошибка удаления файла: " + await response.Content.ReadAsStringAsync());
    }
}

const string DEFAULT_SERVER_URL = "http://localhost:5097";
Console.WriteLine("Введите URL сервера (http://localhost:5097 по умолчанию): ");
string? serverUrl = Console.ReadLine();

if (string.IsNullOrWhiteSpace(serverUrl)) 
{
    serverUrl = DEFAULT_SERVER_URL;
}
try {
    client.BaseAddress = new Uri(serverUrl);
    bool isAuthenticated = false;

    string? username = null;

    while (!isAuthenticated) {
        Console.WriteLine("АВТОРИЗАЦИЯ");
        Console.WriteLine("Логин: ");
        string? inputUsername = Console.ReadLine();
        Console.WriteLine("Пароль: ");
        string? password = Console.ReadLine();
        
        var cookieContainer = new CookieContainer();
        var clientHandler = new HttpClientHandler {
            CookieContainer = cookieContainer
        };
        var clientWithCookies = new HttpClient(clientHandler) {
            BaseAddress = new Uri(serverUrl)
        };

        await LoginOnServer(inputUsername, password, clientWithCookies);
        
        if (cookieContainer.Count > 0) {
            isAuthenticated = true;
            username = inputUsername;
        } else {
            Console.WriteLine("Попробуйте снова.");
            Console.WriteLine("Хотите зарегистрироваться? (да/нет)");
            string? registerResponse = Console.ReadLine();
            if (registerResponse?.ToLower() == "да") {
                Console.Write("Введите логин для регистрации: ");
                string? regUsername = Console.ReadLine();
                Console.Write("Введите пароль для регистрации: ");
                string? regPassword = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(regUsername) && !string.IsNullOrWhiteSpace(regPassword)) {
                    await RegistrationOnServer(regUsername, regPassword);
                } else {
                    Console.WriteLine("Логин и пароль не могут быть пустыми");
                }
            }
        }
    }

   bool exit = false;
    while (!exit) {
        Console.WriteLine("Выберите действие: \n 1 - Загрузить изображение \n 2 - Сгенерировать ASCII Art \n 3 - Удалить файл \n 4 - История запросов \n 5 - Очистить историю запросов \n 6 - Сменить пароль \n 0 - Выход");
        string? choice = Console.ReadLine();

        switch (choice) {
            case "1":
                Console.Write("Введите путь к изображению для загрузки: ");
                string uploadPath = Console.ReadLine() ?? "/home/vitaly/WebApplication1/imag.png";
                await UploadImage(uploadPath);
                break;
                
            case "2":
                Console.Write("Введите имя файла для генерации ASCII Art: ");
                string generateFilename = Console.ReadLine() ?? "imag.png";
                await GenerateASCIIArt(generateFilename);
                break;

            case "3":
                Console.Write("Введите имя файла для удаления: ");
                string deleteFilename = Console.ReadLine() ?? "imag.png";
                await DeleteFile(deleteFilename);
                break;

            case "4":
                DisplayRequestHistory();
                break;

            case "5":
                await ClearHistory();
                Console.WriteLine("История запросов очищена.");
                break;
            
            case "6":
                if (username != null) {
                  await ChangePassword(username);
                }
                else {
                  Console.WriteLine("Ошибка: пользователь не авторизован.");
                }
                break;

            case "0":
                exit = true;
                Console.WriteLine("Выход из программы.");
                break;

            default:
                Console.WriteLine("Неверный выбор, попробуйте снова.");
                break;
        }
    }
} catch (Exception ex) {
    Console.WriteLine("Ошибка!" + ex.Message);
}
public class ArtResponse {
  public required string art { get; set; }
}