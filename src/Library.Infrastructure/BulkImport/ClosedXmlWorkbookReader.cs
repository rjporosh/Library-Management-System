using ClosedXML.Excel;
using Library.Application.Features.BulkImport;

namespace Library.Infrastructure.BulkImport;

/// <summary>Reads the first worksheet of an .xlsx file: row 1 = headers, the rest = data.</summary>
public sealed class ClosedXmlWorkbookReader : IWorkbookReader
{
    public WorkbookData Read(Stream stream)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception ex)
        {
            throw new WorkbookReadException(ex.Message);
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new WorkbookReadException("The workbook has no worksheets.");

            var range = sheet.RangeUsed();
            if (range is null)
            {
                return new WorkbookData([], []);
            }

            var headerRow = range.FirstRow();
            var headers = headerRow.Cells()
                .Select(c => c.GetString().Trim())
                .Where(h => h.Length > 0)
                .ToList();

            var rows = new List<ImportRow>();
            foreach (var xlRow in range.Rows().Skip(1))
            {
                var cells = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                var anyValue = false;

                for (var i = 0; i < headers.Count; i++)
                {
                    var cell = xlRow.Cell(i + 1);
                    var value = cell.GetString();
                    cells[headers[i]] = string.IsNullOrWhiteSpace(value) ? null : value;
                    anyValue |= !string.IsNullOrWhiteSpace(value);
                }

                if (anyValue)
                {
                    rows.Add(new ImportRow(xlRow.RowNumber(), cells));
                }
            }

            return new WorkbookData(headers, rows);
        }
    }
}
