using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using SPBTC_Bot.Classes;

namespace SPBTC_Bot
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Console.Title = "Initialization...";

                AppSettings.Bot = new TelegramBotClient(AppSettings.Telegram_Token);
                AppSettings.Bot.OnMessage += BotOnMessageReceived;
                AppSettings.Bot.OnCallbackQuery += BotOnCallbackQueryReceived;

                Console.Title = AppSettings.Bot.GetMeAsync().Result.FirstName;

                AppSettings.Bot.StartReceiving();
                Console.ReadLine();
                AppSettings.Bot.StopReceiving();
            }
            catch (Exception ex)
            {
                // Если возникает ошибка, бот сам попробует проинформировать хозяина о ее наличии.
                NotifyAboutException(ex);
            }
        }

        private static async void NotifyAboutException(Exception ex)
        {
            if (AppSettings.Bot != null) await AppSettings.Bot.SendTextMessageAsync(AppSettings.AuthorId, "Возникло исключение.\r\n" + ex);
        }

        private static async void BotOnCallbackQueryReceived(object sender, CallbackQueryEventArgs e)
        {
            try
            {
                string callback = e.CallbackQuery.Data;
                await AppSettings.Bot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "OK");

                int SYS_UserID = Payments.Ids.IndexOf(e.CallbackQuery.From.Id);

                if (callback == "Купить")
                {
                    if (SYS_UserID == -1)
                    {
                        Payments.Ids.Add(e.CallbackQuery.From.Id);
                        Payments.Wants.Add("");
                    }
                    Payments.Init(e.CallbackQuery.From.Id);
                    return;
                }
                if (callback.Contains("rub"))
                {
                    if (SYS_UserID == -1)
                    {
                        // На случай, если бота выгрузили из памяти.
                        Payments.Ids.Add(e.CallbackQuery.From.Id);
                        Payments.Wants.Add(callback);
                    }
                    else Payments.Wants[SYS_UserID] = callback;
                    Payments.SelectPaymentMethod(e.CallbackQuery.From.Id);
                    return;
                }
                if (callback == "QIWI")
                {
                    Payments.QIWI_Init(e.CallbackQuery.From.Id);
                }
            }
            catch (Exception) { }
        }

        private static async void BotOnMessageReceived(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var message = e.Message;
            if (message == null || message.Type != MessageType.Text || message.Text == null) return;
            string name = $"{message.From.FirstName} {message.From.LastName}({message.From.Username})";
            Console.WriteLine($"{name}: {message.Text}");
            if(message.Text == "/start")
            {
                var startButtons = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Купить")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Починить")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Привязать")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Перепривязать")
                        }
                    });
                await AppSettings.Bot.SendTextMessageAsync(message.From.Id, "Choose your destiny:", replyMarkup: startButtons);
                return;
            }
            long TX_ID = 0;
            if(Int64.TryParse(message.Text, out TX_ID))
            {
                if(TX_ID != 0)
                {
                    Payments.QIWI_Check(message.From.Id, TX_ID);
                }
            }
        }
    }
}
