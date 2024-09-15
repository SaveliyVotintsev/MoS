using System.Numerics;
using MathNet.Numerics;
using MoS.Web.Components;
using MoS.Web.Models;

namespace MoS.Web.Pages;

public partial class CalculationForm
{
    private const int Decimals = 6;

    private CalculateData? _calculateData;
    private CalculateResult? _calculateResult;

    private bool _resultsAvailable;
    private string? _errorMessage;
    private InputForm? _inputForm;

    private void Submit()
    {
        try
        {
            CalculateResults();
        }
        catch (Exception exception)
        {
            _resultsAvailable = false;
            _errorMessage = $"Ошибка при расчете: {exception.Message}";
        }

        _resultsAvailable = true;
        _errorMessage = null;
    }

    private void SetVariantValues(VariantData values)
    {
        _inputForm?.SetData(values);
    }

    private void CalculateResults()
    {
        if (_inputForm == null)
        {
            return;
        }

        (double k11, double k21, double k31, double k41, double k51, double T31, double T41) = _inputForm.GetData();

        double a0 = k11 * k21 * k31 * k41 / (T31 * T41);
        double a1 = (1 + k31 * k51) / (T31 * T41);
        double a2 = (T41 * (k31 * k51 + 1) + T31) / (T31 * T41);
        double a3 = 1;
        double b0 = a0;

        _calculateData = new CalculateData(a0, a1, a2, a3, b0);

        Polynomial polynomial = new(a0, a1, a2, a3);
        Polynomial differentiate = polynomial.Differentiate();

        Complex[] roots = polynomial.Roots().OrderByDescending(complex => complex.IsReal()).ToArray();
        Complex[] derivatives = roots.Select(x => differentiate.Evaluate(x)).ToArray();
        Complex[] hBezEList = roots.Zip(derivatives, (a, b) => ComplexFormatter.HBezE(b0, a, b)).ToArray();

        int length = roots.All(x => x.IsReal()) ? roots.Length : roots.Length - 1;
        string[] eh = roots.Zip(hBezEList, (a, b) => ComplexFormatter.ToEHAlt(b, a)).Take(length).ToArray();

        string result = $"1 {string.Join(" ", eh.Select(e => e.StartsWith('-') ? e : $"+ {e}"))}".Replace(",", ".");

        _calculateResult = new CalculateResult(roots, derivatives, hBezEList, eh, result);
    }
}
