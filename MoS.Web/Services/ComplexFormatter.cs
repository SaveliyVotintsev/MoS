using System.Numerics;
using MathNet.Numerics;

namespace MoS.Web.Services;

public static class ComplexFormatter
{
    public static string FormatComplex(Complex complex, int decimals = 6)
    {
        string realPart = complex.Real.ToString($"F{decimals}");
        string imagPart = complex.Imaginary.ToString($"F{decimals}");

        return complex.Imaginary switch
        {
            > 0 => $"{realPart} + {imagPart}i",
            0 => $"{realPart}",
            var _ => $"{realPart} - {Math.Abs(complex.Imaginary).ToString($"F{decimals}")}i"
        };
    }

    public static string ToE(Complex complex, int decimals = 6)
    {
        if (complex.IsReal())
        {
            return complex.Real.ToString($"F{decimals}");
        }

        double a = complex.Real;
        double b = complex.Imaginary;
        double magnitude = Math.Sqrt(a * a + b * b);
        double angle = Math.Atan(b / a);
        return $"{magnitude.ToString($"F{decimals}")} * e^({(angle * 180 / Math.PI).ToString($"F{decimals}")} * i)";
    }

    public static string ToEHAlt(Complex val, Complex pi)
    {
        double real = val.Real;
        double imaginary = val.Imaginary;
        double magnitude = Math.Sqrt(real * real + imaginary * imaginary);
        double angle = Math.Atan(imaginary / real);

        if (imaginary == 0)
        {
            return $"{Math.Sign(real) * magnitude:F6} * exp({pi.Real:F6} * t)";
        }

        return $"{Math.Sign(real) * 2 * magnitude:F6} * exp({pi.Real:F6} * t) * cos({Math.Abs(pi.Imaginary):F6} * t + {angle:F6})";
    }

    public static Complex HBezE(double b, Complex p, Complex proiz)
    {
        return b / (p * proiz);
    }
}
