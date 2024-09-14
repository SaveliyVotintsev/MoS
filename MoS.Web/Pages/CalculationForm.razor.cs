using System.Globalization;
using System.Numerics;
using MathNet.Numerics;
using Microsoft.AspNetCore.Components;

namespace MoS.Web.Pages;

public partial class CalculationForm
{
    private double b0;
    private double a0;
    private double a1;
    private double a2;
    private double a3;
    private string[] Roots;
    private string[] Derivatives;
    private string[] hBezEList;
    private string[] eh;
    private bool ResultsAvailable;
    private Dictionary<int, (double k11, double k21, double k31, double k41, double k51, double T31, double T41)> Variants;
    private string? errorMessage;

    [Inject]
    public HttpClient Http { get; set; }

    [Inject]
    public ILogger<CalculationForm> _logger { get; set; }

    private double k11 { get; set; }
    private double k21 { get; set; }
    private double k31 { get; set; }
    private double k41 { get; set; }
    private double k51 { get; set; }
    private double T31 { get; set; }
    private double T41 { get; set; }
    private string variant { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadVariantsAsync();
    }

    private async Task LoadVariantsAsync()
    {
        try
        {
            _logger.LogTrace("Начало загрузки вариантов");

            _logger.LogTrace("Попытка получить содержимое файла 'variants.txt'");
            string fileContent = await Http.GetStringAsync("variants.txt");
            _logger.LogTrace("Содержимое файла успешно получено");

            _logger.LogTrace("Инициализация словаря Variants");
            Variants = new Dictionary<int, (double k11, double k21, double k31, double k41, double k51, double T31, double T41)>();

            _logger.LogTrace("Разделение содержимого файла на строки");
            string[] lines = fileContent.Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            _logger.LogTrace("Обработка каждой строки");

            foreach (string line in lines)
            {
                _logger.LogTrace("Обработка строки: {Line}", line);
                string[] parts = line.Split(' ');

                if (parts.Length == 8 && int.TryParse(parts[0], out int key))
                {
                    _logger.LogTrace("Парсинг частей для ключа: {Key}", key);

                    Variants[key] = (
                        double.Parse(parts[1], CultureInfo.InvariantCulture),
                        double.Parse(parts[2], CultureInfo.InvariantCulture),
                        double.Parse(parts[3], CultureInfo.InvariantCulture),
                        double.Parse(parts[4], CultureInfo.InvariantCulture),
                        double.Parse(parts[5], CultureInfo.InvariantCulture),
                        double.Parse(parts[6], CultureInfo.InvariantCulture),
                        double.Parse(parts[7], CultureInfo.InvariantCulture)
                    );

                    _logger.LogTrace("Успешно добавлен вариант для ключа: {Key}", key);
                }
                else
                {
                    _logger.LogTrace("Строка не содержит корректных данных или имеет неправильный формат: {Line}", line);
                }
            }

            _logger.LogTrace("Варианты успешно загружены");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при загрузке вариантов");
        }
    }

    private void Submit()
    {
        try
        {
            if (!string.IsNullOrEmpty(variant))
            {
                if (Variants.TryGetValue(int.Parse(variant), out (double k11, double k21, double k31, double k41, double k51, double T31, double T41) values))
                {
                    k11 = values.k11;
                    k21 = values.k21;
                    k31 = values.k31;
                    k41 = values.k41;
                    k51 = values.k51;
                    T31 = values.T31;
                    T41 = values.T41;
                }
            }

            a0 = k11 * k21 * k31 * k41 / (T31 * T41);
            a1 = (1 + k31 * k51) / (T31 * T41);
            a2 = (T41 * (k31 * k51 + 1) + T31) / (T31 * T41);
            a3 = 1;
            b0 = a0;

            Polynomial polynomial = new(a0, a1, a2, a3);
            Complex[] p = polynomial.Roots();

            Roots = p.Select(root => ComplexFormatter.FormatComplex(root)).ToArray();

            Complex[] A = new Complex[p.Length];

            for (int i = 0; i < p.Length; i++)
            {
                A[i] = PolynomialDerivative.Proizvodnaya(p[i], a3, a2, a1);
            }

            Derivatives = A.Select(a => ComplexFormatter.ToE(a)).ToArray();

            Complex[] hBezEListArray = new Complex[p.Length];

            for (int i = 0; i < p.Length; i++)
            {
                hBezEListArray[i] = PolynomialDerivative.HBezE(b0, p[i], PolynomialDerivative.Proizvodnaya(p[i], a3, a2, a1));
            }

            hBezEList = hBezEListArray.Select(h => h.ToString()).ToArray();

            eh = new string[p.Length - 1];

            for (int i = 0; i < p.Length - 1; i++)
            {
                eh[i] = ComplexFormatter.ToEH(hBezEListArray[i], p[i]);
            }

            ResultsAvailable = true;
            errorMessage = null;
        }
        catch (Exception ex)
        {
            ResultsAvailable = false;
            errorMessage = $"Ошибка при расчете: {ex.Message}";
        }
    }

    public static class ComplexFormatter
    {
        public static string FormatComplex(Complex c, int decimals = 6)
        {
            string realPart = c.Real.ToString($"F{decimals}");
            string imagPart = c.Imaginary.ToString($"F{decimals}");
            return c.Imaginary >= 0 ? $"{realPart} + {imagPart}i" : $"{realPart} - {Math.Abs(c.Imaginary).ToString($"F{decimals}")}i";
        }

        public static string ToE(Complex val)
        {
            double a = val.Real;
            double b = val.Imaginary;
            double magnitude = Math.Sqrt(a * a + b * b);
            double angle = Math.Atan(b / a);
            return $"{magnitude} * e^(i * {angle * 180 / Math.PI})";
        }

        public static string ToEH(Complex val, Complex pi)
        {
            double a = val.Real;
            double b = val.Imaginary;
            double magnitude = Math.Sqrt(a * a + b * b);
            double angle = Math.Atan(b / a);

            if (b == 0)
            {
                return $"{Math.Sign(a) * magnitude:F6} * e^({pi.Real:F6} * t)";
            }

            return $"{Math.Sign(a) * 2 * magnitude:F6} * e^({pi.Real:F6} * t) * cos({Math.Abs(pi.Imaginary):F6} * t + {angle:F6})";
        }
    }

    public static class PolynomialDerivative
    {
        public static Complex Proizvodnaya(Complex p, double a3, double a2, double a1)
        {
            return a3 * 3 * p * p + a2 * 2 * p + a1;
        }

        public static Complex HBezE(double b, Complex p, Complex proiz)
        {
            return b / (p * proiz);
        }
    }
}
