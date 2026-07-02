using LitigApp.Application.Features.Imports;

namespace LitigApp.Application.Common.Abstractions;

/// <summary>Parses an uploaded .xlsx into an <see cref="ExcelPreview"/>. Throws on a corrupt/non-xlsx file.</summary>
public interface IExcelParser
{
    ExcelPreview Parse(Stream stream, string fileName);
}
