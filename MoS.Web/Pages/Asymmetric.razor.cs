namespace MoS.Web.Pages;

public partial class Asymmetric
{
    private readonly List<KeyResult> _results = [];
    private int _kMin = 1;
    private int _kMax = 20;
    private int _eMin = 3;
    private int _eMax = 99;

    private string _errorMessage = string.Empty;
    private double _progress;
    private int PrimeP { get; set; } = 23;
    private int PrimeQ { get; set; } = 29;

    private int KMin
    {
        get => _kMin;
        set
        {
            if (value > _kMax)
            {
                _kMax = value;
            }

            _kMin = value;
        }
    }

    private int KMax
    {
        get => _kMax;
        set
        {
            if (value < _kMin)
            {
                _kMin = value;
            }

            _kMax = value;
        }
    }

    private int EMin
    {
        get => _eMin;
        set
        {
            if (value > _eMax)
            {
                _eMax = value;
            }

            _eMin = value;
        }
    }

    private int EMax
    {
        get => _eMax;
        set
        {
            if (value < _eMin)
            {
                _eMin = value;
            }

            _eMax = value;
        }
    }

    public async Task CalculateKeys()
    {
        _errorMessage = string.Empty;

        if (!IsPrime(PrimeP) || !IsPrime(PrimeQ))
        {
            _errorMessage = "Числа P и Q должны быть простыми.";
            return;
        }

        int modulus = PrimeP * PrimeQ;
        int phi = (PrimeP - 1) * (PrimeQ - 1);
        int totalCombinations = (KMax - KMin + 1) * ((EMax - EMin) / 2 + 1);
        int currentIteration = 0;

        _results.Clear();
        _progress = 0;

        await Task.Run(() =>
        {
            for (int k = KMin; k <= KMax; k++)
            {
                for (int e = EMin % 2 == 0 ? EMin + 1 : EMin; e <= EMax; e += 2)
                {
                    if (Gcd(e, phi) != 1)
                    {
                        continue;
                    }

                    double privateKeyCandidate = CalculatePrivateKey(k, e, phi);
                    double fractionalPart = privateKeyCandidate - Math.Truncate(privateKeyCandidate);

                    if (fractionalPart is < 0.00001 and > -0.0000001)
                    {
                        _results.Add(new KeyResult
                        {
                            Modulus = modulus,
                            Phi = phi,
                            PublicKey = e,
                            PrivateKey = (int)privateKeyCandidate,
                            K = k,
                            D = privateKeyCandidate,
                        });
                    }

                    currentIteration++;
                    _progress = (double)currentIteration / totalCombinations * 100;
                }
            }
        });

        _progress = 100;
    }

    private double CalculatePrivateKey(int k, int e, int phi)
    {
        return (k * phi + 1) / (double)e;
    }

    private bool IsPrime(int number)
    {
        if (number < 2)
        {
            return false;
        }

        for (int i = 2; i <= Math.Sqrt(number); i++)
        {
            if (number % i == 0)
            {
                return false;
            }
        }

        return true;
    }

    private int Gcd(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }

        return a;
    }

    public class KeyResult
    {
        public int Modulus { get; set; }
        public int Phi { get; set; }
        public int PublicKey { get; set; }
        public int PrivateKey { get; set; }
        public int K { get; set; }
        public double D { get; set; }
    }
}
