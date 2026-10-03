using System.Collections.ObjectModel;
using RegistroEstudiantes.Data;
using RegistroEstudiantes.Models;

namespace RegistroEstudiantes.Views;

public partial class RegistroEstudiantesPage : ContentPage
{
    private readonly DatabaseService _database = new();
    private readonly List<Estudiante> _todos = [];
    private CancellationTokenSource? _busquedaCts;
    private Estudiante? _enEdicion;
    private bool _ocupado;

    public ObservableCollection<Estudiante> EstudiantesVisibles { get; } = [];

    public RegistroEstudiantesPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RecargarAsync();
    }

    private async Task RecargarAsync()
    {
        await EjecutarConCargaAsync(async () =>
        {
            var estudiantes = await _database.ObtenerEstudiantesAsync();
            _todos.Clear();
            _todos.AddRange(estudiantes);
            AplicarBusqueda(BusquedaEntry.Text ?? string.Empty);
        });
    }

    private async void OnGuardarClicked(object? sender, EventArgs e)
    {
        if (_ocupado || !Validar()) return;

        var estudiante = _enEdicion ?? new Estudiante();
        estudiante.Nombre = NombreEntry.Text!.Trim();
        estudiante.Apellido = ApellidoEntry.Text!.Trim();
        estudiante.NumeroCuenta = NumeroCuentaEntry.Text!.Trim();
        estudiante.Carrera = CarreraEntry.Text!.Trim();
        var estabaEditando = _enEdicion is not null;

        await EjecutarConCargaAsync(async () =>
        {
            if (estabaEditando)
                await _database.ActualizarEstudianteAsync(estudiante);
            else
                await _database.GuardarEstudianteAsync(estudiante);

            var estudiantes = await _database.ObtenerEstudiantesAsync();
            _todos.Clear();
            _todos.AddRange(estudiantes);
            AplicarBusqueda(BusquedaEntry.Text ?? string.Empty);
            LimpiarFormulario();
            MostrarEstado(estabaEditando ? "Estudiante actualizado correctamente." : "Estudiante registrado correctamente.", false);
            SemanticScreenReader.Announce(EstadoLabel.Text);
        });
    }

    private bool Validar()
    {
        if (string.IsNullOrWhiteSpace(NombreEntry.Text)) return ErrorValidacion("Ingresa el nombre.", NombreEntry);
        if (NombreEntry.Text.Trim().Length > 60) return ErrorValidacion("El nombre no puede superar 60 caracteres.", NombreEntry);
        if (string.IsNullOrWhiteSpace(ApellidoEntry.Text)) return ErrorValidacion("Ingresa el apellido.", ApellidoEntry);
        if (ApellidoEntry.Text.Trim().Length > 60) return ErrorValidacion("El apellido no puede superar 60 caracteres.", ApellidoEntry);
        if (string.IsNullOrWhiteSpace(NumeroCuentaEntry.Text)) return ErrorValidacion("Ingresa el número de cuenta.", NumeroCuentaEntry);
        if (string.IsNullOrWhiteSpace(CarreraEntry.Text)) return ErrorValidacion("Ingresa la carrera.", CarreraEntry);
        if (CarreraEntry.Text.Trim().Length > 100) return ErrorValidacion("La carrera no puede superar 100 caracteres.", CarreraEntry);
        EstadoLabel.IsVisible = false;
        return true;
    }

    private bool ErrorValidacion(string mensaje, Entry campo)
    {
        MostrarEstado(mensaje, true);
        campo.Focus();
        return false;
    }

    private void OnEditarClicked(object? sender, EventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Estudiante estudiante) return;
        _enEdicion = estudiante;
        NombreEntry.Text = estudiante.Nombre;
        ApellidoEntry.Text = estudiante.Apellido;
        NumeroCuentaEntry.Text = estudiante.NumeroCuenta;
        CarreraEntry.Text = estudiante.Carrera;
        GuardarButton.Text = "Actualizar Estudiante";
        MostrarEstado($"Editando estudiante ID {estudiante.Id}.", false);
        NombreEntry.Focus();
        SemanticScreenReader.Announce("Formulario en modo de edición.");
    }

    private async void OnEliminarClicked(object? sender, EventArgs e)
    {
        if (_ocupado || (sender as BindableObject)?.BindingContext is not Estudiante estudiante) return;
        var confirmar = await DisplayAlertAsync("Eliminar estudiante", $"¿Deseas eliminar a {estudiante.Nombre} {estudiante.Apellido}?", "Eliminar", "Cancelar");
        if (!confirmar) return;

        await EjecutarConCargaAsync(async () =>
        {
            await _database.EliminarEstudianteAsync(estudiante);
            _todos.RemoveAll(s => s.Id == estudiante.Id);
            AplicarBusqueda(BusquedaEntry.Text ?? string.Empty);
            if (_enEdicion?.Id == estudiante.Id) LimpiarFormulario();
            MostrarEstado("Estudiante eliminado correctamente.", false);
            SemanticScreenReader.Announce(EstadoLabel.Text);
        });
    }

    private void OnLimpiarClicked(object? sender, EventArgs e) => LimpiarFormulario();

    private void LimpiarFormulario()
    {
        NombreEntry.Text = string.Empty;
        ApellidoEntry.Text = string.Empty;
        NumeroCuentaEntry.Text = string.Empty;
        CarreraEntry.Text = string.Empty;
        _enEdicion = null;
        GuardarButton.Text = "Guardar Estudiante";
        EstadoLabel.IsVisible = false;
        NombreEntry.Focus();
    }

    private void OnBusquedaTextChanged(object? sender, TextChangedEventArgs e)
    {
        _busquedaCts?.Cancel();
        var cts = _busquedaCts = new CancellationTokenSource();
        _ = BuscarConDemoraAsync(e.NewTextValue ?? string.Empty, cts.Token);
    }

    private async Task BuscarConDemoraAsync(string texto, CancellationToken token)
    {
        try
        {
            await Task.Delay(250, token);
            MainThread.BeginInvokeOnMainThread(() => AplicarBusqueda(texto));
        }
        catch (OperationCanceledException) { }
    }

    private void AplicarBusqueda(string texto)
    {
        var termino = texto.Trim();
        var filtrados = string.IsNullOrEmpty(termino)
            ? _todos
            : _todos.Where(s => s.Id.ToString().Contains(termino, StringComparison.OrdinalIgnoreCase)
                || s.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase)
                || s.Apellido.Contains(termino, StringComparison.OrdinalIgnoreCase)
                || s.NumeroCuenta.Contains(termino, StringComparison.OrdinalIgnoreCase)
                || s.Carrera.Contains(termino, StringComparison.OrdinalIgnoreCase)).ToList();

        EstudiantesVisibles.Clear();
        foreach (var estudiante in filtrados) EstudiantesVisibles.Add(estudiante);
        ContadorLabel.Text = termino.Length == 0
            ? $"{_todos.Count} {(_todos.Count == 1 ? "estudiante" : "estudiantes")}"
            : $"{filtrados.Count} resultados de {_todos.Count} estudiantes";
        VacioLabel.Text = _todos.Count == 0
            ? "Aún no hay estudiantes registrados. Completa el formulario para agregar el primero."
            : "No se encontraron estudiantes con esa búsqueda.";
        VacioLabel.IsVisible = filtrados.Count == 0;
    }

    private async Task EjecutarConCargaAsync(Func<Task> operacion)
    {
        if (_ocupado) return;
        _ocupado = true;
        GuardarButton.IsEnabled = false;
        CargaIndicator.IsVisible = CargaIndicator.IsRunning = true;
        try { await operacion(); }
        catch (Exception ex)
        {
            MostrarEstado($"No se pudo completar la operación: {ex.Message}", true);
        }
        finally
        {
            CargaIndicator.IsRunning = CargaIndicator.IsVisible = false;
            GuardarButton.IsEnabled = true;
            _ocupado = false;
        }
    }

    private void MostrarEstado(string mensaje, bool error)
    {
        EstadoLabel.Text = mensaje;
        EstadoLabel.TextColor = error ? Color.FromArgb("#B91C1C") : Color.FromArgb("#15803D");
        EstadoLabel.IsVisible = true;
    }
}
