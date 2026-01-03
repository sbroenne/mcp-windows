using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Sbroenne.WindowsMcp.Office;

/// <summary>
/// Provides COM Interop helpers for saving documents in Microsoft Office applications.
/// Uses late-binding (dynamic) to avoid requiring Office Interop assemblies.
/// 
/// This is a CONVENIENCE helper for quick saves. Limitations:
/// - Saves the ACTIVE document only (the one currently in focus)
/// - File format is determined by file extension (.docx, .xlsx, .pptx, etc.)
/// - Cannot set advanced options (compatibility mode, encryption, metadata)
/// - Cannot convert between formats (e.g., .doc to .pdf requires explicit export)
/// 
/// For complex save operations, use UI Automation or keyboard_control to navigate
/// the File menu and Save As dialog manually.
/// </summary>
[SupportedOSPlatform("windows")]
public static class OfficeComHelper
{
    /// <summary>
    /// Office application types supported for COM automation.
    /// </summary>
    public enum OfficeAppType
    {
        /// <summary>Not an Office application.</summary>
        None,
        /// <summary>Microsoft Word (WINWORD.EXE).</summary>
        Word,
        /// <summary>Microsoft Excel (EXCEL.EXE).</summary>
        Excel,
        /// <summary>Microsoft PowerPoint (POWERPNT.EXE).</summary>
        PowerPoint,
        /// <summary>Microsoft Visio (VISIO.EXE).</summary>
        Visio,
        /// <summary>Microsoft Publisher (MSPUB.EXE).</summary>
        Publisher,
    }

    // Word file formats (WdSaveFormat)
    private const int WdFormatDocumentDefault = 16; // .docx
    private const int WdFormatDocument = 0;         // .doc
    private const int WdFormatPDF = 17;             // .pdf
    private const int WdFormatRTF = 6;              // .rtf

    // Excel file formats (XlFileFormat)
    private const int XlOpenXMLWorkbook = 51;       // .xlsx
    private const int XlExcel8 = 56;                // .xls
    private const int XlWorkbookDefault = 51;       // .xlsx
    private const int XlCSV = 6;                    // .csv

    // PowerPoint file formats (PpSaveAsFileType)
    private const int PpSaveAsDefault = 11;         // .pptx
    private const int PpSaveAsPresentation = 1;     // .ppt
    private const int PpSaveAsPDF = 32;             // .pdf

    /// <summary>
    /// Determines if the given process name is a supported Office application.
    /// </summary>
    /// <param name="processName">The process name (e.g., "WINWORD", "EXCEL").</param>
    /// <returns>The Office application type, or None if not an Office app.</returns>
    public static OfficeAppType GetOfficeAppType(string? processName)
    {
        if (string.IsNullOrEmpty(processName))
        {
            return OfficeAppType.None;
        }

        return processName.ToUpperInvariant() switch
        {
            "WINWORD" => OfficeAppType.Word,
            "EXCEL" => OfficeAppType.Excel,
            "POWERPNT" => OfficeAppType.PowerPoint,
            "VISIO" => OfficeAppType.Visio,
            "MSPUB" => OfficeAppType.Publisher,
            _ => OfficeAppType.None,
        };
    }

