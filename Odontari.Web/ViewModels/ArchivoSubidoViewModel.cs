namespace Odontari.Web.ViewModels;

/// <summary>Item de archivo subido para listar en la vista (expediente / subir archivos).</summary>
public class ArchivoSubidoItemViewModel
{
    public int Id { get; set; }
    public string FileNameOriginal { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Extension { get; set; } = null!;
}
