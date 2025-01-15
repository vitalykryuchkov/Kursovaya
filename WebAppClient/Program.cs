using System.Net.Http.Headers;
using System.Net.Http.Json;

HttpClient client  = new HttpClient();

Token? LoginOnServer(string? username, string? password) {
  if (username == null || username.Length == 0 || password == null || password.Length == 0) {
    return;
  }
  string request = "/login?login=" + username + "&password=" + password;
  var responce = client.PostAsync(request, null).Result;
  if (responce.IsSuccessStatusCode) {
    Console.WriteLine("Авторизация прошла успешно"); 
    return responce.Content.ReadFromJsonAsync<Token>().Result;
  }
  else {
    Console.WriteLine("Авторизация провалена");
    return null;
  }
}

string GetRandom() {
  string request = "/random";

  var responce = client.GetAsync(request).Result;
  if (responce.IsSuccessStatusCode) {
    return responce.Content.ReadAsStringAsync().Result;
  }
  else {
    return "данные недоступны";
  }
}

const string DEFAULT_SERVER_URL = "http://localhost:5097";
Console.WriteLine("Введите URL сервера (http://localhost:5097 по умолчанию): ");
string? serverUrl = Console.ReadLine();

if (serverUrl==null || serverUrl.Length == 0) 
{
  serverUrl = DEFAULT_SERVER_URL;
}
try {
  client.BaseAddress = new Uri(serverUrl);

  Console.WriteLine("АВТОРИЗАЦИЯ");
  Console.WriteLine("Логан: ");
  string? username = Console.ReadLine();
  Console.WriteLine("Пароль: ");
  string? password = Console.ReadLine();


  Token? token = LoginOnServer(username, password);
  if (token == null) {
    Console.WriteLine("Дальнейшее выполнение невозможно");
    return; //Наша программа должна возвращаться обратно к вводу лоигна и пароля короче половину переделывать 
  }
  client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value.access_token);
  Console.WriteLine(GetRandom());
}
catch (Exception ex){
  Console.WriteLine("Ошибка!" + ex.Message);
}

public struct Token {
  public required string access_token{get;set};
  public required string username{get;set};
}
