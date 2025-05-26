using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.InlineQueryResults;
using System.IO;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Drawing;
using static System.Net.Mime.MediaTypeNames;


namespace tgBot
{
    internal class Program
    {
        public static List<ChatId> current_users = new List<ChatId>();

        public static List<bool> current_logins = new List<bool>();

        public static List<bool> isloggining = new List<bool>();

        public static List<bool> isPassword = new List<bool>();

        public static List<bool> isCheckingShildren = new List<bool>();

        public static List<string> cur_logins = new List<string>();

        private static List<string> events = new List<string>();

        public static List<bool> isEvent = new List<bool>();

        private static string[] children;

        private static States _state;

        private static TelegramBotClient client;

        private static ReceiverOptions receiver;

        private static string token;

        static DateTime currentDateOld;
        static DateTime currentDateNew;

        static string _currentDateString;

        static string desktopPath;

        static string folderPath;
        static Dictionary<string, List<string>> classChildren;

        private static List<string> alarm_events;

        private static Dictionary<string, List<string>> alarm_event_huge;

        private static Timer _timer;

        static async Task Main()
        {

            initializeUsers();

            SearchForChildren();

            //bot token
            token = "7638423803:AAFM0hV_Q50mk1GcheAxygVdL6eigQjmYrU";

            var cts = new CancellationTokenSource();

            client = new TelegramBotClient("7638423803:AAFM0hV_Q50mk1GcheAxygVdL6eigQjmYrU");

            createReceiver();


            client.StartReceiving(UpdateHandler, ExceptionHandler, receiver, cts.Token);

            User me = await client.GetMeAsync();

            Console.WriteLine($"bot is active {me.FirstName}");

            await Task.Delay(-1);
        }

        private static async Task ExceptionHandler(ITelegramBotClient client, Exception exception, CancellationToken cts)
        {
            Console.WriteLine(exception.Message);
            await Task.CompletedTask;
        }

