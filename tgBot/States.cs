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

namespace tgBot
{
    public class States
    {
        Chat chat;
        User user;
        Message mess;
        ITelegramBotClient client;


        public static string isLogging = "isLogin";
        public static string isLoggined = "isLoged";
        public static string defaultState = "def";
        public static string isPassTyping = "pass";
        public static string isCheckChildString = "child";
        public static string isEvent = "event";
        public string _currentState;

        public static List<ChatId> _currentUsers = Program.current_users;
        public static List<bool> _isLogIn = Program.isloggining;
        public static List<bool> _isPass = Program.isPassword;
        public static List<bool> _isLoged = Program.current_logins;
        public static List<bool> isChecingChildre = Program.isCheckingShildren;
        public static List<bool> isEventBool = Program.isEvent;
        public States(Chat _chat, User _user, Message _mess, ITelegramBotClient _client)
        {
            this.chat = _chat;
            this.user = _user;
            this.mess = _mess;
            this.client = _client;
        }
        public void ChangeStates(ref List<bool> newList,int j, bool change)
        {
            for (int i = 0; i < newList.Count; i++)
            {
                if (i == j)
                {
                    newList[i] = change;
                }
            }
        }
        public String returnState()
        {
            int j = SearchForUserIndex(chat);
            if (ReturnSearchedStatebool(_isLogIn, j))
            {
                _currentState = isLogging;
            }
            if (ReturnSearchedStatebool(_isPass, j))
            {
                _currentState = isPassTyping;
            }
            if (ReturnSearchedStatebool(_isLoged, j))
            {
                _currentState = isLoggined;
            }
            if (ReturnSearchedStatebool(isChecingChildre, j))
            {
                _currentState = isCheckChildString;
            }
            if (ReturnSearchedStatebool(isEventBool, j))
            {
                _currentState = isEvent;
            }
            return _currentState;
        }
        public int SearchForUserIndex(Chat _user)
        {
            for (int i = 0; i < _currentUsers.Count; i++)
            {
                if (_currentUsers[i] == _user.Id)
                {
                    return i;
                }
            }
            return 0;
        }
        public bool ReturnSearchedStatebool(List<bool> newList, int _searchIndex)
        {
            for (int i = 0; i < newList.Count; i++)
            {
                if (i == _searchIndex)
                {
                    if (newList[i])
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
