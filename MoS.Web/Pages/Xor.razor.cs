using Microsoft.AspNetCore.Components;

namespace MoS.Web.Pages;

public partial class Xor
{
    private readonly Dictionary<int, char> _altCodes = new();
    private readonly List<Calc> _table = [];
    public string InputSymbol { get; set; } = string.Empty;
    public string InputAltCode { get; set; } = string.Empty;
    public string InputWord { get; set; } = string.Empty;
    public int ActiveTabIndex { get; set; } = 2;
    public int ResultTabIndex { get; set; }
    public int NumberOfRounds { get; set; } = 2;
    public string KeyShifts { get; set; } = "1 1";
    public bool IsEncryptMode { get; set; }
    private Dictionary<string, string> Results { get; } = new();

    [Inject]
    private HttpClient Client { get; set; } = default!;

    public static string To2(int x)
    {
        return Convert.ToString(x, 2).PadLeft(4, '0');
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAltCodesAsync();
    }

    private async Task LoadAltCodesAsync()
    {
        string fileContent = await Client.GetStringAsync("data/alt-codes.txt");
        string[] lines = fileContent.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string[] parts = line.Split(' ');

            if (parts.Length == 2 && int.TryParse(parts[0], out int code))
            {
                _altCodes[code] = parts[1][0];
            }
        }
    }

    private string Shift(string a, int n)
    {
        n %= a.Length;

        if (n < 0)
        {
            n = a.Length + n;
        }

        return a[^n..] + a[..^n];
    }

    private string XorStrings(string a, string b)
    {
        return string.Concat(a.Zip(b, (x, y) => (int.Parse(x.ToString()) ^ int.Parse(y.ToString())).ToString()));
    }

    private void Process()
    {
        try
        {
            Results.Clear();
            _table.Clear();

            if (ActiveTabIndex == 0)
            {
                if (InputSymbol.Length != 1)
                {
                    Results["Ошибка"] = "Ошибка: Введите только один символ.\n";
                    return;
                }

                ProcessSingleSymbol(_altCodes.First(x => x.Value == InputSymbol[0]).Key, InputSymbol);
            }
            else if (ActiveTabIndex == 1)
            {
                if (!int.TryParse(InputAltCode, out int code) || !_altCodes.ContainsKey(code))
                {
                    Results["Ошибка"] = "Ошибка: Введите правильный ALT-код.\n";
                    return;
                }

                ProcessSingleSymbol(code, _altCodes[code].ToString());
            }
            else if (ActiveTabIndex == 2)
            {
                if (string.IsNullOrWhiteSpace(InputWord))
                {
                    Results["Ошибка"] = "Ошибка: Введите слово.\n";
                    return;
                }

                foreach (char c in InputWord)
                {
                    ProcessSingleSymbol(_altCodes.FirstOrDefault(x => x.Value == c).Key, c.ToString());
                }
            }
        }
        catch (Exception)
        {
            Results["Ошибка"] = "Ошибка: Введите правильный ALT-код.\n";
        }
    }

    private void ProcessSingleSymbol(int code, string symbol)
    {
        string result = string.Empty;
        result += $"Символ: {symbol}\n";
        result += $"Его ALT-код: {code}\n";

        result += $"Изначальный код, k = {code}\n";
        string binCode = Convert.ToString(code, 2).PadLeft(8, '0');
        result += "Код в 2СС: " + binCode + "\n";

        string leftHalf = binCode[..4];
        string rightHalf = binCode[4..];

        List<int> keyShifts = KeyShifts.Split(' ').Select(int.Parse).ToList();

        if (!IsEncryptMode)
        {
            keyShifts.Reverse();
        }

        result += "------------\n";

        for (int i = 0; i < NumberOfRounds; i++)
        {
            int k = keyShifts[i % keyShifts.Count];
            result += $"Раунд {i + 1}, k = {k}\n";
            result += $"Левая часть = {leftHalf}\n";
            result += $"Правая часть = {rightHalf}\n";

            string shiftedLeftHalf = Shift(leftHalf, k);
            result += $"Сдвигаем левую часть на k = {k} ..\n";
            result += $"Результат сдвига = {shiftedLeftHalf}\n";

            result += "Выполняем XOR с правой частью..\n";
            rightHalf = XorStrings(shiftedLeftHalf, rightHalf);
            result += $"Результат XOR = {rightHalf}\n";

            if (i != NumberOfRounds - 1)
            {
                (leftHalf, rightHalf) = (rightHalf, leftHalf);
            }

            result += "Результат раунда: " + leftHalf + rightHalf + "\n";
            result += "------------\n";
        }

        string finalBinaryResult = leftHalf + rightHalf;
        result += "Результат финального раунда: " + finalBinaryResult + "\n";

        int finalResult = Convert.ToInt32(finalBinaryResult, 2);
        char finalChar = _altCodes[finalResult];

        result += $"Новый код символа: {finalResult} (ALT-код: {finalResult}, Символ: {finalChar})\n";
        _table.Add(new Calc(symbol, code, binCode, To2(finalResult), finalResult, finalChar));
        Results[symbol] = result;
    }

    private record Calc(string Symbol, int Code, string BinCode, string S, int FinalResult, char? FinalChar);
}
