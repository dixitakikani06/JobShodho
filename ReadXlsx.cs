using ClosedXML.Excel;
using var wb = new XLWorkbook("/tmp/ai-jobs.xlsx");
var ws = wb.Worksheets.First();
var lastRow = ws.LastRowUsed()!.RowNumber();
for (int r = 1; r <= lastRow; r++)
{
    var vals = new List<string>();
    for (int c = 1; c <= 7; c++) vals.Add(ws.Cell(r,c).GetString());
    Console.WriteLine(string.Join(" | ", vals));
}
