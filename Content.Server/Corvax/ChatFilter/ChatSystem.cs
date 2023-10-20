using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    private static readonly Dictionary<string, string> SlangReplace = new()
    {
        // Game
        { "кк", "красный код" },
        { "ск", "синий код" },
        { "зк", "зелёный код" },
        { "инжинер", "инженер" },
        { "дизарм", "толчок" },
        // IC
        { "синд", "синдикат" },
        { "кст", "кстати" },
        { "плз", "пожалуйста" },
        { "пж", "пожалуйста" },
        { "сяб", "спасибо" },
        { "прив", "привет" },
        { "ок", "окей" },
        { "лан", "ладно" },
        { "збс", "заебись" },
        { "мб", "может быть" },
        { "омг", "боже мой" },
        { "нзч", "не за что" },
        { "ясн", "ясно" },
        { "всм", "всмысле" },
        { "чзх", "что за херня?" },
        { "гг", "хорошо сработано" },
        { "брух", "мда..." },
        { "хилл", "лечение" },
        { "подхиль", "полечи" },
        { "хильни", "полечи" },
        { "хелп", "помоги" },
        { "хелпани", "помоги" },
        { "хелпанул", "помог" },
        { "крч", "короче говоря" },
        { "шатл", "шаттл" },
        // OOC
        { "афк", "ссд" },
        { "админ", "бог" },
        { "админы", "боги" },
        { "админов", "богов" },
        { "забанят", "покарают" },
        { "бан", "наказание" },
        { "нонрп", "плохо" },
        { "нрп", "плохо" },
        { "ерп", "ужас" },
        { "рдм", "плохо" },
        { "дм", "плохо" },
        { "гриф", "плохо" },
        { "фрикил", "плохо" },
        { "фрикилл", "плохо" },
        { "лкм", "левая рука" },
        { "пкм", "правая рука" },
        // Twitch
        { "Пидор", "кхе-кхе" },
        { "Пидорас", "кхе-кхе" },
        { "Пидорасина", "кхе-кхе" },
        { "Пидар", "кхе-кхе" },
        { "Педар", "кхе-кхе" },
        { "Пииддаарр", "кхе-кхе" },
        { "пидор", "кхе-кхе" },
        { "пидорас", "кхе-кхе" },
        { "пидарас", "кхе-кхе" },
        { "педик", "кхе-кхе" },
        { "даун", "кхе-кхе" },
        { "слава Украине", "кхе-кхе" },
        { "Слава Украине", "кхе-кхе" },
        { "слава России", "кхе-кхе" },
        { "Слава России", "кхе-кхе" },
        { "ватник", "кхе-кхе" },
        { "хохол", "кхе-кхе" },
        { "порно", "кхе-кхе" },
        { "нигер", "кхе-кхе" },
        { "негр", "кхе-кхе" },
        { "нееггрр", "кхе-кхе" },
        { "ниггер", "кхе-кхе" },
        { "негер", "кхе-кхе" },
        { "нигир", "кхе-кхе" },
        { "нига", "кхе-кхе" },
        { "Негры", "кхе-кхе" },
        { "негра", "кхе-кхе" },
        { "негру", "кхе-кхе" },
        { "негры", "кхе-кхе" },
    };

    private string ReplaceWords(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        return Regex.Replace(message, "\\b(\\w+)\\b", match =>
        {
            bool isUpperCase = match.Value.All(Char.IsUpper);

            if (SlangReplace.TryGetValue(match.Value.ToLower(), out var replacement))
                return isUpperCase ? replacement.ToUpper() : replacement;
            return match.Value;
        });
    }
}
