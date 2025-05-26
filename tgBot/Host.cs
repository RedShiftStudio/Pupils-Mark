using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace tgBot
{
    internal class Host
    {
        TelegramBotClient client;
        public Host(string token)
        {
            client = new TelegramBotClient(token);
        }
        public void Start()
        {
            client.StartReceiving(updateHandler, errorHandler);
        }

        private async Task errorHandler(ITelegramBotClient client, Exception exception, CancellationToken token)
        {
            Console.WriteLine($"there is an error: {exception.Message}");
            await Task.CompletedTask;
        }

        private async Task updateHandler(ITelegramBotClient client, Update update, CancellationToken token)
        {
            Message mes = update.Message;
            User user = mes.From;
            if (mes.Type == MessageType.Text)
            {
                Console.WriteLine($"{user.Username} wrote the bot: {mes.Text}");
                if (mes.Text.Contains("/start"))
                {
                    
                }
            }
            Console.WriteLine(update.Message.Text);
            await Task.CompletedTask;
        }
    }
}
