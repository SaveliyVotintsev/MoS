using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace MoS.Web.Pages;

public partial class AdequacyAssessment
{
    private readonly List<Result> _results = [];
    private readonly List<SensitivityResult> _sensitivityResults = [];

    private readonly Component[] components =
    [
        new("k_1"),
        new("k_2"),
        new("k_3"),
        new("k_4"),
        new("k_5"),
        new("T_3"),
        new("T_4"),
    ];

    private List<(Experience[], string)> _experiences = [];
    private Experience[]? _experiencesModel;
    private Experience[]? _experiencesReference;
    private Experience[]? _experiencesModelSensitivity;
    private string? _inputData;
    private string? _inputComponents;

    [Inject]
    private IJSRuntime JS { get; set; }

    private Experience[]? AddExperiments(int requiredCount = 10)
    {
        if (_inputData == null)
        {
            return null;
        }

        List<Experience> experiences = [];

        string[] lines = _inputData.Replace(",", ".").Split(["\r\n", "\r", "\n", Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string[] values = line.Split([" ", "\t"], StringSplitOptions.RemoveEmptyEntries);

            if (values.Length == 5
                && double.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double yMax)
                && double.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double t1)
                && double.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double y1)
                && double.TryParse(values[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double t2)
                && double.TryParse(values[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double y2))
            {
                Experience experience = new(yMax, t1, y1, t2, y2);
                experiences.Add(experience);
                Console.WriteLine(experience);
            }
        }

        if (experiences.Count != requiredCount)
        {
            return null;
        }

        _inputData = string.Empty;
        return experiences.ToArray();
    }

