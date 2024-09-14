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
    private Dictionary<int, (double k1, double k2, double k3, double k4, double k5, double T3, double T4)> Variants;

    [Inject]
    public HttpClient Http { get; set; }

    private double k1 { get; set; }
    private double k2 { get; set; }
    private double k3 { get; set; }
    private double k4 { get; set; }
    private double k5 { get; set; }
    private double T3 { get; set; }
    private double T4 { get; set; }
    private string variant { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadVariantsAsync();
    }

    private async Task LoadVariantsAsync()
    {
        try
        {
            string fileContent = await Http.GetStringAsync("variants.txt");

            Variants = new Dictionary<int, (double k1, double k2, double k3, double k4, double k5, double T3, double T4)>();
            string[] lines = fileContent.Split('\n');

            foreach (string line in lines)
            {
                string[] parts = line.Split(' ');

                if (parts.Length == 8 && int.TryParse(parts[0], out int key))
                {
                    Variants[key] = (
                        double.Parse(parts[1], CultureInfo.InvariantCulture),
                        double.Parse(parts[2], CultureInfo.InvariantCulture),
                        double.Parse(parts[3], CultureInfo.InvariantCulture),
                        double.Parse(parts[4], CultureInfo.InvariantCulture),
                        double.Parse(parts[5], CultureInfo.InvariantCulture),
                        double.Parse(parts[6], CultureInfo.InvariantCulture),
                        double.Parse(parts[7], CultureInfo.InvariantCulture)
                    );
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading variants: {ex.Message}");
        }
    }

    private void Submit()
    {
        if (!string.IsNullOrEmpty(variant))
        {
            if (Variants.TryGetValue(int.Parse(variant), out (double k1, double k2, double k3, double k4, double k5, double T3, double T4) values))
            {
                k1 = values.k1;
                k2 = values.k2;
                k3 = values.k3;
                k4 = values.k4;
                k5 = values.k5;
                T3 = values.T3;
                T4 = values.T4;
            }
        }

        a0 = k1 * k2 * k3 * k4 / (T3 * T4);
        a1 = (1 + k3 * k5) / (T3 * T4);
        a2 = (T4 * (k3 * k5 + 1) + T3) / (T3 * T4);
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
