using Microsoft.JSInterop;
using MudBlazor;

namespace MoS.Web.Services;

public interface IClipboardService
{
    ValueTask CopyToClipboard(string text);
}

public class ClipboardService(IJSRuntime jsRuntime, ISnackbar snackbar) : IClipboardService
{
    private const int MaxLenght = 10;

    public ValueTask CopyToClipboard(string text)
    {
        bool isIncrease = text.Length > MaxLenght;
        int lenght = isIncrease ? MaxLenght : text.Length;
        snackbar.Add($"Скопировано {text[..lenght]}{(isIncrease ? "..." : "")}", Severity.Success);
        return jsRuntime.InvokeVoidAsync("navigator.clipboard.writeText", text);
    }
}
