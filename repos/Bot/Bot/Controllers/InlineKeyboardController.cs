using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace VoiceTexterBot.Controllers
{
    public class InlineKeyboardController
    {
        private readonly ITelegramBotClient _telegramClient;

        public InlineKeyboardController(ITelegramBotClient telegramBotClient)
        {
            _telegramClient = telegramBotClient;
        }
        public InlineKeyboardMarkup GetLanguageKeyboard()
        {
            var buttons = new List<InlineKeyboardButton[]>
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Русский", "ru"),
                    InlineKeyboardButton.WithCallbackData("English", "en"),
                    InlineKeyboardButton.WithCallbackData("Français", "fr")
                }
            };

            return new InlineKeyboardMarkup(buttons);
        }

        public async Task Handle(CallbackQuery? callbackQuery, CancellationToken ct)
        {
            if (callbackQuery == null) return;

            Console.WriteLine($"Контроллер {GetType().Name} получил сообщение");

            string languageText = callbackQuery.Data switch
            {
                "ru" => "Русский",
                "en" => "English",
                "fr" => "Français",
                _ => "Неизвестный язык"
            };

            await _telegramClient.SendMessage(
                chatId: callbackQuery.From.Id,
                text: $"Выбран язык: {languageText} (код: {callbackQuery.Data})",
                cancellationToken: ct);
        }
    }
}