using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using QiwiApiSharp;
using QiwiApiSharp.Enumerations;
using QiwiApiSharp.Entities;

namespace SPBTC_Bot.Classes
{
    class Payments
    {
        public static List<int> Ids = new List<int>();
        public static List<string> Wants = new List<string>();

        private static Random rnd = new Random();
        public static async void Init(int Id)
        {
            var Buttons = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Mails[800rub]"),
                            InlineKeyboardButton.WithCallbackData("MailsBuilder[700rub]"),
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Steam[800rub]"),
                            InlineKeyboardButton.WithCallbackData("SteamRetriver[2000rub]")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("ArcheAge[500rub]"),
                            InlineKeyboardButton.WithCallbackData("CrossFire[600rub]")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("WarFace[650rub]"),
                            InlineKeyboardButton.WithCallbackData("WoT[650rub]")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("AntiPublic[500rub]"),
                            InlineKeyboardButton.WithCallbackData("AntiPublic Ext[700rub]")
                        }
                    });
            await AppSettings.Bot.SendTextMessageAsync(Id, "Какую программу Вы хотите купить?", replyMarkup: Buttons);
        }

        public static async void SelectPaymentMethod(int Id)
        {
            var Buttons = new InlineKeyboardMarkup(new[]
            {
                InlineKeyboardButton.WithCallbackData("QIWI"),
                InlineKeyboardButton.WithCallbackData("Bitcoin")
            });
            await AppSettings.Bot.SendTextMessageAsync(Id, "Пожалуйста, выберите метод оплаты.", replyMarkup: Buttons);
        }

        public static async void QIWI_Init(int Id)
        {
            int PID = Ids.IndexOf(Id);
            string soft = Wants[PID];
            int price = Int32.Parse(soft.Split('[')[1].Split(']')[0].Replace("rub","")); // Да, цена парсится из названия. Да, другие варианты - еще хуже.
            string CardWithoutSpaces = AppSettings.QiwiNumber.Replace(" ", "");
            string link = $"https://qiwi.com/payment/form/1963?&amountInteger={price}&amountFraction=00&extra%5B%27account%27%5D={CardWithoutSpaces}";
            string text = $"[Нажмите чтобы оплатить]({link}), или сделайте это вручную. Для ручной оплаты переведите {price} рублей на карту QIWI(Visa): {AppSettings.QiwiNumber}. После чего пришлите в ответ № квитанции(он же № платежа). Эту информацию можно найти на странице оплаты после совершенного платежа, или в истории Вашего QIWI кошелька.";
            await AppSettings.Bot.SendTextMessageAsync(Id, text, parseMode: ParseMode.Markdown);
        }

        public static async void QIWI_Check(int Id, long TX_ID)
        {
            int PID = Ids.IndexOf(Id);
            string soft = Wants[PID];
            int WantedSum = Int32.Parse(soft.Split('[')[1].Split(']')[0].Replace("rub", ""));
            QiwiApi.Initialize(AppSettings.QiwiToken);
            if (QiwiApi.Initialized)
            {
                Source[] src = { Source.CARD, Source.QW_RUB };
                PaymentHistoryResponse H = await QiwiApi.PaymentHistoryAsync(AppSettings.QiwiLogin, 10, Operation.IN, src);
                List<Payment> payments = H.data;
                foreach (Payment payment in payments)
                {
                    if (payment.txnId == TX_ID)
                    {
                        if(payment.status == PaymentStatus.SUCCESS)
                        {
                            if(payment.sum.amount >= WantedSum)
                            {
                                // Дописать сюда то, что будет происходить, если оплата успешно прошла
                            }
                            else
                            {
                                await AppSettings.Bot.SendTextMessageAsync(Id, $"Платеж найден, но переведенная сумма меньше требуемой. Вы перевели {payment.sum.amount}, а нужно было {WantedSum}. Свяжитесь с автором бота({AppSettings.AuthorContact}) для урегулирования этого вопроса.");
                            }
                        }
                        else
                        {
                            await AppSettings.Bot.SendTextMessageAsync(Id, "Платеж найден, но не исполнен. Возможно, возникла ошибка при оплате, или требуется время на обработку. Попробуйте подождать несколько минут и снова прислать номер платежа.");
                        }
                        return;
                    }
                }
                await AppSettings.Bot.SendTextMessageAsync(Id, "Платеж не найден.");
            }
            else
            {
                await AppSettings.Bot.SendTextMessageAsync(Id, "Ошибка инициализации QIWI. Попробуйте прислать № платежа еще раз. Если ошибка повторится вновь, обратитесь к разработчику: " + AppSettings.AuthorContact);
            }
        }


    }
}
