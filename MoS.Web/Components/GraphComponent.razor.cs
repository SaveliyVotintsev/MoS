using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MoS.Web.Components;

public partial class GraphComponent
{
    private const double DefaultStep = 0.1;
    private const double DefaultMaxT = 20;

    private const string StepKey = "GraphStep";
    private const string MaxTKey = "GraphMaxT";

    private string _functionString = string.Empty;

    private double _step = DefaultStep;
    private double _maxT = DefaultMaxT;

    public double Step
    {
        get => _step;
        set
        {
            _step = value;
            _ = Refresh();
        }
    }

    public double MaxT
    {
        get => _maxT;
        set
        {
            _maxT = value;
            _ = Refresh();
        }
    }

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    [Inject]
    private ILocalStorageService LocalStorage { get; set; } = null!;

    public async Task Generate(string functionString, bool force = false)
    {
        if (force == false && functionString.Equals(_functionString, StringComparison.InvariantCultureIgnoreCase))
        {
            return;
        }

        _functionString = functionString;
        await JsRuntime.InvokeVoidAsync("updateChart", _functionString, Step, MaxT);
        StateHasChanged();
    }

    protected override async Task OnInitializedAsync()
    {
        await JsRuntime.InvokeVoidAsync("deleteChart");
    }

    protected override async Task OnParametersSetAsync()
    {
        await LoadStateAsync();
    }

    private async Task Refresh()
    {
        await Generate(_functionString, true);
        await SaveStateAsync();
    }

    private void Restore()
    {
        Step = DefaultStep;
        MaxT = DefaultMaxT;
    }

    private async Task SaveStateAsync()
    {
        await LocalStorage.SetItemAsync(StepKey, _step);
        await LocalStorage.SetItemAsync(MaxTKey, _maxT);
    }

    private async Task LoadStateAsync()
    {
        Step = await LocalStorage.GetItemAsync<double?>(StepKey) ?? DefaultStep;
        MaxT = await LocalStorage.GetItemAsync<double?>(MaxTKey) ?? DefaultMaxT;
    }
}
