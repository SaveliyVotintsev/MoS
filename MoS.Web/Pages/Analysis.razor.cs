using MathNet.Numerics;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MoS.Web.Components;
using MoS.Web.Models;
using MoS.Web.Services;
using NCalc;
using System.Globalization;
using System.Numerics;

namespace MoS.Web.Pages;

public partial class Analysis
{
    private const string ResultId = "result";
    private const int Decimals = 6;

    private CalculateData? _calculateData;
    private CalculateResult? _calculateResult;

    private bool _isReloadRequired = true;
    private bool _resultsAvailable;
    private string? _errorMessage;
    private InputForm? _inputForm;
    private AsyncExpression _expression;
    private List<double> _ts;
    private Result? _res;
    private GraphComponent? _graph;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    private async Task Submit()
    {
        try
        {
            await CalculateResults();
        }
        catch (Exception exception)
        {
            _resultsAvailable = false;
            _errorMessage = $"Ошибка при расчете: {exception.Message}";
            StateHasChanged();
            return;
        }

        _resultsAvailable = true;
        _isReloadRequired = false;
        _errorMessage = null;
        StateHasChanged();
    }

    private void SetVariantValues(VariantData values)
    {
        _inputForm?.SetData(values);
        _isReloadRequired = true;
        StateHasChanged();
    }

    private async Task CalculateResults()
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

        _expression = new AsyncExpression(result, ExpressionOptions.IterateParameters | ExpressionOptions.IgnoreCaseAtBuiltInFunctions, CultureInfo.InvariantCulture);
        _ts = [];

        for (double i = 0; i <= _graph.MaxT; i += _graph.Step)
        {
            _ts.Add(i);
        }

        _expression.Parameters["t"] = _ts.ToArray();

        _calculateResult = new CalculateResult(roots, derivatives, hBezEList, eh, result);
        await Calc();
        await _graph.Generate(_calculateResult.Result, true);

        StateHasChanged();
    }

    private async Task Calc()
    {
        object? o = await _expression.EvaluateAsync();

        if (o is not List<object> a)
        {
            return;
        }

        double[] b = a.Cast<double>().ToArray();
        double YMax = b.Max();

        (double t1, double y1) = (0, 0);
        (double t2, double y2) = (0, 0);

        for (int i = b.Length - 2; i >= 0; i--)
        {
            if (b[i] is > 1.05 or < 0.95)
            {
                t1 = _ts[i];
                t2 = _ts[i + 1];

                y1 = b[i];
                y2 = b[i + 1];

                break;
            }
        }

        double y = y1 > 1.05 ? 1.05 : 0.95;
        double tps = (t2 * y1 - t1 * y2 - t2 * y + t1 * y) / (y1 - y2);
        double overshoot = (YMax - 1) / 1d * 100d;

        double established = 0;

        int count = 0;

        int end = Math.Max(0, b.Length - 3);

        for (int i = b.Length - 1; i >= end; i--)
        {
            count++;
            established += b[i];
        }

        established /= count;

        _res = new Result(t1, y1, t2, y2, YMax, tps, overshoot, established);
    }

    private record Result(double t1, double y1, double t2, double y2, double YMax, double tps, double overshoot, double established);
}
