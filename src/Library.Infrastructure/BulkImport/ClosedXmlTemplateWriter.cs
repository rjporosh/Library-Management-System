using ClosedXML.Excel;
using Library.Application.Features.BulkImport;

namespace Library.Infrastructure.BulkImport;

/// <summary>
/// Builds a formatted import template: a data sheet (bold header + example
/// rows) and an "Instructions" sheet describing every column and its
/// accepted values.
/// </summary>
public sealed class ClosedXmlTemplateWriter : IImportTemplateWriter
{
    public byte[] Build(ImportTemplateSpec spec)
    {
        using var workbook = new XLWorkbook();

        var sheet = workbook.Worksheets.Add(spec.SheetName);
        for (var col = 0; col < spec.Columns.Count; col++)
        {
            var cell = sheet.Cell(1, col + 1);
            cell.Value = spec.Columns[col].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        for (var r = 0; r < spec.ExampleRows.Count; r++)
        {
            var row = spec.ExampleRows[r];
            for (var c = 0; c < row.Count; c++)
            {
                sheet.Cell(r + 2, c + 1).Value = row[c];
            }
        }

        sheet.Columns().AdjustToContents();

        var guide = workbook.Worksheets.Add("Instructions");
        guide.Cell(1, 1).Value = "Column";
        guide.Cell(1, 2).Value = "Required";
        guide.Cell(1, 3).Value = "Description";
        guide.Cell(1, 4).Value = "Accepted values";
        guide.Row(1).Style.Font.Bold = true;

        for (var i = 0; i < spec.Columns.Count; i++)
        {
            var column = spec.Columns[i];
            guide.Cell(i + 2, 1).Value = column.Header;
            guide.Cell(i + 2, 2).Value = column.Required ? "Yes" : "No";
            guide.Cell(i + 2, 3).Value = column.Description;
            guide.Cell(i + 2, 4).Value = column.SupportedValues ?? "-";
        }

        guide.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
