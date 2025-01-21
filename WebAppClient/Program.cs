using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using System.Net;

HttpClient client  = new HttpClient();

async Task LoginOnServer(string? username, string? password, HttpClient client) {
  if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) {
    return;
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
  var response = await client.PostAsync("/register", content);
  if (response.IsSuccessStatusCode) {
    Console.WriteLine("Регистрация прошла успешно");
    return true; 
  } else {
    Console.WriteLine("Регистрация провалена: " + await response.Content.ReadAsStringAsync());
    return false;
  }
}

void HistoryofUser(){
    string request = "/history";

    var responce = client.GetAsync(request).Result;
    if (responce.IsSuccessStatusCode){
        var content = responce.Content.ReadAsStringAsync().Result;
        content = content.Substring(1, content.Length-2);
        int count = 0;
        for (int i = 0; i < content.Length; i++){
            if (content[i] == ',')
                count++;
        }
        string[] input = new string[count];
        input = content.Split(',');
        System.Console.WriteLine("History of your requests:");
        for (int j = 0; j < input.Length; j++)
            Console.WriteLine($"{j+1}.   {input[j]}");
        Console.WriteLine();
    }
    else{
        Console.WriteLine(responce.Content.ReadAsStringAsync().Result);
    }
}

void ClearHistory(){
    string request = "/clear_history";

    var responce = client.DeleteAsync(request).Result;
    if (responce.IsSuccessStatusCode){
        Console.WriteLine(responce.Content.ReadAsStringAsync().Result);
        Console.WriteLine();
    }
    else{
        Console.WriteLine(responce.Content.ReadAsStringAsync().Result);
        Console.WriteLine();
    }
}

async Task UploadImage(string filePath) {
  var content = new MultipartFormDataContent();
  var fileStream = new FileStream(filePath, FileMode.Open);
  content.Add(new StreamContent(fileStream),"file", Path.GetFileName(filePath));

  var response = await client.PostAsync("/upload", content);
  if (response.IsSuccessStatusCode) {
    Console.WriteLine("Изображение загружено: " + await response.Content.ReadAsStringAsync());
  }
}
async Task GenerateASCIIArt(string filename) {
  var content = new StringContent(filename, Encoding.UTF8, "application/json");
  var response = await client.PostAsync("/generate", content);
  if (response.IsSuccessStatusCode) {
    var result = await response.Content.ReadAsStringAsync();
    Console.WriteLine("ASCII Art: " + result);
  }
  else {
    Console.WriteLine("Ошибка генерации ASCII Art: " + await response.Content.ReadAsStringAsync());
  }
}
async Task DeleteFile(string filename) {
    var content = new StringContent(filename, Encoding.UTF8, "application/json");
    var response = await client.PostAsync("/delete", content);
    if (response.IsSuccessStatusCode) {
        Console.WriteLine("Файл успешно удален: " + await response.Content.ReadAsStringAsync());
    } else {
        Console.WriteLine("Ошибка удаления файла: " + await response.Content.ReadAsStringAsync());
    }
}

string GetRandom(HttpClient client) {
  string request = "/random";
  var response = client.GetAsync(request).Result;
  if (response.IsSuccessStatusCode) {
    var jsonResponse = response.Content.ReadAsStringAsync().Result;
    var artResponse = JsonSerializer.Deserialize<ArtResponse>(jsonResponse);
    return artResponse?.art?.Replace("\\n","\n")?.Trim() ?? "Данные недоступны";
  }
  else {
    return "Данные недоступны";
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
    while (true) {
        Console.WriteLine("АВТОРИЗАЦИЯ");
        Console.WriteLine("Логин: ");
        string? username = Console.ReadLine();
        Console.WriteLine("Пароль: ");
        string? password = Console.ReadLine();
        
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler {
            CookieContainer = cookieContainer
        };
        var clientWithCookies = new HttpClient(handler) {
            BaseAddress = new Uri(serverUrl)
        };

        await LoginOnServer(username, password, clientWithCookies);
        
        if (cookieContainer.Count > 0) {
            Console.WriteLine(GetRandom(clientWithCookies));
            break;
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
        bool exit = false;
        while (!exit) {
          Console.WriteLine("Выберите действие: \n 1 - Загрузить изображение \n 2 - Сгенерировать ASCII Art \n 3 - Удалить файл \n 4 - История запросов \n 5 - Очистить историю запросов \n 0 - Выход");
          string? choice = Console.ReadLine();

          switch (choice) {
            case "1":
                Console.Write("Введите путь к изображению для загрузки: ");
                string uploadPath = "/home/vitaly/WebApplication1/imag.png";
                await UploadImage(uploadPath);
                break;
                
            case "2":
                Console.Write("Введите имя файла для генерации ASCII Art: ");
                string generateFilename = "imag.png";
                await GenerateASCIIArt(generateFilename);
                break;

            case "3":
                Console.Write("Введите имя файла для удаления: ");
                string deleteFilename = "imag.png";
                await DeleteFile(deleteFilename);
                break;

            case "4":
              HistoryofUser();
              break;

            case "5":
              ClearHistory();
              break;

            case "0":
                exit = true;
                Console.WriteLine("Выход из программы.");
                return;

            default:
                Console.WriteLine("Неверный выбор, попробуйте снова.");
                break;
        }
    }
  }
} catch (Exception ex) {
    Console.WriteLine("Ошибка!" + ex.Message);
}
public class ArtResponse {
  public required string art { get; set; }
}
