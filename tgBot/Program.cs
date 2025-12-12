using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Microsoft.Data.Sqlite;

namespace tgBot
{
    internal class Program
    {
        public static List<long> current_users = new List<long>();
        public static List<bool> current_logins = new List<bool>();
        public static List<bool> isLogging = new List<bool>();
        public static List<bool> isPassword = new List<bool>();
        public static List<bool> isCheckingChildren = new List<bool>();
        public static List<string> curLogins = new List<string>();
        public static List<bool> isEvent = new List<bool>();

        private static string[] logins = { "10M", "9L1" };
        private static string[] passwords = { "123", "456" };

        private static TelegramBotClient client;
        private static ReceiverOptions receiver;
        private static readonly string token = "";
        private static readonly string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "gymnasia.db");
        private static readonly long adminChatId = your_id;

        static async Task Main()
        {
            InitializeDatabase();
            LoadUsersFromDb();

            client = new TelegramBotClient(token);
            receiver = new ReceiverOptions
            {
                AllowedUpdates = { UpdateType.Message },
                ThrowPendingUpdates = true
            };

            var cts = new CancellationTokenSource();
            client.StartReceiving(UpdateHandler, ExceptionHandler, receiver, cts.Token);

            var me = await client.GetMeAsync();
            Console.WriteLine($"Bot is active: {me.FirstName}");

            await Task.Delay(-1);
        }

        private static async Task ExceptionHandler(ITelegramBotClient _, Exception exception, CancellationToken __)
        {
            Console.WriteLine($"Exception: {exception.Message}");
        }

        private static async Task UpdateHandler(ITelegramBotClient botClient, Update update, CancellationToken _)
        {
            if (update.Message is not { } message) return;
            if (message.From is not { } user) return;
            if (message.Chat is not { } chat) return;

            AddUserToMemory(chat.Id);
            int userIndex = current_users.IndexOf(chat.Id);
            if (userIndex == -1) return;

            if (message.Type != MessageType.Text) return;
            Console.WriteLine($"{user.FirstName} {user.Id} написал: {message.Text} в: {chat.Id}");

            if (message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(chat.Id, "Я бот Гимназии Один Девять\nНажмите:\n/functional — функционал\n/info — описание");
                return;
            }

            if (message.Text == "/exit")
            {
                ResetUserState(userIndex);
                await botClient.SendTextMessageAsync(chat.Id, "Вы вышли.");
                return;
            }

            if (message.Text == "/events")
            {
                await ShowEvents(chat, botClient);
                isEvent[userIndex] = false;
                return;
            }

            if (message.Text == "/subscribe")
            {
                await botClient.SendTextMessageAsync(chat.Id, "Напишите номер события и ваше имя в формате:\n1:Алексей Иванов-10M\nЕсли нет списка — /events");
                isEvent[userIndex] = true;
                return;
            }

            if (isEvent[userIndex])
            {
                await SubscribeToEvent(chat, message, botClient, userIndex);
                return;
            }

            if (message.Text == "/login" || isLogging[userIndex])
            {
                if (current_logins[userIndex])
                {
                    await botClient.SendTextMessageAsync(chat.Id, "Вы уже вошли.");
                    return;
                }
                if (isLogging[userIndex])
                {
                    await HandleLoginStep(chat, message, botClient, userIndex);
                }
                else
                {
                    isLogging[userIndex] = true;
                    await botClient.SendTextMessageAsync(chat.Id, "Введите логин (например, 10M):");
                }
                return;
            }

            if (isPassword[userIndex])
            {
                await HandlePasswordStep(chat, message, botClient, userIndex);
                return;
            }

            if (message.Text == "/childrencheck")
            {
                if (!current_logins[userIndex])
                {
                    await botClient.SendTextMessageAsync(chat.Id, "Сначала авторизуйтесь (/login).");
                    return;
                }

                var children = GetChildrenForClass(curLogins[userIndex]);
                if (children.Count == 0)
                {
                    await botClient.SendTextMessageAsync(chat.Id, "Нет учеников в этом классе.");
                    return;
                }

                await botClient.SendTextMessageAsync(chat.Id, string.Join("\n", children));
                await botClient.SendTextMessageAsync(chat.Id, "------------------\nУкажите отсутствующих (через запятую):");
                isCheckingChildren[userIndex] = true;
                return;
            }

            if (isCheckingChildren[userIndex])
            {
                await SaveAbsences(chat, message, userIndex);
                return;
            }

            if (message.Text == "/functional")
            {
                if (current_logins[userIndex])
                {
                    await botClient.SendTextMessageAsync(chat.Id, "Доступные команды:\n/childrencheck — отметить отсутствующих\n/events — посмотреть события\n/subscribe — подписаться на событие");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chat.Id, "Сначала войдите через /login");
                }
                return;
            }

