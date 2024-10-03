using System.Numerics;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace MoS.Web.Components;

public partial class AsymmetricCrypt
{
    private readonly Dictionary<int, char> _altCodes = new();
    public string PublicKeyModulus { get; set; }
    public string PublicKeyExponent { get; set; }
    public string PrivateKeyModulus { get; set; }
    public string PrivateKeyExponent { get; set; }
    private string Plaintext { get; set; }
    private string Ciphertext { get; set; }
    private string EncryptedResult { get; set; }
    private string DecryptedResult { get; set; }
    private string _errorMessage { get; set; }

    [Inject]
    private HttpClient Client { get; set; } = default!;

    public void Encrypt()
    {
        try
        {
            if (!BigInteger.TryParse(PublicKeyModulus, out BigInteger modulus) || !BigInteger.TryParse(PublicKeyExponent, out BigInteger exponent))
            {
                _errorMessage = "Invalid public key.";
                return;
            }


            byte[] bytesToEncrypt = Encoding.UTF8.GetBytes(Plaintext.Select(x=>_altCodes.GetValueOrDefault(x,'\0')));
            BigInteger plaintextInt = new(bytesToEncrypt);
            BigInteger encrypted = BigInteger.ModPow(plaintextInt, exponent, modulus);
            EncryptedResult = encrypted.ToString();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error in encryption: {ex.Message}";
        }
    }

    public void Decrypt()
    {
        try
        {
            if (!BigInteger.TryParse(PrivateKeyModulus, out BigInteger modulus) || !BigInteger.TryParse(PrivateKeyExponent, out BigInteger exponent))
            {
                _errorMessage = "Invalid private key.";
                return;
            }

            BigInteger encrypted = BigInteger.Parse(Ciphertext);
            BigInteger decrypted = BigInteger.ModPow(encrypted, exponent, modulus);
            byte[] decryptedBytes = decrypted.ToByteArray();
            DecryptedResult = Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error in decryption: {ex.Message}";
        }
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
}
