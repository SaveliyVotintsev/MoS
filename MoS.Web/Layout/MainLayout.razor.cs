using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace MoS.Web.Layout;

public partial class MainLayout
{
    private const string LocalStorageKey = "IsDarkMod";

    private bool _drawerOpen = true;
    private bool _isDarkMode = true;
    private MudTheme? _theme;

    private MudThemeProvider _mudThemeProvider = null!;

    public string DarkLightModeButtonIcon => _isDarkMode switch
    {
        true => Icons.Material.Rounded.AutoMode,
        false => Icons.Material.Outlined.DarkMode,
    };

    [Inject]
    private ILocalStorageService LocalStorage { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _mudThemeProvider.WatchSystemPreference(OnSystemPreferenceChanged);
            StateHasChanged();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        bool? isDarkMod = await LocalStorage.GetItemAsync<bool?>(LocalStorageKey);
        _isDarkMode = isDarkMod ?? true;

        _theme = new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = Colors.Blue.Darken1,
                AppbarBackground = Colors.Blue.Darken1,
            },
            PaletteDark = new PaletteDark
            {
                Primary = Colors.Blue.Default,
            },
        };
    }

    private Task OnSystemPreferenceChanged(bool newValue)
    {
        _isDarkMode = newValue;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void DrawerToggle()
    {
        _drawerOpen = !_drawerOpen;
    }

    private async Task DarkModeToggle()
    {
        _isDarkMode = !_isDarkMode;
        await LocalStorage.SetItemAsync(LocalStorageKey, _isDarkMode);
    }
}