        private static async Task UpdateHandler(ITelegramBotClient client, Update update, CancellationToken cts)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        {
                            await CheckForDate();
                            //write message and its properties
                            Message current_message = update.Message;

                            User current_user = current_message.From;

                            Chat current_chat = current_message.Chat;

                            AddUser(current_chat);

                            _state = new States(current_chat, current_user, current_message, client);

                            int searchIndexOfPerson = _state.SearchForUserIndex(current_chat);
                            //Console.WriteLine(isloggining[searchIndexOfPerson]);
                            //Console.WriteLine(isPassword[searchIndexOfPerson]);
                            //Console.WriteLine(isCheckingShildren[searchIndexOfPerson]);
                            //Console.WriteLine(current_logins[searchIndexOfPerson]);
                            //Console.WriteLine(folderPath);
                            //Console.WriteLine(cur_logins[searchIndexOfPerson]);
                            if (current_message.Type == MessageType.Text)
                            {
                                Console.WriteLine($"{current_user.FirstName} {current_user.Id} написал: {current_message.Text} в: {current_chat.Id}");

                                if (_state.returnState() == States.isCheckChildString && current_message.Text != "/childrencheck")
                                {
                                    //if (_state.returnState() == States.isLoggined)
                                    //{
                                    await TryToCheckChildren(current_chat, current_message, current_user, client, _state);
                                    //}
                                }
                                if (_state.returnState() == States.isEvent && _state.returnState() != States.isPassTyping && _state.returnState() != States.isLogging && !current_message.Text.Contains("/subscribe") && !current_message.Text.Contains("/events"))
                                {
                                    await TryToSubscribeToEvent(current_chat, current_message, current_user, client, _state);
                                }
                                if (current_message.Text.Contains("/start"))
                                {
                                    _state.ChangeStates(ref isloggining, searchIndexOfPerson, false);
                                    _state.ChangeStates(ref isPassword, searchIndexOfPerson, false);

                                    string textToSend = "Я бот Гимназии Один Девять";
                                    await client.SendTextMessageAsync(current_chat.Id, textToSend, replyToMessageId: current_message.MessageId);

                                    textToSend = "нажми кнопку, чтобы узнать мою фукнциональность\n" + "/functional\n" + "или мое описание\n" + "/info";
                                    await client.SendTextMessageAsync(current_chat.Id, textToSend);
                                   
                                    return;
                                }
                                if (current_message.Text.Contains("/exit"))
                                {
                                    _state.ChangeStates(ref isloggining, searchIndexOfPerson, false);
                                    _state.ChangeStates(ref isPassword, searchIndexOfPerson, false);
                                    _state.ChangeStates(ref current_logins, searchIndexOfPerson, false);
                                    _state.ChangeStates(ref isCheckingShildren, searchIndexOfPerson, false);
                                    _state.ChangeStates(ref isEvent, searchIndexOfPerson, false);
                                    cur_logins[searchIndexOfPerson] = string.Empty;
                                    await client.SendTextMessageAsync(current_chat.Id, "you have been quited", replyToMessageId: current_message.MessageId);
                                    return;
                                }
                                if (current_message.Text.Contains("/events"))
                                {
                                    await TryToShowEvents(current_chat, current_message, current_user, client, _state);
                                    _state.ChangeStates(ref isEvent, searchIndexOfPerson, false);
                                }
                                if (current_message.Text.Contains("/subscribe"))
                                {
                                    await client.SendTextMessageAsync(current_chat,"choose event to subscribe, write the number of it and your name\n" + "for example:\n" +"1:Alex Something-10M");
                                    await client.SendTextMessageAsync(current_chat,"if you dont have the list of events write\n" + "/events");
                                    _state.ChangeStates(ref isEvent, searchIndexOfPerson, true);
                                    return;
                                }                                
                                if (current_message.Text.Contains("/functional"))
                                {
                                    if (current_logins[searchIndexOfPerson])
                                    {
                                        await client.SendTextMessageAsync(current_chat.Id, "");
                                    }
                                    else
                                    {
                                        await client.SendTextMessageAsync(current_chat.Id, "before it, login");
                                    }
                                    return;
                                }
                                if (current_message.Text.Contains("/login") || _state.returnState() == States.isLogging)
                                {
                                    if (_state.returnState() == States.isLogging)
                                    {
                                        await TryToLogIn(current_chat, current_message, current_user, client, _state);
                                        return;
                                    }
                                    if (_state.returnState() == States.isLoggined)
                                    {
                                        await client.SendTextMessageAsync(current_chat.Id, "you have already loged", replyToMessageId: current_message.MessageId);
                                        return;
                                    }
                                    else
                                    {
                                        _state.ChangeStates(ref isloggining, searchIndexOfPerson, true);
                                        _state.ChangeStates(ref isPassword, searchIndexOfPerson, false);
                                        await client.SendTextMessageAsync(current_chat.Id, "type your login", replyToMessageId: current_message.MessageId);
                                        return;
                                    }
                                }
                                if (isPassword[_state.SearchForUserIndex(current_chat)])
                                {
                                    await TryToEnterPassword(current_chat, current_message, current_user, client, _state);
                                    return;
                                }
                                if (current_message.Text.Equals("/childrencheck"))
                                {

                                    if (_state.returnState() == States.isLoggined)
                                    {
                                        if (_state.returnState() != States.isCheckChildString)
                                        {
                                            if (classChildren.TryGetValue(cur_logins[searchIndexOfPerson], out List<string> ChildrenList))
                                            {
                                                for (int i = 0; i < ChildrenList.Count; i++)
                                                {
                                                    await client.SendTextMessageAsync(current_chat.Id, ChildrenList[i]);
                                                }
                                            }
                                            await client.SendTextMessageAsync(current_chat.Id, "------------------");
                                            await client.SendTextMessageAsync(current_chat.Id, "type who is apsent", replyToMessageId: current_message.MessageId);

                                            _state.ChangeStates(ref isCheckingShildren, searchIndexOfPerson, true);
                                            return;
                                        }
                                        return;
                                    }
                                    else
                                    {
                                        await client.SendTextMessageAsync(current_chat.Id, "before it login", replyToMessageId: current_message.MessageId);
                                        return;
                                    }
                                }


                            }
                            return;

                        }

                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public int ISearchForUserIndex(Chat _user)
        {
            for (int i = 0; i < current_users.Count; i++)
            {
                if (current_users[i] == _user.Id)
                {
                    Console.WriteLine(i);
                    return i;
                }
            }
            return 0;
        }
        private static async Task removeReplyKeyboard(Chat chat)
        {
            var replyKeyboardRemove = new ReplyKeyboardRemove();
            await client.SendTextMessageAsync(chat.Id, "reply", replyMarkup: replyKeyboardRemove);
        }
        private static ReplyKeyboardMarkup createMarkupKeyBoard()
        {
            ReplyKeyboardMarkup reply = new ReplyKeyboardMarkup(
                new List<KeyboardButton[]>()
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("something"),
                        new KeyboardButton("something")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("something"),
                        new KeyboardButton("something")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("something"),
                        new KeyboardButton("something")
                    }
                }
                )
            {
                ResizeKeyboard = true
            };
            return reply;
        }
        private static ReplyKeyboardMarkup createMarkupKeyBoardLoggedIn()
        {
            ReplyKeyboardMarkup reply = new ReplyKeyboardMarkup(
                new List<KeyboardButton[]>()
                {
                    new KeyboardButton[]
                    {
                        new KeyboardButton("something"),
                        new KeyboardButton("something")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("something"),
                        new KeyboardButton("something")
                    },
                    new KeyboardButton[]
                    {
                        new KeyboardButton("отметить детей"),
                    }
                }
                )
            {
                ResizeKeyboard = true
            };
            return reply;
        }
        private static BotCommand[] createCommands()
        {
            BotCommand[] command = new BotCommand[]
            {
                new BotCommand { Command = "/start", Description = "Start the bot" },
                new BotCommand { Command = "/functional", Description = "Get a list of commands" },
                new BotCommand { Command = "/info", Description = "Get information about the bot" },
                new BotCommand { Command = "/LogIn", Description = "log in as a teacher" }
            };
            return command;
        }

        private static void createReceiver()
        {
            receiver = new ReceiverOptions
            {
                AllowedUpdates = new UpdateType[]
                {
                    UpdateType.Message,
                    UpdateType.CallbackQuery
                },
                ThrowPendingUpdates = true,
            };
        }

        private static string[] logins;

        private static string[] passwords;


        private static async void addUser(Chat chat)
        {
            if (current_users.Count != 0)
            {
                for (int i = 0; i < current_users.Count; i++)
                {
                    if (chat.Id != current_users[i])
                    {
                        current_users.Add(chat.Id);
                        current_logins.Add(false);
                        isloggining.Add(false);
                        isPassword.Add(false);
                        isCheckingShildren.Add(false);
                        isEvent.Add(false);
                        cur_logins.Add("");
                        alarm_events.Add(string.Empty);
                    }
                    else
                    {
                        return;
                    }
                }
            }
            await Task.CompletedTask;
        }

        private static async void AddUser(Chat chat)
        {
            string newDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string newPath = Path.Combine(newDesktopPath, "people.txt");
            string[] users = System.IO.File.ReadAllLines(newPath);
            for (int i = 0; i < users.Length; i++)
            {
                if (users[i] == Convert.ToString(chat.Id))
                {
                    return;
                }
            }
            current_logins.Add(false);
            isloggining.Add(false);
            isPassword.Add(false);
            isCheckingShildren.Add(false);
            isEvent.Add(false);
            cur_logins.Add("");
            alarm_events.Add(string.Empty);
            string newUser = Convert.ToString(chat.Id);
            string oldUsers = System.IO.File.ReadAllText(newPath);
            System.IO.File.WriteAllText(newPath, oldUsers+"\n"+newUser);
            for (int i = 0; i < users.Length; i++)
            {
                //check if it is me(creator)
                if (int.Parse(users[i]) != 1621225477)
                {
                    current_users.Add(users[i]);
                }
            }
            await Task.CompletedTask;
        }

        private static bool succesfulLogin = false;

        private static void SearchForChildren()
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string childrenFolderPath = Path.Combine(desktopPath, "children");

            if (!Directory.Exists(childrenFolderPath))
            {
                Console.WriteLine("Папка 'children' не найдена на рабочем столе.");
                return;
            }

            string[] txtFiles = Directory.GetFiles(childrenFolderPath, "*.txt");

            classChildren = new Dictionary<string, List<string>>();


            foreach (string txtFile in txtFiles)
            {
                string className = Path.GetFileNameWithoutExtension(txtFile);

                string[] children = System.IO.File.ReadAllLines(txtFile);

                classChildren[className] = new List<string>(children);
            }
            //foreach (var kvp in classChildren)
            //{
            //    Console.WriteLine($"Класс: {kvp.Key}");
            //    foreach (var child in kvp.Value)
            //    {
            //        Console.WriteLine($"  {child}");
            //    }
            //}
        }

        private static void initializeUsers()
        {
            logins = new string[]
            {
                "10M",
                "9L1"
            };
            passwords = new string[]
            {
                "123",
                "456"
            };
            isCheckingShildren.Add(false);
            current_logins.Add(false);
            isloggining.Add(false);
            isPassword.Add(false);
            current_users.Add(1621225477);
            cur_logins.Add(string.Empty);
            isEvent.Add(false);
            //alarm_events.Add(string.Empty);
        }

        private static async Task CheckForDate()
        {
            currentDateNew = DateTime.Now;
            string newDateString = currentDateNew.ToString("MM-dd");
            _currentDateString = currentDateOld.ToString("MM-dd");
            if (newDateString != _currentDateString)
            {
                currentDateOld = currentDateNew;
                _currentDateString = currentDateOld.ToString("MM-dd");
                desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                folderPath = Path.Combine(desktopPath, _currentDateString);
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    foreach (var kvp in classChildren)
                    {
                        string className = kvp.Key;
                        List<string> children = kvp.Value;

                        string outputFilePath = Path.Combine(folderPath, $"{className}.txt");
                        if (!Directory.Exists(outputFilePath))
                        {
                            System.IO.File.CreateText(outputFilePath);
                            Console.WriteLine($"Файл создан: {outputFilePath}");
                            //System.IO.File.WriteAllLines(outputFilePath, children);
                        }
                    }
                    return;
                }
                await Task.CompletedTask;
            }
            else
            {
                return;
            }
        }
        private static async Task TryToSubscribeToEvent(Chat chat, Message message, User user, ITelegramBotClient client, States state)
        {
            string OlddesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string eventDirectory = Path.Combine(OlddesktopPath, "events");

            events = GetFolderNames(eventDirectory);

            string input = message.Text;

            string[] parts = input.Split(new char[] { ':' }, 2);

            string event_string = parts[0];

            //Console.WriteLine(event_string);

            string[] remaining_parts = parts[1].Split(new char[] { '-' }, 2);

            string name_person_event = remaining_parts[0];

            //Console.WriteLine(name_person_event);

            string person_class = remaining_parts[1];

            //Console.WriteLine(person_class);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            string main_direction = Path.Combine(desktopPath, "checked");

            string newEventAfter1 = Path.Combine(main_direction, events[int.Parse(event_string) - 1]);
            

            for (int i = 0; i < events.Count; i++)
            {
                string newEvent = Path.Combine(main_direction, events[i]);
                if (!Directory.Exists(newEvent))
                {
                    Directory.CreateDirectory(newEvent);
                }
            }
            //for (int j = 0; j < logins.Length; j++)
            //{
            //    string newClassAfter1 = Path.Combine(newEventAfter1, logins[j] + ".txt");
            //    if (!Directory.Exists(newClassAfter1))
            //    {
            //        System.IO.File.WriteAllText(newClassAfter1, string.Empty);
            //    }
            //}

            string newEventAfter = Path.Combine(main_direction, events[int.Parse(event_string) - 1]);
            string newClassAfter = Path.Combine(newEventAfter, person_class+".txt");
            //if (Directory.Exists(newClassAfter))
            //{
            //    write(newClassAfter, name_person_event);
            //}
            //else
            //{
            //    System.IO.File.CreateText(newClassAfter);
            //    write(newClassAfter, name_person_event);
            //}
            string old;
            if (System.IO.File.Exists(newClassAfter))
            {
                //Console.WriteLine("aboba");
                //write(newClassAfter, string.Empty);
                old = System.IO.File.ReadAllText(newClassAfter);
            }
            else
            {
                write(newClassAfter, string.Empty);
                old = System.IO.File.ReadAllText(newClassAfter);
            }
            
            Console.WriteLine(old);
            write(newClassAfter, old+ "\n" + name_person_event);
            await client.SendTextMessageAsync(chat, "successfuly added");
            state.ChangeStates(ref isEvent, state.SearchForUserIndex(chat), false);

            return;
        }
        private static void write(string path, string toWrite)
        {
            System.IO.File.WriteAllText(path, toWrite);
        }
        private static async Task createText(string path)
        {
            System.IO.File.CreateText(path);
            await Task.CompletedTask;
        }
        private static async Task writeAll(string path, string toWrite)
        {
            System.IO.File.WriteAllText(path, toWrite);
            await Task.CompletedTask;
        }
        private static async Task TryToShowEvents(Chat chat, Message message, User user, ITelegramBotClient client, States state)
        {
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string eventDirectory = Path.Combine(desktopPath, "events");

            if (Directory.Exists(eventDirectory))
            {
                //get events from folder
                events = GetFolderNames(eventDirectory);
                for (int i = 0; i < events.Count; i++)
                {
                    string folder = Path.Combine(eventDirectory, events[i]);
                    await ProcessFolders(folder, chat, client, i);
                }
                //foreach (var item in events)
                //{
                //    string folder = Path.Combine(eventDirectory, item);
                //    await ProcessFolders(folder, chat, client);
                //}
            }
            await Task.CompletedTask;
        }
        private static List<string> GetFolderNames(string directory)
        {
            List<string> folderNames = new List<string>();

            DirectoryInfo directoryInfo = new DirectoryInfo(directory);

            DirectoryInfo[] subDirectories = directoryInfo.GetDirectories();

            foreach (var subDirectory in subDirectories)
            {
                folderNames.Add(subDirectory.Name);
            }
            return folderNames;
        }

        private static async Task ProcessFolders(string directory, Chat user, ITelegramBotClient client, int j)
        {
            string dateFile = Path.Combine(directory, "date.txt");
            string descriptionFile = Path.Combine(directory, "description.txt");
            string imageFile = Path.Combine(directory, "image.png");

            using (var fileStream = new FileStream(imageFile, FileMode.Open, FileAccess.Read))
            {
                var input = new InputFileStream(fileStream);
                await client.SendPhotoAsync(user, input);
            }

            string dateContent = System.IO.File.ReadAllText(dateFile);
            string descriptionContent = System.IO.File.ReadAllText(descriptionFile);

            await ShowEvents(dateContent, descriptionContent, user, client, j);

            await Task.CompletedTask;
        }
        private static async Task ShowEvents(string date, string descrit, Chat user, ITelegramBotClient client, int j)
        {
            await client.SendTextMessageAsync(user, (j+1).ToString() + ": " + events[j]);
            await client.SendTextMessageAsync(user, descrit);
            await client.SendTextMessageAsync(user, "will be in: " + date);
            await Task.CompletedTask;
        }

        private static async Task TryToCheckChildren(Chat chat, Message message, User user, ITelegramBotClient client, States state)
        {
            //Console.WriteLine(2);
            string userText = message.Text;
            //if (Directory.Exists(folderPath))
            //{
            //Console.WriteLine(1);
            string newdesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string newfolderPath = Path.Combine(newdesktopPath, _currentDateString);
            string outputfile = Path.Combine(newfolderPath, $"{cur_logins[_state.SearchForUserIndex(chat)]}.txt");
            //if (Directory.Exists(outputfile))
            //{
            System.IO.File.WriteAllText(outputfile, string.Empty);
            //Console.WriteLine(3);
            string[] lines = userText.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            List<string> childrenList = new List<string>(lines);
            for (int i = 0; i < childrenList.Count; i++)
            {
                System.IO.File.WriteAllLines(outputfile, childrenList);
            }
            await client.SendTextMessageAsync(chat.Id, "successfully written", replyToMessageId: message.MessageId);
            _state.ChangeStates(ref isCheckingShildren, _state.SearchForUserIndex(chat), false);
            return;
            //}
            //}s
            await Task.CompletedTask;
        }
        private static async Task TryToLogIn(Chat chat, Message message, User user, ITelegramBotClient client, States state)
        {
            if (state.returnState() == States.isLogging)
            {
                for (int i = 0; i < logins.Length; i++)
                {
                    if (message.Text == logins[i])
                    {
                        cur_logins[_state.SearchForUserIndex(chat)] = logins[i];
                        _state.ChangeStates(ref isloggining, _state.SearchForUserIndex(chat), false);
                        _state.ChangeStates(ref isPassword, _state.SearchForUserIndex(chat), true);
                        await client.SendTextMessageAsync(chat.Id, "type your password", replyToMessageId: message.MessageId);
                        return;
                    }
                }
                await client.SendTextMessageAsync(chat.Id, "user is incorrect retype it", replyToMessageId: message.MessageId);
            }
            await Task.CompletedTask;
        }
        private static async Task TryToEnterPassword(Chat chat, Message message, User user, ITelegramBotClient client, States state)
        {
            if (state.returnState() == States.isPassTyping)
            {
                for (int i = 0; i < passwords.Length; i++)
                {
                    if (message.Text == passwords[i])
                    {
                        isPassword[_state.SearchForUserIndex(chat)] = false;
                        current_logins[_state.SearchForUserIndex(chat)] = true;
                        await client.SendTextMessageAsync(chat.Id, "successful", replyToMessageId: message.MessageId);
                        return;
                    }
                }
                await client.SendTextMessageAsync(chat.Id, "password is incorrect retype it", replyToMessageId: message.MessageId);

            }
            await Task.CompletedTask;
        }

        private static async Task TryToAlarmUsers()
        {

            await Task.CompletedTask;
        }
    }
}
