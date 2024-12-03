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
    private CalculateData? _calculateData;
    private CalculateResult? _calculateResult;

    private bool _resultsAvailable;
    private string? _errorMessage;

    private InputForm? _inputForm;
    private GraphComponent? _graph;

    private AsyncExpression? _expression;
    private List<double>? _steps;
    private Result? _analysisResult;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = default!;

    private int Decimals { get; set; } = 6;
    private string Format => $"F{Decimals}";

    private static (double t1, double y1, double t2, double y2) FindBoundaryValues(double[] values, List<double> steps)
    {
        double t1 = 0, y1 = 0, t2 = 0, y2 = 0;

        for (int i = values.Length - 2; i >= 0; i--)
        {
            if (values[i] is <= 1.05 and >= 0.95)
            {
                continue;
            }

            t1 = steps[i];
            t2 = steps[i + 1];
            y1 = values[i];
            y2 = values[i + 1];
            break;
        }

        return (t1, y1, t2, y2);
    }

    private static double CalculateEstablishedAverage(double[] values)
    {
        double established = values.Sum();
        return established / values.Length;
    }

    private async Task Submit()
    {
        _resultsAvailable = false;
        StateHasChanged();

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
        _errorMessage = null;
        StateHasChanged();
        await ReloadFormulas();
    }

    private async Task ReloadFormulas()
    {
        if (_analysisResult != null)
        {
            _analysisResult.Formulas = UpdateFormulas(_analysisResult);
        }

        await JsRuntime.InvokeVoidAsync("resetMath");
        StateHasChanged();
    }

    private void SetVariantValues(VariantData values)
    {
        _inputForm?.SetData(values);
        StateHasChanged();
    }

    private async Task CalculateResults()
    {
        if (_inputForm == null)
        {
            return;
        }

        (double k11, double k21, double k31, double k41, double k51, double t31, double t41) = _inputForm.GetData();

        double a0 = k11 * k21 * k31 * k41 / (t31 * t41);
        double a1 = (1 + k31 * k51) / (t31 * t41);
        double a2 = (t41 * (k31 * k51 + 1) + t31) / (t31 * t41);
        double a3 = 1;
        double b0 = a0;

        _calculateData = new CalculateData(a0, a1, a2, a3, b0);

        Polynomial polynomial = new(a0, a1, a2, a3);
        Polynomial differentiate = polynomial.Differentiate();

        Complex[] roots = polynomial.Roots().OrderByDescending(complex => complex.IsReal()).ToArray();
        Complex[] derivatives = roots.Select(x => differentiate.Evaluate(x)).ToArray();
        Complex[] hBezEList = roots.Zip(derivatives, (a, b) => ComplexFormatter.HBezE(b0, a, b)).ToArray();

        int length = roots.All(x => x.IsReal()) ? roots.Length : roots.Length - 1;
        string[] eh = roots.Zip(hBezEList, (a, b) => ComplexFormatter.ToEHAlt(b, a, Decimals)).Take(length).ToArray();

        string result = $"1 {string.Join(" ", eh.Select(e => e.StartsWith('-') ? e : $"+ {e}"))}".Replace(",", ".");

        _expression = new AsyncExpression(result, ExpressionOptions.IterateParameters | ExpressionOptions.IgnoreCaseAtBuiltInFunctions, CultureInfo.InvariantCulture);
        _steps = [];

        if (_graph != null)
        {
            for (double i = 0; i <= _graph.MaxT; i += _graph.Step)
            {
                _steps.Add(i);
            }
        }

        _expression.Parameters["t"] = _steps.ToArray();

        _calculateResult = new CalculateResult(roots, derivatives, hBezEList, eh, result);
        await Analyze();

        if (_graph != null)
        {
            await _graph.Generate(_calculateResult.Result, true);
        }

        StateHasChanged();
    }

    private async Task Analyze()
    {
        if (_steps == null || _expression == null || await _expression.EvaluateAsync() is not List<object> rawValues)
        {
            return;
        }

        double[] values = rawValues.Cast<double>().ToArray();

        double yMax = values.Max();
        (double t1, double y1, double t2, double y2) = FindBoundaryValues(values, _steps);

        double y = y1 > 1.05 ? 1.05 : 0.95;
        double tps = (t2 * y1 - t1 * y2 - t2 * y + t1 * y) / (y1 - y2);
        double[] establishedValues = values[^3..];
        double established = CalculateEstablishedAverage(establishedValues);
        double overshoot = (yMax - established) / established * 100d;

        _analysisResult = new Result(t1, y1, t2, y2,
            yMax, tps, overshoot, established, y)
        {
            EstablishedValues = establishedValues,
        };

        _analysisResult.Formulas = UpdateFormulas(_analysisResult);
    }

    private Formulas UpdateFormulas(Result analysisResult)
    {
        (double t1, double y1, double t2, double y2, double yMax, double tps, double overshoot, double established, double y) = analysisResult;
        double[] establishedValues = analysisResult.EstablishedValues;

        string timeRegulationFormula =
            $$"""
              $$     
              t_p = 
              \frac{t_2 \cdot y_1 - t_1 \cdot y_2 - t_2 \cdot y + t_1 \cdot y}{y_1 - y_2} = 
              \frac{{{t2.ToString(Format)}} \cdot {{y1.ToString(Format)}} - {{t1.ToString(Format)}} \cdot {{y2.ToString(Format)}} - {{t2.ToString(Format)}} \cdot {{y.ToString(Format)}} + {{t1.ToString(Format)}} \cdot {{y.ToString(Format)}}}{{{y1.ToString(Format)}} - {{y2.ToString(Format)}}}
              = {{tps.ToString(Format)}}
              $$
              """;

        string overshootFormula =
            $$$"""
               $$
               \sigma = \frac{y_{\text{макс}} - y_\text{уст}}{y_\text{уст}} \times 100\% =
               \sigma = \frac{{{{yMax.ToString(Format)}}} - {{{established.ToString(Format)}}}}{{{{established.ToString(Format)}}}} \times 100\% =
               {{{overshoot.ToString(Format)}}}\%
               $$
               """;

        string steadyStateFormula =
            $$$"""
               $$
               h = \frac{{1}}{{n}} \sum_{{i=1}}^{{n}} y_i = 
               \frac{{{{string.Join('+', establishedValues.Select(x => x.ToString(Format)))}}}}{{{{establishedValues.Length}}}} = 
               {{{established.ToString(Format)}}}
               $$
               """;

        Formulas formulas = new(new MarkupString(timeRegulationFormula),
            new MarkupString(overshootFormula),
            new MarkupString(steadyStateFormula));

        return formulas;
    }

    private record Result(double T1, double Y1, double T2, double Y2, double YMax, double Tps, double Overshoot, double Established, double Y)
    {
        public Formulas Formulas { get; set; }
        public double[] EstablishedValues { get; set; }
    }

    private record Formulas(MarkupString TimeRegulation, MarkupString Overshoot, MarkupString SteadyState);
}
