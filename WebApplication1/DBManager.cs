
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

public class DBManager {
    private SqliteConnection? connection = null;

    private string HashPassword(string password) {
        using (var algorithm = SHA256.Create()) {
            var bytes_hash = algorithm.ComputeHash(Encoding.Unicode.GetBytes(password));
            return Convert.ToBase64String(bytes_hash);
        }
    }

    public bool ConnectToDB(string DB_PATH) {
        Console.WriteLine("Connection to db...");
        try {
            connection = new SqliteConnection("Data Source=" + DB_PATH);
            connection.Open();
            if (connection.State != System.Data.ConnectionState.Open) {
                Console.WriteLine("Failed");
                return false;
            } else {
                Console.WriteLine("Done!");
                return true;
            }
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public void Disconnect() {
        if (connection == null) 
            return;
        if (connection.State != System.Data.ConnectionState.Open)
            return;
        connection.Close();
        Console.WriteLine("Disconnect from db");
        connection = null; // Сбросить соединение
    }

    public bool CheckConnection() {
        return connection != null;
    }

    public bool AddUser(string login, string password) {
        if (!CheckConnection()) {
            Console.WriteLine("Failed");
            return false;
        }

        string request = "SELECT Login FROM users WHERE Login=@login";
        using (var command = new SqliteCommand(request, connection)) {
            command.Parameters.AddWithValue("@login", login);
            using (var reader = command.ExecuteReader()) {
                if (reader.HasRows) {
                    Console.WriteLine("Пользователь уже существует.");
                    return false; 
                }
            }
        }

        request = "INSERT INTO users (Login, Password) VALUES (@login, @password)";
        using (var command = new SqliteCommand(request, connection)) {
            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@password", HashPassword(password));
            int result = command.ExecuteNonQuery();
            return result == 1;
        }
    }

    public bool CheckUser(string login, string password) {
        if (connection == null) 
            return false;

        string request = "SELECT Login FROM users WHERE Login=@login AND Password=@password";
        using (var command = new SqliteCommand(request, connection)) {
            command.Parameters.AddWithValue("@login", login);
            command.Parameters.AddWithValue("@password", HashPassword(password));

            try {

                using (var reader = command.ExecuteReader()) {
                    return reader.HasRows;
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                return false;
            }
        }
    }
    public bool UserExists(string username) {
      if (!CheckConnection()) {
        Console.WriteLine("Нет соединения с базой данных.");
        return false;
      }
      string request = "SELECT Login FROM users WHERE Login=@username";
      using (var command = new SqliteCommand(request, connection)) {
        command.Parameters.AddWithValue("@username", username);
        using (var reader = command.ExecuteReader()) {
          return reader.HasRows;
        }
      }
    }
    public bool ChangePassword(string username, string newPassword) {
      if (!CheckConnection()) {
        Console.WriteLine("Нет соединения с базой данных.");
        return false;
      }
      string request = "UPDATE users SET Password=@newPassword WHERE Login=@username";
      using (var command = new SqliteCommand(request, connection)) {
        command.Parameters.AddWithValue("@newPassword", newPassword);
        command.Parameters.AddWithValue("@username", username);
        int rowsAffected = command.ExecuteNonQuery();
        return rowsAffected > 0;
      }
    }
    public bool RegisterUser(string login, string password) {
        bool added = AddUser(login, password);
        if (!added) {
            Console.WriteLine("Ошибка при добавлении нового пользователя");
        }
        return added;
    }
}
