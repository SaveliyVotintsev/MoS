using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace MoS.Web.Pages;

public partial class ProductionCalculator
{
    private const int Precision = 2;

    private readonly List<MachineEntry> _newMachineDates = [];
    private readonly List<MachineEntry> _removedMachineDates = [];
    private readonly List<string> _calculationSteps = [];

    private double _initialMachines;

    private DateTime? _newMachineDate;
    private double _newMachineCount;

    private DateTime? _removedMachineDate;
    private double _removedMachineCount;

    private double _downtimePercentage;
    private double _productivityPerMachine;
    private double _plannedProduction;

    private double _workingDays = 258;
    private double _shiftsCount = 2;
    private double _shiftDuration = 8;

    private CalculationResult? _result;

    [Parameter]
    public bool? LoadDefaults { get; set; } = false;

    [Inject]
    private IJSRuntime JsRuntime { get; set; } = null!;

    protected override void OnParametersSet()
    {
        if (LoadDefaults == true)
        {
            SetDefaultValues();
            Calculate();
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && LoadDefaults == true)
        {
           await Task.Delay(100)
               .ContinueWith(_ => JsRuntime.InvokeVoidAsync("scrollToElement", "result"))
               .ConfigureAwait(false);
        }
    }

    private void AddNewMachineDate()
    {
        if (_newMachineDate != default && _newMachineCount > 0)
        {
            _newMachineDates.Add(new MachineEntry { Date = _newMachineDate.Value, Count = _newMachineCount });
            _newMachineDate = default;
            _newMachineCount = 0;
        }
    }

    private void AddRemovedMachineDate()
    {
        if (_removedMachineDate != default && _removedMachineCount > 0)
        {
            _removedMachineDates.Add(new MachineEntry { Date = _removedMachineDate.Value, Count = _removedMachineCount });
            _removedMachineDate = default;
            _removedMachineCount = 0;
        }
    }

    private void RemoveNewMachineDate(MachineEntry entry)
    {
        _newMachineDates.Remove(entry);
    }

    private void RemoveRemovedMachineDate(MachineEntry entry)
    {
        _removedMachineDates.Remove(entry);
    }

    private void ClearValues()
    {
        _initialMachines = 0;
        _newMachineDates.Clear();
        _removedMachineDates.Clear();
        _newMachineDate = default;
        _newMachineCount = 0;
        _removedMachineDate = default;
        _removedMachineCount = 0;
        _workingDays = 258;
        _downtimePercentage = 0;
        _productivityPerMachine = 6;
        _plannedProduction = 0;
        _shiftsCount = 2;
        _shiftDuration = 8;
        _calculationSteps.Clear();
        _result = null;
    }

    private void SetDefaultValues()
    {
        _initialMachines = 120;
        _newMachineDates.Clear();
        _newMachineDates.Add(new MachineEntry { Date = new DateTime(DateTime.Now.Year, 11, 1), Count = 23 });
        _removedMachineDates.Clear();
        _removedMachineDates.Add(new MachineEntry { Date = new DateTime(DateTime.Now.Year, 5, 1), Count = 7 });
        _workingDays = 258;
        _downtimePercentage = 4;
        _productivityPerMachine = 6;
        _plannedProduction = 1700000;
        _shiftsCount = 2;
        _shiftDuration = 8;
        _calculationSteps.Clear();
        _result = null;
        Calculate();
    }

    private void Calculate()
    {
        _calculationSteps.Clear();

        double averageMachines = _initialMachines;

        foreach (MachineEntry entry in _newMachineDates)
        {
            int monthsActive = 12 - entry.Date.Month + 1;
            averageMachines += entry.Count * (monthsActive / 12.0);
        }

        foreach (MachineEntry entry in _removedMachineDates)
        {
            int monthsActive = 12 - entry.Date.Month + 1;
            averageMachines -= entry.Count * (monthsActive / 12.0);
        }

        double totalHours = _workingDays * _shiftsCount * _shiftDuration;

        double downtimeHours = totalHours * (_downtimePercentage / 100);
        double availableHours = totalHours - downtimeHours;

        double productionCapacity = averageMachines * _productivityPerMachine * availableHours;

        double utilizationCoefficient = _plannedProduction / productionCapacity;

        _result = new CalculationResult
        {
            ProductionCapacity = productionCapacity,
            UtilizationCoefficient = utilizationCoefficient,
        };

        _calculationSteps.Add("1. Среднегодовое количество станков (Qср.г):");
        _calculationSteps.Add($"   Qср.г = {_initialMachines} + Σ(введенные станки * (количество месяцев / 12)) - Σ(выбывшие станки * (количество месяцев / 12))");

        foreach (MachineEntry entry in _newMachineDates)
        {
            int monthsActive = 12 - entry.Date.Month + 1;
            _calculationSteps.Add($"   + {entry.Count} * ({monthsActive} / 12) = {(entry.Count * (monthsActive / 12.0)).ToString($"N{Precision}")}");
        }

        foreach (MachineEntry entry in _removedMachineDates)
        {
            int monthsActive = 12 - entry.Date.Month + 1;
            _calculationSteps.Add($"   - {entry.Count} * ({monthsActive} / 12) = {(entry.Count * (monthsActive / 12.0)).ToString($"N{Precision}")}");
        }

        _calculationSteps.Add($"   Qср.г = {averageMachines.ToString($"N{Precision}")}");

        _calculationSteps.Add("2. Общее время работы (Фреж):");
        _calculationSteps.Add($"   Фреж = {_workingDays} * {_shiftsCount} * {_shiftDuration} = {totalHours.ToString($"N{Precision}")} часов");

        _calculationSteps.Add("3. Время простоя (D):");
        _calculationSteps.Add($"   D = Фреж * ({_downtimePercentage} / 100) = {downtimeHours.ToString($"N{Precision}")} часов");

        _calculationSteps.Add("4. Доступное время (Фэфф):");
        _calculationSteps.Add($"   Фэфф = Фреж - D = {availableHours.ToString($"N{Precision}")} часов");

        _calculationSteps.Add("5. Производственная мощность (ПМ):");
        _calculationSteps.Add("   ПМ = Фэфф * производительность * Qср.г");
        _calculationSteps.Add($"   ПМ = {availableHours.ToString($"N{Precision}")} * {_productivityPerMachine.ToString($"N{Precision}")} * {averageMachines.ToString($"N{Precision}")} = {productionCapacity.ToString($"N{Precision}")} деталей");

        _calculationSteps.Add("6. Коэффициент использования мощности (U):");
        _calculationSteps.Add($"   U = план выпуска / P = {_plannedProduction.ToString($"N{Precision}")} / {productionCapacity.ToString($"N{Precision}")} = {utilizationCoefficient.ToString($"N{Precision}")}");
    }

    private class MachineEntry
    {
        public DateTime Date { get; set; }
        public double Count { get; set; }
    }

    private class CalculationResult
    {
        public double ProductionCapacity { get; set; }
        public double UtilizationCoefficient { get; set; }
    }
}
