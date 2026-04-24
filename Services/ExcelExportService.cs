using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace QuanLyNhanSu_WPF.Services
{
    public class ExcelExportService
    {
        public void ExportToExcel<T>(IEnumerable<T> data, string sheetName, string defaultFileName, Action<IXLWorksheet, IEnumerable<T>> customMapping)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = defaultFileName
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(sheetName);
                    
                    // Header styling
                    worksheet.Row(1).Style.Font.Bold = true;
                    worksheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#673AB7"); // Deep Purple
                    worksheet.Row(1).Style.Font.FontColor = XLColor.White;

                    // Data mapping
                    customMapping(worksheet, data);

                    // Auto-filter and column adjustment
                    worksheet.Columns().AdjustToContents();
                    
                    workbook.SaveAs(saveFileDialog.FileName);
                }
            }
        }
    }
}