            if (message.Text == "/info")
            {
                await botClient.SendTextMessageAsync(chat.Id, "Бот для учителей гимназии №19. Позволяет отмечать отсутствующих и управлять событиями.");
                return;
            }

            if (message.Text == "/adminview")
            {
                if (chat.Id != adminChatId)
                {
                    await botClient.SendTextMessageAsync(chat.Id, "Доступ запрещён.");
                    return;
                }
                await ShowAllClassesAndStudents(chat, botClient);
                return;
            }
        }

        private static void InitializeDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    ChatId INTEGER PRIMARY KEY,
                    IsLoggedIn INTEGER,
                    CurrentLogin TEXT
                );
                CREATE TABLE IF NOT EXISTS Classes (
                    ClassName TEXT PRIMARY KEY
                );
                CREATE TABLE IF NOT EXISTS Students (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ClassName TEXT,
                    FullName TEXT,
                    FOREIGN KEY (ClassName) REFERENCES Classes(ClassName)
                );
                CREATE TABLE IF NOT EXISTS Attendance (
                    Date TEXT,
                    ClassName TEXT,
                    AbsentStudents TEXT,
                    PRIMARY KEY (Date, ClassName)
                );
                CREATE TABLE IF NOT EXISTS Events (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT,
                    Date TEXT,
                    Description TEXT
                );
                CREATE TABLE IF NOT EXISTS EventSubscriptions (
                    EventId INTEGER,
                    ClassName TEXT,
                    StudentName TEXT,
                    PRIMARY KEY (EventId, ClassName, StudentName)
                );
            ";
            cmd.ExecuteNonQuery();

            SeedInitialData(connection);
        }

        private static void SeedInitialData(SqliteConnection connection)
        {
            using var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Classes";
            var count = (long)checkCmd.ExecuteScalar();
            if (count > 0) return;

            foreach (var login in logins)
            {
                using var classCmd = connection.CreateCommand();
                classCmd.CommandText = "INSERT INTO Classes (ClassName) VALUES (@class)";
                classCmd.Parameters.AddWithValue("@class", login);
                classCmd.ExecuteNonQuery();

                for (int i = 1; i <= 5; i++)
                {
                    using var studentCmd = connection.CreateCommand();
                    studentCmd.CommandText = "INSERT INTO Students (ClassName, FullName) VALUES (@class, @name)";
                    studentCmd.Parameters.AddWithValue("@class", login);
                    studentCmd.Parameters.AddWithValue("@name", $"{login}_Student{i}");
                    studentCmd.ExecuteNonQuery();
                }
            }
        }

        private static void LoadUsersFromDb()
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ChatId, IsLoggedIn, CurrentLogin FROM Users";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var chatId = reader.GetInt64("ChatId");
                var isLoggedIn = reader.GetBoolean("IsLoggedIn");
                var currentLogin = reader.IsDBNull("CurrentLogin") ? "" : reader.GetString("CurrentLogin");

                current_users.Add(chatId);
                current_logins.Add(isLoggedIn);
                isLogging.Add(false);
                isPassword.Add(false);
                isCheckingChildren.Add(false);
                isEvent.Add(false);
                curLogins.Add(currentLogin);
            }
        }

        private static void AddUserToMemory(long chatId)
        {
            if (current_users.Contains(chatId)) return;

            current_users.Add(chatId);
            current_logins.Add(false);
            isLogging.Add(false);
            isPassword.Add(false);
            isCheckingChildren.Add(false);
            isEvent.Add(false);
            curLogins.Add("");

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT OR IGNORE INTO Users (ChatId, IsLoggedIn, CurrentLogin) VALUES (@chatId, 0, NULL)";
            cmd.Parameters.AddWithValue("@chatId", chatId);
            cmd.ExecuteNonQuery();
        }

        private static void ResetUserState(int index)
        {
            current_logins[index] = false;
            isLogging[index] = false;
            isPassword[index] = false;
            isCheckingChildren[index] = false;
            isEvent[index] = false;
            curLogins[index] = "";

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Users SET IsLoggedIn = 0, CurrentLogin = NULL WHERE ChatId = @chatId";
            cmd.Parameters.AddWithValue("@chatId", current_users[index]);
            cmd.ExecuteNonQuery();
        }

        private static List<string> GetChildrenForClass(string className)
        {
            var children = new List<string>();
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT FullName FROM Students WHERE ClassName = @class";
            cmd.Parameters.AddWithValue("@class", className);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                children.Add(reader.GetString("FullName"));
            }
            return children;
        }

        private static async Task ShowEvents(Chat chat, ITelegramBotClient client)
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Date, Description FROM Events ORDER BY Id";
            using var reader = cmd.ExecuteReader();
            var events = new List<(int id, string name, string date, string desc)>();
            while (reader.Read())
            {
                events.Add((
                    reader.GetInt32("Id"),
                    reader.GetString("Name"),
                    reader.GetString("Date"),
                    reader.GetString("Description")
                ));
            }

            if (events.Count == 0)
            {
                await client.SendTextMessageAsync(chat.Id, "Нет запланированных событий.");
                return;
            }

            foreach (var (id, name, date, desc) in events)
            {
                await client.SendTextMessageAsync(chat.Id, $"{id}: {name}\n{desc}\nДата: {date}");
            }
        }

        private static async Task SubscribeToEvent(Chat chat, Message message, ITelegramBotClient client, int userIndex)
        {
            try
            {
                var input = message.Text;
                var parts = input.Split(':', 2);
                if (parts.Length != 2) throw new Exception();

                var eventIdStr = parts[0].Trim();
                var rest = parts[1].Trim();
                var nameClass = rest.Split('-', 2);
                if (nameClass.Length != 2) throw new Exception();

                var studentName = nameClass[0].Trim();
                var className = nameClass[1].Trim();

                if (!int.TryParse(eventIdStr, out int eventId)) throw new Exception();

                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();

                using var checkEvent = connection.CreateCommand();
                checkEvent.CommandText = "SELECT COUNT(*) FROM Events WHERE Id = @id";
                checkEvent.Parameters.AddWithValue("@id", eventId);
                if ((long)checkEvent.ExecuteScalar() == 0)
                {
                    await client.SendTextMessageAsync(chat.Id, "Событие не найдено.");
                    return;
                }

                using var insertCmd = connection.CreateCommand();
                insertCmd.CommandText = @"
                    INSERT INTO EventSubscriptions (EventId, ClassName, StudentName)
                    VALUES (@eventId, @class, @name)
                    ON CONFLICT DO NOTHING";
                insertCmd.Parameters.AddWithValue("@eventId", eventId);
                insertCmd.Parameters.AddWithValue("@class", className);
                insertCmd.Parameters.AddWithValue("@name", studentName);
                insertCmd.ExecuteNonQuery();

                await client.SendTextMessageAsync(chat.Id, "Успешно добавлено!");
                isEvent[userIndex] = false;
            }
            catch
            {
                await client.SendTextMessageAsync(chat.Id, "Неверный формат. Пример:\n1:Алексей Иванов-10M");
            }
        }

        private static async Task HandleLoginStep(Chat chat, Message message, ITelegramBotClient client, int userIndex)
        {
            var login = message.Text.Trim();
            if (logins.Contains(login))
            {
                curLogins[userIndex] = login;
                isLogging[userIndex] = false;
                isPassword[userIndex] = true;
                await client.SendTextMessageAsync(chat.Id, "Введите пароль:");
            }
            else
            {
                await client.SendTextMessageAsync(chat.Id, "Неверный логин. Попробуйте снова.");
            }
        }

        private static async Task HandlePasswordStep(Chat chat, Message message, ITelegramBotClient client, int userIndex)
        {
            var pwd = message.Text.Trim();
            int idx = Array.IndexOf(logins, curLogins[userIndex]);
            if (idx >= 0 && pwd == passwords[idx])
            {
                isPassword[userIndex] = false;
                current_logins[userIndex] = true;

                using var connection = new SqliteConnection($"Data Source={dbPath}");
                connection.Open();
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "UPDATE Users SET IsLoggedIn = 1, CurrentLogin = @login WHERE ChatId = @chatId";
                cmd.Parameters.AddWithValue("@login", curLogins[userIndex]);
                cmd.Parameters.AddWithValue("@chatId", current_users[userIndex]);
                cmd.ExecuteNonQuery();

                await client.SendTextMessageAsync(chat.Id, "Успешный вход!");
            }
            else
            {
                await client.SendTextMessageAsync(chat.Id, "Неверный пароль. Попробуйте снова.");
            }
        }

        private static async Task SaveAbsences(Chat chat, Message message, int userIndex)
        {
            var absences = message.Text.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            var className = curLogins[userIndex];
            var today = DateTime.Now.ToString("MM-dd");

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Attendance (Date, ClassName, AbsentStudents)
                VALUES (@date, @class, @absent)
                ON CONFLICT(Date, ClassName) DO UPDATE SET AbsentStudents = @absent";
            cmd.Parameters.AddWithValue("@date", today);
            cmd.Parameters.AddWithValue("@class", className);
            cmd.Parameters.AddWithValue("@absent", string.Join(",", absences));
            cmd.ExecuteNonQuery();

            await client.SendTextMessageAsync(chat.Id, "Отсутствующие сохранены.");
            isCheckingChildren[userIndex] = false;
        }

        private static async Task ShowAllClassesAndStudents(Chat chat, ITelegramBotClient client)
        {
            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            using var classCmd = connection.CreateCommand();
            classCmd.CommandText = "SELECT ClassName FROM Classes ORDER BY ClassName";
            using var classReader = classCmd.ExecuteReader();
            var classes = new List<string>();
            while (classReader.Read())
            {
                classes.Add(classReader.GetString("ClassName"));
            }

            if (classes.Count == 0)
            {
                await client.SendTextMessageAsync(chat.Id, "Нет классов в базе.");
                return;
            }

            foreach (var className in classes)
            {
                await client.SendTextMessageAsync(chat.Id, $"Класс: {className}");

                using var studentCmd = connection.CreateCommand();
                studentCmd.CommandText = "SELECT FullName FROM Students WHERE ClassName = @class ORDER BY FullName";
                studentCmd.Parameters.AddWithValue("@class", className);
                using var studentReader = studentCmd.ExecuteReader();
                var students = new List<string>();
                while (studentReader.Read())
                {
                    students.Add(studentReader.GetString("FullName"));
                }

                if (students.Count == 0)
                {
                    await client.SendTextMessageAsync(chat.Id, "  (нет учеников)");
                }
                else
                {
                    foreach (var student in students)
                    {
                        await client.SendTextMessageAsync(chat.Id, $"  • {student}");
                    }
                }
            }
        }
    }
}