    /// <summary>
    /// Gets the process name for a window handle.
    /// </summary>
    /// <param name="windowHandle">The window handle as a string.</param>
    /// <returns>The process name, or null if not found.</returns>
    public static string? GetProcessNameFromHandle(string? windowHandle)
    {
        if (string.IsNullOrEmpty(windowHandle) || !nint.TryParse(windowHandle, out var hwnd) || hwnd == 0)
        {
            return null;
        }

        try
        {
            _ = GetWindowThreadProcessId(hwnd, out uint processId);
            if (processId == 0)
            {
                return null;
            }

            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Saves the active document in a supported Office application using COM Interop.
    /// </summary>
    /// <param name="appType">The Office application type.</param>
    /// <param name="filePath">The full path to save the file to.</param>
    /// <returns>A result indicating success or failure with error details.</returns>
    public static OfficeComResult SaveDocument(OfficeAppType appType, string filePath)
    {
        return appType switch
        {
            OfficeAppType.Word => SaveWordDocument(filePath),
            OfficeAppType.Excel => SaveExcelWorkbook(filePath),
            OfficeAppType.PowerPoint => SavePowerPointPresentation(filePath),
            OfficeAppType.Visio => SaveVisioDocument(filePath),
            OfficeAppType.Publisher => SavePublisherDocument(filePath),
            _ => OfficeComResult.Failure($"Unsupported Office application type: {appType}"),
        };
    }

    /// <summary>
    /// Saves the active Word document.
    /// </summary>
    private static OfficeComResult SaveWordDocument(string filePath)
    {
        dynamic? wordApp = null;
        dynamic? doc = null;

        try
        {
            wordApp = GetActiveComObject("Word.Application");
            if (wordApp == null)
            {
                return OfficeComResult.Failure("Word is not running or no active instance found.");
            }

            doc = wordApp.ActiveDocument;
            if (doc == null)
            {
                return OfficeComResult.Failure("No active document in Word.");
            }

            // Determine file format from extension
            int fileFormat = GetWordFileFormat(filePath);

            // SaveAs2 is the modern method (Word 2010+)
            doc.SaveAs2(FileName: filePath, FileFormat: fileFormat);

            return OfficeComResult.Success($"Document saved to: {filePath}");
        }
        catch (COMException ex)
        {
            return OfficeComResult.Failure($"COM error saving Word document: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OfficeComResult.Failure($"Error saving Word document: {ex.Message}");
        }
        finally
        {
            ReleaseComObject(doc);
            // Don't release wordApp - we didn't create it, just attached to it
        }
    }

    /// <summary>
    /// Saves the active Excel workbook.
    /// </summary>
    private static OfficeComResult SaveExcelWorkbook(string filePath)
    {
        dynamic? excelApp = null;
        dynamic? workbook = null;

        try
        {
            excelApp = GetActiveComObject("Excel.Application");
            if (excelApp == null)
            {
                return OfficeComResult.Failure("Excel is not running or no active instance found.");
            }

            workbook = excelApp.ActiveWorkbook;
            if (workbook == null)
            {
                return OfficeComResult.Failure("No active workbook in Excel.");
            }

            int fileFormat = GetExcelFileFormat(filePath);

            workbook.SaveAs(Filename: filePath, FileFormat: fileFormat);

            return OfficeComResult.Success($"Workbook saved to: {filePath}");
        }
        catch (COMException ex)
        {
            return OfficeComResult.Failure($"COM error saving Excel workbook: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OfficeComResult.Failure($"Error saving Excel workbook: {ex.Message}");
        }
        finally
        {
            ReleaseComObject(workbook);
        }
    }

    /// <summary>
    /// Saves the active PowerPoint presentation.
    /// </summary>
    private static OfficeComResult SavePowerPointPresentation(string filePath)
    {
        dynamic? pptApp = null;
        dynamic? presentation = null;

        try
        {
            pptApp = GetActiveComObject("PowerPoint.Application");
            if (pptApp == null)
            {
                return OfficeComResult.Failure("PowerPoint is not running or no active instance found.");
            }

            presentation = pptApp.ActivePresentation;
            if (presentation == null)
            {
                return OfficeComResult.Failure("No active presentation in PowerPoint.");
            }

            int fileFormat = GetPowerPointFileFormat(filePath);

            presentation.SaveAs(FileName: filePath, FileFormat: fileFormat);

            return OfficeComResult.Success($"Presentation saved to: {filePath}");
        }
        catch (COMException ex)
        {
            return OfficeComResult.Failure($"COM error saving PowerPoint presentation: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OfficeComResult.Failure($"Error saving PowerPoint presentation: {ex.Message}");
        }
        finally
        {
            ReleaseComObject(presentation);
        }
    }

    /// <summary>
    /// Saves the active Visio document.
    /// </summary>
    private static OfficeComResult SaveVisioDocument(string filePath)
    {
        dynamic? visioApp = null;
        dynamic? doc = null;

        try
        {
            visioApp = GetActiveComObject("Visio.Application");
            if (visioApp == null)
            {
                return OfficeComResult.Failure("Visio is not running or no active instance found.");
            }

            doc = visioApp.ActiveDocument;
            if (doc == null)
            {
                return OfficeComResult.Failure("No active document in Visio.");
            }

            doc.SaveAs(filePath);

            return OfficeComResult.Success($"Document saved to: {filePath}");
        }
        catch (COMException ex)
        {
            return OfficeComResult.Failure($"COM error saving Visio document: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OfficeComResult.Failure($"Error saving Visio document: {ex.Message}");
        }
        finally
        {
            ReleaseComObject(doc);
        }
    }

    /// <summary>
    /// Saves the active Publisher document.
    /// </summary>
    private static OfficeComResult SavePublisherDocument(string filePath)
    {
        dynamic? pubApp = null;
        dynamic? doc = null;

        try
        {
            pubApp = GetActiveComObject("Publisher.Application");
            if (pubApp == null)
            {
                return OfficeComResult.Failure("Publisher is not running or no active instance found.");
            }

            doc = pubApp.ActiveDocument;
            if (doc == null)
            {
                return OfficeComResult.Failure("No active document in Publisher.");
            }

            doc.SaveAs(Filename: filePath);

            return OfficeComResult.Success($"Document saved to: {filePath}");
        }
        catch (COMException ex)
        {
            return OfficeComResult.Failure($"COM error saving Publisher document: {ex.Message}");
        }
        catch (Exception ex)
        {
            return OfficeComResult.Failure($"Error saving Publisher document: {ex.Message}");
        }
        finally
        {
            ReleaseComObject(doc);
        }
    }

    /// <summary>
    /// Gets the Word file format constant from file extension.
    /// </summary>
    private static int GetWordFileFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".docx" => WdFormatDocumentDefault,
            ".doc" => WdFormatDocument,
            ".pdf" => WdFormatPDF,
            ".rtf" => WdFormatRTF,
            _ => WdFormatDocumentDefault, // Default to .docx
        };
    }

    /// <summary>
    /// Gets the Excel file format constant from file extension.
    /// </summary>
    private static int GetExcelFileFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".xlsx" => XlOpenXMLWorkbook,
            ".xls" => XlExcel8,
            ".csv" => XlCSV,
            _ => XlWorkbookDefault, // Default to .xlsx
        };
    }

    /// <summary>
    /// Gets the PowerPoint file format constant from file extension.
    /// </summary>
    private static int GetPowerPointFileFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".pptx" => PpSaveAsDefault,
            ".ppt" => PpSaveAsPresentation,
            ".pdf" => PpSaveAsPDF,
            _ => PpSaveAsDefault, // Default to .pptx
        };
    }

