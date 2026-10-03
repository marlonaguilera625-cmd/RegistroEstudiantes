using SQLite;

namespace RegistroEstudiantes.Models;

public class Estudiante
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string NumeroCuenta { get; set; } = string.Empty;
    public string Carrera { get; set; } = string.Empty;
}
