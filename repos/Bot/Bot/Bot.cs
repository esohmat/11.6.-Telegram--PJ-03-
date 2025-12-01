using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace VoiceTexterBot
{
    internal class Bot : BackgroundService
    {
        private ITelegramBotClient _telegramClient;

      
        private Dictionary<long, string> _userStates = new Dictionary<long, string>();

        public Bot(ITelegramBotClient telegramClient)
        {
            _telegramClient = telegramClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _telegramClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, new ReceiverOptions() { AllowedUpdates = { } }, cancellationToken: stoppingToken);

            Console.WriteLine("Бот запущен");
        }

        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message)
            {
                var message = update.Message;

                if (message.Text != null)
                {
                    
                    if (message.Text == "/start")
                    {
                        await ShowMainMenu(message.Chat.Id, cancellationToken);
                        return;
                    }

                    if (_userStates.ContainsKey(message.Chat.Id))
                    {
                        var userState = _userStates[message.Chat.Id];

                        if (userState == "count_chars")
                        {
                         
                            int charCount = message.Text.Length;
                            await _telegramClient.SendMessage( message.Chat.Id, $"В вашем сообщении {charCount} символов",  cancellationToken: cancellationToken);

                           
                            await ShowMainMenu(message.Chat.Id, cancellationToken);
                            _userStates.Remove(message.Chat.Id); 
                        }
                        else if (userState == "sum_numbers")
                        {
                         
                            try
                            {
                                var numbers = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                double sum = 0;
                                bool hasError = false;

                                foreach (var number in numbers)
                                {
                                    if (double.TryParse(number, out double parsedNumber))
                                    {
                                        sum += parsedNumber;
                                    }
                                    else
                                    {
                                        hasError = true;
                                        break;
                                    }
                                }

                                if (!hasError)
                                {
                                    await _telegramClient.SendMessage(
                                        message.Chat.Id,
                                        $"Сумма чисел: {sum}",
                                        cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    await _telegramClient.SendMessage(  message.Chat.Id, "Пожалуйста, отправьте только числа, разделенные пробелами (например: 2 3 15)", cancellationToken: cancellationToken);
                                }
                            }
                            catch (Exception)
                            {
                                await _telegramClient.SendMessage( message.Chat.Id, " Произошла ошибка при обработке чисел. Пожалуйста, cancellationToken: cancellationToken)");
                            }

                            await ShowMainMenu(message.Chat.Id, cancellationToken);
                            _userStates.Remove(message.Chat.Id);
                        }
                    }
                }
            }

            if (update.Type == UpdateType.CallbackQuery)
            {
                var callbackQuery = update.CallbackQuery;
                var chatId = callbackQuery.Message.Chat.Id;

                await _telegramClient.AnswerCallbackQuery( callbackQuery.Id, cancellationToken: cancellationToken);

                await _telegramClient.DeleteMessage(chatId, callbackQuery.Message.MessageId, cancellationToken: cancellationToken);

                if (callbackQuery.Data == "count_chars")
                { 
                    _userStates[chatId] = "count_chars";

                    await _telegramClient.SendMessage( chatId, "Отправьте мне текст, и я посчитаю количество символов в нем:", cancellationToken: cancellationToken);
                }
                else if (callbackQuery.Data == "sum_numbers")
                {
                    _userStates[chatId] = "sum_numbers";

                    await _telegramClient.SendMessage(
                        chatId,
                        "Отправьте мне числа, разделенные пробелами (например: 2 3 15), и я вычислю их сумму:", cancellationToken: cancellationToken);
                }
            }
        }
        private async Task ShowMainMenu(long chatId, CancellationToken cancellationToken)
        {
            var inlineKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData(" Подсчет символов", "count_chars"),
                    InlineKeyboardButton.WithCallbackData(" Сумма чисел", "sum_numbers")
                }
            });

            await _telegramClient.SendMessage(chatId, "Выберите действие:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
        }

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}", _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            Console.WriteLine("Ожидаем 10 секунд перед повторным подключением.");
            Thread.Sleep(10000);

            return Task.CompletedTask;
        }
    }
}