    /// <summary>
    /// Gets an active COM object by ProgID using GetActiveObject pattern.
    /// </summary>
    private static dynamic? GetActiveComObject(string progId)
    {
        try
        {
            // GetActiveObject was removed from .NET Core, use P/Invoke
            var clsid = GetClsidFromProgId(progId);
            if (clsid == Guid.Empty)
            {
                return null;
            }

            int hr = GetActiveObject(ref clsid, IntPtr.Zero, out object? obj);
            if (hr != 0 || obj == null)
            {
                return null;
            }

            return obj;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the CLSID for a ProgID.
    /// </summary>
    private static Guid GetClsidFromProgId(string progId)
    {
        int hr = CLSIDFromProgID(progId, out Guid clsid);
        return hr == 0 ? clsid : Guid.Empty;
    }

    /// <summary>
    /// Safely releases a COM object.
    /// </summary>
    private static void ReleaseComObject(object? obj)
    {
        if (obj != null)
        {
            try
            {
                Marshal.ReleaseComObject(obj);
            }
            catch
            {
                // Ignore release errors
            }
        }
    }

    #region P/Invoke

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint processId);

    [DllImport("oleaut32.dll", PreserveSig = false)]
    private static extern int GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object? ppunk);

    [DllImport("ole32.dll", CharSet = CharSet.Unicode)]
    private static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);

    #endregion
}

/// <summary>
/// Result of an Office COM operation.
/// </summary>
public readonly struct OfficeComResult
{
    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Message describing the result or error.
    /// </summary>
    public string Message { get; }

    private OfficeComResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static OfficeComResult Success(string message) => new(true, message);

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    public static OfficeComResult Failure(string message) => new(false, message);
}