    private void AddComponents()
    {
        if (_inputComponents == null)
        {
            return;
        }

        string[] lines = _inputComponents.Replace(",", ".").Split(["\r\n", "\r", "\n", Environment.NewLine], StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length != components.Length)
        {
            return;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string[] values = line.Split([" ", "\t"], StringSplitOptions.RemoveEmptyEntries);

            if (values.Length == 2
                && double.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double min)
                && double.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double max))
            {
                components[i].Min = min;
                components[i].Max = max;
            }
        }

        _inputComponents = string.Empty;
    }

    private void CheckingModelAdequacy()
    {
        if (_experiencesModel == null || _experiencesReference == null)
        {
            return;
        }

        Interpolation(_experiencesModel);
        Interpolation(_experiencesReference);

        Overshoot(_experiencesModel);
        Overshoot(_experiencesReference);

        AdequacyAssessment("Перерегулирование", _experiencesModel.Select(x => x.overshoot).ToArray(), _experiencesReference.Select(x => x.overshoot).ToArray());
        AdequacyAssessment("t_p", _experiencesModel.Select(x => x.tps).ToArray(), _experiencesReference.Select(x => x.tps).ToArray());

        _experiences =
        [
            (_experiencesModel, "Таблица переходных характеристики математической модели"),
            (_experiencesReference, "Таблица переходных характеристики эталона"),
        ];

        CheckingModelSensitivity();
        return;

        void AdequacyAssessment(string parameter, double[] model, double[] reference)
        {
            int N2 = 10;
            int N1 = 10;

            Result result = new("Проверка адекватности модели по средним значениям откликов модели и системы", parameter, []);
            double Yn = 1d / N2 * model.Sum();
            result.Content.Add($"Yn = {Yn:F6}");

            double Ysn = 1d / N1 * reference.Sum();
            result.Content.Add($"Ysn = {Ysn:F6}");

            double Dn = 1d / (N2 - 1) * model.Select(Ynk => Math.Pow(Ynk - Yn, 2)).Sum();
            result.Content.Add($"Dn = {Dn:F6}");
            double Dsn = 1d / (N1 - 1) * reference.Select(Ysnk => Math.Pow(Ysnk - Ysn, 2)).Sum();
            result.Content.Add($"Dsn = {Dsn:F6}");
            double Dpn = ((N1 - 1) * Dn + (N2 - 1) * Dsn) / (N1 + N2 - 2);
            result.Content.Add($"Dpn = {Dpn:F6}");
            double tn = Math.Abs((Yn - Ysn) * Math.Sqrt(N1 * N2 / (Dpn * (N1 + N2))));
            result.Content.Add($"tn = {tn:F6}");

            int freedomDegrees = N1 + N2 - 2;
            double alpha = 0.05;
            double tkr = StudentDistribution(freedomDegrees, alpha);
            result.Content.Add($"{tn:F6} <= {tkr}: {tn <= tkr}");

            _results.Add(result);
            result = new Result("Проверка адекватности модели по дисперсиям отклонений откликов модели от среднего значения откликов системы", parameter, []);
            double Don = 1d / (N2 - 1) * model.Select(Ynk => Math.Pow(Ynk - Ysn, 2)).Sum();
            result.Content.Add($"Don = {Don:F6}");
            double F;
            int n1, n2;

            if (Don > Dsn)
            {
                F = Don / Dsn;
                n1 = N2;
                n2 = N1;
            }
            else
            {
                F = Dsn / Don;
                n1 = N1;
                n2 = N2;
            }

            result.Content.Add($"F = {F:F6}");
            double Fkr = FisherDistribution(n1, n2);

            result.Content.Add($"{F:F6} < {Fkr}: {F < Fkr}");
            _results.Add(result);

            result = new Result("Проверка адекватности модели по максимальному значению абсолютных отклонений откликов модели от откликов системы", parameter, []);

            double dYn = model.Zip(reference)
                             .Select(x =>
                             {
                                 (double Ynk, double Ysnk) = x;
                                 return Math.Abs(Ynk - Ysnk) / Ysn;
                             })
                             .Max()
                         * 100;

            result.Content.Add($"dYn = {dYn:F6}");

            double dYg = 20;
            result.Content.Add($"{dYn:F6} <= {dYg}: {dYn <= dYg}");
            _results.Add(result);
        }

        double FisherDistribution(int freedom1, int freedom2)
        {
            double[,] table =
            {
                { 18.51, 19.00, 19.16, 19.25, 19.30, 19.33, 19.35, 19.37, 19.38, 19.39, 19.40 },
                { 10.13, 9.55, 9.28, 9.12, 9.01, 8.94, 8.89, 8.85, 8.81, 8.79, 8.76 },
                { 7.71, 6.94, 6.59, 6.39, 6.26, 6.16, 6.09, 6.04, 6.00, 5.96, 5.94 },
                { 6.61, 5.79, 5.41, 5.19, 5.05, 4.95, 4.88, 4.82, 4.77, 4.74, 4.70 },
                { 5.99, 5.14, 4.76, 4.53, 4.39, 4.28, 4.21, 4.15, 4.10, 4.06, 4.03 },
                { 5.59, 4.74, 4.35, 4.12, 3.97, 3.87, 3.79, 3.73, 3.68, 3.64, 3.60 },
                { 5.32, 4.46, 4.07, 3.84, 3.69, 3.58, 3.50, 3.44, 3.39, 3.35, 3.31 },
                { 5.12, 4.26, 3.86, 3.63, 3.48, 3.37, 3.29, 3.23, 3.18, 3.14, 3.10 },
                { 4.96, 4.10, 3.71, 3.48, 3.33, 3.22, 3.14, 3.07, 3.02, 2.98, 2.94 },
                { 4.84, 3.98, 3.59, 3.36, 3.20, 3.09, 3.01, 2.95, 2.90, 2.85, 2.82 },
                { 4.75, 3.89, 3.49, 3.26, 3.11, 3.00, 2.91, 2.85, 2.80, 2.75, 2.72 },
            };

            List<int> freedoms1 = Enumerable.Range(1, 11).ToList();
            List<int> freedoms2 = Enumerable.Range(2, 11).ToList();

            int i = freedoms1.IndexOf(freedom1);
            int j = freedoms2.IndexOf(freedom2);

            return table[i, j];
        }

        double StudentDistribution(int freedom, double alpha)
        {
            double[,] table =
            {
                { 6.314, 12.706, 25.452, 31.821, 63.657 },
                { 2.920, 4.303, 6.205, 6.965, 9.925 },
                { 2.353, 3.182, 4.177, 4.541, 5.841 },
                { 2.132, 2.776, 3.495, 3.747, 4.604 },
                { 2.015, 2.571, 3.163, 3.365, 4.032 },
                { 1.943, 2.447, 2.969, 3.143, 3.707 },
                { 1.895, 2.365, 2.841, 2.998, 3.499 },
                { 1.860, 2.306, 2.752, 2.896, 3.355 },
                { 1.833, 2.262, 2.685, 2.821, 3.250 },
                { 1.812, 2.228, 2.634, 2.764, 3.169 },
                { 1.782, 2.179, 2.560, 2.681, 3.055 },
                { 1.761, 2.145, 2.510, 2.624, 2.977 },
                { 1.746, 2.120, 2.473, 2.583, 2.921 },
                { 1.734, 2.101, 2.445, 2.552, 2.878 },
                { 1.725, 2.086, 2.423, 2.528, 2.845 },
            };

            List<double> alphas = [0.10, 0.05, 0.025, 0.020, 0.010];
            List<int> freedomDegrees = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 12, 14, 16, 18, 20];

            int i = freedomDegrees.IndexOf(freedom);
            int j = alphas.IndexOf(alpha);

            return table[i, j];
        }
    }

    private void Interpolation(IEnumerable<Experience> experiences)
    {
        foreach (Experience experience in experiences)
        {
            (double t1, double y1, double t2, double y2) = experience;

            double y = y1 > 1.05 ? 1.05 : 0.95;
            experience.tps = (t2 * y1 - t1 * y2 - t2 * y + t1 * y) / (y1 - y2);
        }
    }

    private void Overshoot(IEnumerable<Experience> experiences)
    {
        foreach (Experience experience in experiences)
        {
            experience.overshoot = (experience.YMax - 1) / 1d * 100d;
        }
    }

    private void CheckingModelSensitivity()
    {
        if (_experiencesModelSensitivity == null)
        {
            return;
        }

        List<double> dX = components.Select(tuple =>
            {
                (double min, double max) = tuple;
                return (max - min) * 2 / (max + min) * 100;
            })
            .ToList();

        Overshoot(_experiencesModelSensitivity);
        Interpolation(_experiencesModelSensitivity);

        for (int i = 0; i < _experiencesModelSensitivity.Length; i++)
        {
            string component = components[i / 2].Name;

            if (i % 2 == 0)
            {
                component += "_min";
            }
            else
            {
                component += "_max";
            }

            _experiencesModelSensitivity[i].Component = component;
        }

        List<double> dYo = Sensitive(_experiencesModelSensitivity.Select(x => x.overshoot).ToArray());
        List<double> dYt = Sensitive(_experiencesModelSensitivity.Select(x => x.tps).ToArray());
        List<double> dYq1 = dYo.Zip(dYt).Select(n => Math.Sqrt(Math.Pow(n.First, 2) + Math.Pow(n.Second, 2))).ToList();
        List<double> dYq2 = dYo.Zip(dYt).Select(n => Math.Max(n.First, n.Second)).ToList();

        for (int i = 0; i < dX.Count; i++)
        {
            double dXq = dX[i];
            _sensitivityResults.Add(new SensitivityResult(components[i].Name, dXq, dYo[i], dYt[i], dYq1[i], dYq2[i]));
        }
    }

    private List<double> Sensitive(double[] Y)
    {
        List<double> dY = [];

        for (int i = 0; i < Y.Length - 1; i += 2)
        {
            double min = Y[i];
            double max = Y[i + 1];

            dY.Add(Math.Abs(max - min) * 2 / (max + min) * 100);
        }

        return dY;
    }

    private void Restore()
    {
        _inputData = """
                     1,25903	7,05	0,93832	7,37	0,9764
                     1,14155	7,69	0,93576	8,01	0,988
                     1,34611	12,17	0,94904	12,49	0,95569
                     1,27783	7,69	0,93318	8,01	0,98453
                     1,34475	7,69	0,93392	8,01	0,99908
                     1,30525	7,69	0,94573	8,01	0,97807
                     1,34047	7,37	0,90377	7,69	0,9856
                     1,31038	7,69	0,93724	8,01	0,98153
                     1,30455	7,05	0,86443	7,37	0,97797
                     1,38854	7,37	0,94441	7,69	0,98043

                     """;

        _experiencesModel = AddExperiments();

        _inputData = """
                     1,49119	4,49	1,10528	4,81	1,04984
                     1,42638	7,05	0,92838	7,37	0,97819
                     1,48836	7,05	0,92490	7,37	0,99392
                     1,25639	7,05	0,92291	7,37	0,99035
                     1,46000	7,05	0,92441	7,37	0,99035
                     1,34600	7,37	0,93887	7,69	0,97849
                     1,20367	7,05	0,92938	7,37	0,96103
                     1,46049	6,41	0,86582	6,37	0,99633
                     1,46000	7,37	0,92850	8,01	0,99879
                     1,26633	6,73	0,90711	7,05	0,99353

                     """;

        _experiencesReference = AddExperiments();

        _inputData = """
                     1,08077	6,09	1,05601	6,41	1,04326
                     1,33873	7,69	1,05192	8,01	1,04226
                     1,08077	6,09	1,05601	6,41	1,04326
                     1,33873	7,69	1,05192	8,01	1,04226
                     1,28671	5,45	1,05651	5,77	1,01615
                     1,25144	6,73	0,94507	7,05	0,95846
                     1,08077	6,09	1,05601	6,41	1,04326
                     1,33873	7,69	1,05192	8,01	1,04226
                     1,46504	9,61	0,93217	9,93	0,95097
                     1,15085	5,45	1,06469	5,77	1,03972
                     1,23651	4,49	1,09403	4,81	1,04847
                     1,29703	7,05	0,93917	7,37	0,9573
                     1,12699	3,53	1,05327	3,85	1,02578
                     1,32786	8,97	0,94273	9,29	0,96579

                     """;

        _experiencesModelSensitivity = AddExperiments(14);

        (double min, double max)[] componentsData =
        [
            (0.20, 0.60),
            (2.00, 6.00),
            (3.00, 9.00),
            (0.45, 1.35),
            (0.50, 1.50),
            (0.35, 1.05),
            (0.50, 1.50),
        ];

        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            (component.Min, component.Max) = componentsData[i];
        }
    }

    private void Reset()
    {
        _results.Clear();
        _sensitivityResults.Clear();
        _experiences.Clear();
        _experiencesModel = null;
        _experiencesReference = null;
        _experiencesModelSensitivity = null;
    }

    private async Task DownloadFileFromURL()
    {
        string fileName = "example.ods";
        string fileURL = "data/example.ods";
        await JS.InvokeVoidAsync("triggerFileDownload", fileName, fileURL);
    }

    private class Component(string name)
    {
        public string Name { get; } = name;
        public double Min { get; set; }
        public double Max { get; set; }

        public void Deconstruct(out double min, out double max)
        {
            min = Min;
            max = Max;
        }
    }

    private class Experience(double YMax, double t1, double y1, double t2, double y2)
    {
        public double YMax { get; } = YMax;
        public double t1 { get; } = t1;
        public double y1 { get; } = y1;
        public double t2 { get; } = t2;
        public double y2 { get; } = y2;
        public double overshoot { get; set; }
        public double tps { get; set; }
        public string Component { get; set; }

        public void Deconstruct(out double t1, out double y1, out double t2, out double y2)
        {
            t1 = this.t1;
            y1 = this.y1;
            t2 = this.t2;
            y2 = this.y2;
        }
    }

    private record SensitivityResult(string compomemt, double Xg, double Ysigma, double Ytp, double Yg1, double Yg2);

    private record Result(string Title, string parameter, List<string> Content);
}
