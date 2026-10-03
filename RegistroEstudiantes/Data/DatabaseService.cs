using RegistroEstudiantes.Models;
using SQLite;

namespace RegistroEstudiantes.Data;

public sealed class DatabaseService
{
    private readonly SQLiteAsyncConnection _database;
    private readonly Task _initialization;

    public DatabaseService()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, "estudiantes.db3");
        _database = new SQLiteAsyncConnection(path);
        _initialization = _database.CreateTableAsync<Estudiante>();
    }

    public async Task<int> GuardarEstudianteAsync(Estudiante estudiante)
    {
        await _initialization;
        return await _database.InsertAsync(estudiante);
    }

    public async Task<List<Estudiante>> ObtenerEstudiantesAsync()
    {
        await _initialization;
        return await _database.Table<Estudiante>().OrderBy(e => e.Id).ToListAsync();
    }

    public async Task<int> ActualizarEstudianteAsync(Estudiante estudiante)
    {
        await _initialization;
        return await _database.UpdateAsync(estudiante);
    }

    public async Task<int> EliminarEstudianteAsync(Estudiante estudiante)
    {
        await _initialization;
        return await _database.DeleteAsync(estudiante);
    }
}
