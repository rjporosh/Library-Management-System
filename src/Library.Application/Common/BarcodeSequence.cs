namespace Library.Application.Common;

/// <summary>Parses the numeric suffix of sequential barcodes like "BC-0007".</summary>
public static class BarcodeSequence
{
    public static int MaxSuffix(string prefix, IEnumerable<string> barcodes)
    {
        var max = 0;
        foreach (var barcode in barcodes)
        {
            if (barcode.Length > prefix.Length
                && int.TryParse(barcode.AsSpan(prefix.Length), out var n)
                && n > max)
            {
                max = n;
            }
        }

        return max;
    }
}
