using Microsoft.AspNetCore.Mvc;
using NPOI.HSSF.UserModel;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.Streaming;
using NPOI.XSSF.UserModel;
using System.Diagnostics;
using System.Text.Json;

namespace Accura_MES.Utilities
{
    public class FileUtils
    {
        #region excel proccess
        [ApiExplorerSettings(IgnoreApi = true)]
        public static bool IsExcelFileUsingNPOI(IFormFile file)
        {
            try
            {
                // 打開檔案流
                using (var stream = file.OpenReadStream())
                {
                    // 嘗試讀取 .xlsx 格式
                    if (file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        XSSFWorkbook workbook = new XSSFWorkbook(stream);
                        return workbook != null && workbook.NumberOfSheets > 0;
                    }
                    // 嘗試讀取 .xls 格式
                    else if (file.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                    {
                        HSSFWorkbook workbook = new HSSFWorkbook(stream);
                        return workbook != null && workbook.NumberOfSheets > 0;
                    }
                    else
                    {
                        // 檔案副檔名不符合 .xls 或 .xlsx 格式
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果發生例外，則認為這不是有效的 Excel 檔案
                Debug.WriteLine(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 讀取 excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>(sheetName, row[col])</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static Dictionary<string, List<List<string>>> ReadExcel(string filePath)
        {
            var result = new Dictionary<string, List<List<string>>>();
            IWorkbook workbook;

            // 判斷檔案副檔名以選擇處理方式
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new XSSFWorkbook(fileStream); // 讀取 .xlsx
                }
                else if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new HSSFWorkbook(fileStream); // 讀取 .xls
                }
                else
                {
                    throw new NotSupportedException("檔案格式不支援，僅支援 .xls 和 .xlsx 格式。");
                }

                // 遍歷所有工作表
                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    var sheet = workbook.GetSheetAt(sheetIndex);
                    Debug.WriteLine($"開始讀取工作表: {sheet.SheetName}");

                    var sheetContent = new List<List<string>>();

                    // 遍歷工作表中的所有行
                    for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        var rowContent = new List<string>(); // 紀錄內容

                        if (row == null) continue;

                        for (int cellIndex = 0; cellIndex < row.LastCellNum; cellIndex++)
                        {
                            var cell = row.GetCell(cellIndex);
                            var cellValue = cell == null ? string.Empty : cell.ToString();
                            rowContent.Add(cellValue);

                            Debug.WriteLine($"[Sheet: {sheet.SheetName}] Row {rowIndex + 1}, Cell {cellIndex + 1}: {cellValue}");
                        }

                        sheetContent.Add(rowContent);
                    }

                    result[sheet.SheetName] = sheetContent;
                    Debug.WriteLine($"結束讀取工作表: {sheet.SheetName}");
                }

                return result;
            }
        }

        /// <summary>
        /// 讀取 料Bom excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>(sheetName, row[col])</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static Dictionary<string, List<List<string>>> PartBomExcel(string filePath)
        {
            var result = new Dictionary<string, List<List<string>>>();
            IWorkbook workbook;

            // 判斷檔案副檔名以選擇處理方式
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new XSSFWorkbook(fileStream); // 讀取 .xlsx
                }
                else if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new HSSFWorkbook(fileStream); // 讀取 .xls
                }
                else
                {
                    throw new NotSupportedException("檔案格式不支援，僅支援 .xls 和 .xlsx 格式。");
                }

                // 只讀第一個工作表
                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    var sheet = workbook.GetSheetAt(sheetIndex);
                    Debug.WriteLine($"開始讀取工作表: {sheet.SheetName}");

                    var sheetContent = new List<List<string>>();
                    int stop_index = 0;
                    // 遍歷工作表中的所有行
                    for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        var rowContent = new List<string>(); // 紀錄內容

                        if (row == null) continue;
                        if (rowIndex == 1)
                        {
                            stop_index = row.LastCellNum;
                        }
                        int space_count = 0;
                        for (int cellIndex = 0; cellIndex < row.LastCellNum; cellIndex++)
                        {
                            //不讀取空白區域
                            if (stop_index != 0 && stop_index < cellIndex)
                            {
                                break;
                            }
                            var cell = row.GetCell(cellIndex);

                            string cellValue;
                            if (cell == null)
                            {
                                cellValue = string.Empty;
                            }
                            else
                            {
                                // 根據儲存格類型處理
                                switch (cell.CellType)
                                {
                                    case CellType.Numeric:
                                        if (DateUtil.IsCellDateFormatted(cell))
                                        {
                                            // 如果是日期格式，轉換為標準日期字串
                                            cellValue = cell.DateCellValue.ToString();
                                        }
                                        else
                                        {
                                            // 如果是數值格式，轉換為字串
                                            cellValue = cell.NumericCellValue.ToString();
                                        }
                                        break;

                                    case CellType.String:
                                        cellValue = cell.StringCellValue;
                                        break;

                                    case CellType.Boolean:
                                        cellValue = cell.BooleanCellValue.ToString();
                                        break;

                                    case CellType.Formula:
                                        cellValue = cell.ToString(); // 或計算公式值
                                        break;

                                    default:
                                        cellValue = string.Empty;
                                        break;
                                }
                            }
                            rowContent.Add(cellValue);

                            //Debug.WriteLine($"[Sheet: {sheet.SheetName}] Row {rowIndex + 1}, Cell {cellIndex + 1}: {cellValue}");

                            //重設中斷點
                            if (rowIndex == 1 && string.IsNullOrEmpty(cellValue))
                            {
                                space_count++;
                                if (space_count == 3)
                                {
                                    stop_index = cellIndex;
                                }
                            }
                        }
                        if (rowContent.Count > 4)
                            sheetContent.Add(rowContent);
                    }

                    result[sheet.SheetName] = sheetContent;
                    Debug.WriteLine($"結束讀取工作表: {sheet.SheetName}");
                    break;
                }

                return result;
            }
        }

        /// <summary>
        /// 讀取 製令excel
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>(sheetName, row[col])</returns>
        /// <exception cref="NotSupportedException"></exception>
        public static Dictionary<string, List<List<string>>> ReadExcel_plus(string filePath)
        {
            var result = new Dictionary<string, List<List<string>>>();
            IWorkbook workbook;

            // 判斷檔案副檔名以選擇處理方式
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new XSSFWorkbook(fileStream); // 讀取 .xlsx
                }
                else if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new HSSFWorkbook(fileStream); // 讀取 .xls
                }
                else
                {
                    throw new NotSupportedException("檔案格式不支援，僅支援 .xls 和 .xlsx 格式。");
                }

                // 只讀第一個工作表
                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    var sheet = workbook.GetSheetAt(sheetIndex);
                    Debug.WriteLine($"開始讀取工作表: {sheet.SheetName}");

                    var sheetContent = new List<List<string>>();
                    int stop_index = 0;
                    // 遍歷工作表中的所有行
                    for (int rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                    {
                        var row = sheet.GetRow(rowIndex);
                        var rowContent = new List<string>(); // 紀錄內容

                        if (row == null) continue;
                        if (rowIndex == 1)
                        {
                            stop_index = row.LastCellNum;
                        }
                        int space_count = 0;
                        for (int cellIndex = 0; cellIndex < row.LastCellNum; cellIndex++)
                        {
                            //不讀取空白區域
                            if (stop_index != 0 && stop_index < cellIndex)
                            {
                                break;
                            }
                            var cell = row.GetCell(cellIndex);

                            string cellValue;
                            if (cell == null)
                            {
                                cellValue = string.Empty;
                            }
                            else
                            {
                                // 根據儲存格類型處理
                                switch (cell.CellType)
                                {
                                    case CellType.Numeric:
                                        if (DateUtil.IsCellDateFormatted(cell))
                                        {
                                            // 如果是日期格式，轉換為標準日期字串
                                            cellValue = cell.DateCellValue.ToString();
                                        }
                                        else
                                        {
                                            // 如果是數值格式，轉換為字串
                                            cellValue = cell.NumericCellValue.ToString();
                                        }
                                        break;

                                    case CellType.String:
                                        cellValue = cell.StringCellValue;
                                        break;

                                    case CellType.Boolean:
                                        cellValue = cell.BooleanCellValue.ToString();
                                        break;

                                    case CellType.Formula:
                                        cellValue = cell.ToString(); // 或計算公式值
                                        break;

                                    default:
                                        cellValue = string.Empty;
                                        break;
                                }
                            }
                            rowContent.Add(cellValue);

                            Debug.WriteLine($"[Sheet: {sheet.SheetName}] Row {rowIndex + 1}, Cell {cellIndex + 1}: {cellValue}");

                            //重設中斷點
                            if (rowIndex == 1 && string.IsNullOrEmpty(cellValue))
                            {
                                space_count++;
                                if (space_count == 3)
                                {
                                    stop_index = cellIndex;
                                }
                            }
                        }
                        if (rowContent.Count > 10)
                            sheetContent.Add(rowContent);
                    }

                    result[sheet.SheetName] = sheetContent;
                    Debug.WriteLine($"結束讀取工作表: {sheet.SheetName}");
                    break;
                }

                return result;
            }
        }

        /// <summary>
        /// 讀取 excel(頁籤名)
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static List<string> ReadExcelPage(string filePath)
        {
            var result = new List<string>();
            IWorkbook workbook;

            // 判斷檔案副檔名以選擇處理方式
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                if (filePath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new XSSFWorkbook(fileStream); // 讀取 .xlsx
                }
                else if (filePath.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                {
                    workbook = new HSSFWorkbook(fileStream); // 讀取 .xls
                }
                else
                {
                    throw new NotSupportedException("檔案格式不支援，僅支援 .xls 和 .xlsx 格式。");
                }

                // 遍歷所有工作表
                for (int sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                {
                    var sheet = workbook.GetSheetAt(sheetIndex);
                    Debug.WriteLine($"開始讀取工作表: {sheet.SheetName}");

                    result.Add(sheet.SheetName);

                    Debug.WriteLine($"結束讀取工作表: {sheet.SheetName}");
                }

                return result;
            }
        }

        /// <summary>
        /// 將輸入的資料寫入 Excel 文件。
        /// </summary>
        /// <param name="input">包含要寫入 Excel 的資料的列表，每個字典代表一行數據。</param>
        /// <param name="filePath">要保存的 Excel 文件的路徑，支持 .xls 和 .xlsx 格式。</param>
        /// <exception cref="NotSupportedException">當文件格式不支援時拋出此異常。</exception>
        /// <remarks>
        /// 程式流程大綱：
        /// 1. 根據文件副檔名選擇創建對應的 IWorkbook 實例。
        /// 2. 檢查是否已經存在名為 "Sheet1" 的工作表，如果存在則刪除它。
        /// 3. 創建一個新的工作表("Sheet1")。
        /// 4. 如果輸入資料不為空，則執行以下步驟：
        ///    a. 添加標題行（從字典的鍵中獲取）。
        ///    b. 添加數據行（從字典的值中獲取）。
        /// 5. 將工作簿寫入指定的文件路徑。
        /// </remarks>
        public static void WriteToExcel(List<Dictionary<string, object?>> input, string filePath)
        {
            IWorkbook workbook;
            if (filePath.EndsWith(".xlsx"))
            {
                workbook = new XSSFWorkbook();
            }
            else if (filePath.EndsWith(".xls"))
            {
                workbook = new HSSFWorkbook();
            }
            else
            {
                throw new NotSupportedException("檔案格式不支援，僅支援 .xls 和 .xlsx 格式。");
            }

            // 檢查是否已經存在名為 "Sheet1" 的工作表，如果存在則刪除它
            int sheetIndex = workbook.GetSheetIndex("Sheet1");
            if (sheetIndex != -1 && workbook.NumberOfSheets > 0)
            {
                workbook.RemoveSheetAt(sheetIndex);
            }

            ISheet sheet = workbook.CreateSheet("Sheet1");

            if (input.Count > 0)
            {
                // 添加標題
                var headers = input[0].Keys.ToList();
                IRow headerRow = sheet.CreateRow(0);
                for (int i = 0; i < headers.Count; i++)
                {
                    headerRow.CreateCell(i).SetCellValue(headers[i]);
                }

                // 添加數據
                for (int i = 0; i < input.Count; i++)
                {
                    var row = input[i];
                    IRow dataRow = sheet.CreateRow(i + 1);
                    for (int j = 0; j < headers.Count; j++)
                    {
                        var cellValue = row[headers[j]]?.ToString() ?? string.Empty;
                        dataRow.CreateCell(j).SetCellValue(cellValue);
                    }
                }
            }

            // 保存文件
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fileStream);
            }
        }

        /// <summary>
        /// 將輸入的資料寫入 Excel 文件的指定工作表中，並在現有資料後繼續寫入。
        /// </summary>
        /// <param name="input">包含要寫入 Excel 的資料的列表，每個字典代表一列數據。</param>
        /// <param name="filePath">要保存的 Excel 文件的路徑，支持 .xls 和 .xlsx 格式。</param>
        /// <param name="sheetName">要寫入的工作表名稱。</param>
        /// <exception cref="NotSupportedException">當文件格式不支援時拋出此異常。</exception>
        /// <remarks>
        /// 程式流程大綱：
        /// 1. 根據文件副檔名選擇創建對應的 IWorkbook 實例。
        /// 2. 檢查是否已經存在指定名稱的工作表，如果不存在則創建它。
        /// 3. 獲取工作表中現有的最後一行索引，從該行之後開始寫入新數據。
        /// 4. 如果輸入資料不為空，則執行以下步驟：
        ///    a. 如果工作表是新創建的，添加標題行（從字典的鍵中獲取）。
        ///    b. 添加數據行（從字典的值中獲取）。
        /// 5. 將工作簿寫入指定的文件路徑。
        /// </remarks>
        public static void AppendToExcel(List<Dictionary<string, object?>> input, string filePath, string sheetName)
        {
            IWorkbook workbook;
            ISheet sheet;

            // 打開現有的 Excel 文件，如果文件不存在則創建新的 IWorkbook 實例
            if (File.Exists(filePath))
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    if (filePath.EndsWith(".xlsx"))
                    {
                        workbook = new XSSFWorkbook(fileStream);
                    }
                    else if (filePath.EndsWith(".xls"))
                    {
                        workbook = new HSSFWorkbook(fileStream);
                    }
                    else
                    {
                        throw new NotSupportedException("檔案格式不支援，僅支援 .xls 和 .xlsx 格式。");
                    }
                }
            }
            else
            {
                if (filePath.EndsWith(".xlsx"))
                {
                    workbook = new XSSFWorkbook();
                }
                else if (filePath.EndsWith(".xls"))
                {
                    workbook = new HSSFWorkbook();
                }
                else
                {
                    throw new NotSupportedException("檔案格式不支援，僅支援 .xls 和 .xlsx 格式。");
                }
            }

            sheet = workbook.GetSheet(sheetName) ?? workbook.CreateSheet(sheetName);

            int lastRowIndex = sheet.LastRowNum;

            if (input.Count > 0)
            {
                // 如果是新創建的工作表，添加標題
                if (lastRowIndex == 0)
                {
                    var headers = input[0].Keys.ToList();   // 取出第一筆字典的所有key作為標題
                    IRow headerRow = sheet.CreateRow(0);

                    for (int i = 0; i < headers.Count; i++)
                    {
                        // 如果 key = "blankColumn"，代表要建立空白欄，並依照 value 的數量建立幾個空白欄
                        if (headers[i] == "blankColumn")
                        {
                            var dictValue = input[0][headers[i]]?.ToString() ?? string.Empty;
                            var columnCount = int.Parse(dictValue);
                            for (int j = 0; j < columnCount; j++)
                            {
                                headerRow.CreateCell(i + j).SetCellValue(string.Empty);
                            }
                        }
                        else
                        {
                            headerRow.CreateCell(i).SetCellValue(headers[i]);
                        }
                    }
                    lastRowIndex++;
                }

                // 添加數據
                for (int i = 0; i < input.Count; i++)
                {
                    var row = input[i];
                    IRow dataRow = sheet.CreateRow(lastRowIndex + i + 1);
                    var headers = input[0].Keys.ToList();
                    for (int j = 0; j < headers.Count; j++)
                    {
                        // 如果 key = "blankColumn"，代表要建立空白欄，所以不需要填值
                        if (headers[j] == "blankColumn")
                        {
                            continue;
                        }

                        var cellValue = row.ContainsKey(headers[j]) ? row[headers[j]]?.ToString() ?? string.Empty : string.Empty;
                        dataRow.CreateCell(j).SetCellValue(cellValue);
                    }
                }
            }

            // 保存文件
            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fileStream);
            }
        }


        #endregion

        #region file type process
        public static bool IsImage(IFormFile file)
        {
            // 檢查 MIME 類型是否為圖片
            var imageMimeTypes = new List<string>
            {
                "image/jpeg",  // JPEG
                "image/png",   // PNG
                "image/gif",   // GIF
                "image/bmp",   // BMP
                "image/tiff",  // TIFF
                "image/webp"   // WEBP
            };

            return imageMimeTypes.Contains(file.ContentType.ToLower());
        }

        public static bool IsPdf(IFormFile file)
        {
            // 結合 MIME 類型和文件內容檢查
            if (file.ContentType.ToLower() != "application/pdf")
            {
                return false;
            }

            const int headerLength = 4; // "%PDF" 的長度
            byte[] buffer = new byte[headerLength];
            using (var stream = file.OpenReadStream())
            {
                stream.Read(buffer, 0, headerLength);
            }
            string header = System.Text.Encoding.ASCII.GetString(buffer);
            return header.StartsWith("%PDF");
        }

        public static string GetFileType(IFormFile file)
        {
            string type = string.Empty;

            if (IsImage(file)) { type = "image"; }
            else if (IsPdf(file)) { type = "pdf"; }
            else { type = "other"; }

            return type;
        }

        public static string GetFileType(string filePath)
        {
            string type = string.Empty;

            if (IsImage(filePath)) { type = "image"; }
            else if (IsPdf(filePath)) { type = "pdf"; }
            else { type = "other"; }

            return type;
        }

        public static bool IsImage(string filePath)
        {
            // 檢查副檔名是否為圖片
            var imageExtensions = new List<string>
            {
                ".jpeg",  // JPEG
                ".jpg",   // JPEG
                ".png",   // PNG
                ".gif",   // GIF
                ".bmp",   // BMP
                ".tiff",  // TIFF
                ".webp"   // WEBP
            };

            string extension = Path.GetExtension(filePath).ToLower();
            return imageExtensions.Contains(extension);
        }

        public static bool IsPdf(string filePath)
        {
            // 檢查副檔名是否為 PDF
            if (Path.GetExtension(filePath).ToLower() != ".pdf")
            {
                return false;
            }

            const int headerLength = 4; // "%PDF" 的長度
            byte[] buffer = new byte[headerLength];
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                stream.Read(buffer, 0, headerLength);
            }
            string header = System.Text.Encoding.ASCII.GetString(buffer);
            return header.StartsWith("%PDF");
        }

        #endregion


        /// <summary>
        /// 從 JSON 文件中讀取並反序列化為指定類型。
        /// </summary>
        /// <typeparam name="T">目標類型</typeparam>
        /// <param name="filePath">JSON 文件的路徑</param>
        /// <returns>反序列化後的物件</returns>
        public static T? ReadJsonFile<T>(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("JSON file not found.", filePath);
                }

                // 讀取 JSON 文件內容
                string jsonContent = File.ReadAllText(filePath);

                // 反序列化
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true // 忽略屬性名稱的大小寫
                };
                return JsonSerializer.Deserialize<T>(jsonContent, options);
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException("Failed to parse JSON file. Ensure the file format is correct.", jsonEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while reading the JSON file: {filePath}", ex);
            }
        }

        /// <summary>
        /// 將物件序列化為 JSON 格式並寫入檔案。
        /// 若提供的路徑已存在同名檔案，則會在檔名後加上序號。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="Exception"></exception>
        public static void WriteJsonFile<T>(string filePath, T data)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
                }

                string directory = Path.GetDirectoryName(filePath) ?? string.Empty;
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
                string extension = Path.GetExtension(filePath);
                string newFilePath = filePath;
                int counter = 0;

                // 檢查檔案是否存在，若存在則修改檔名
                while (File.Exists(newFilePath))
                {
                    newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension}_{counter}{extension}");
                    counter++;
                }

                string jsonContent = JsonHelper.Serialize(data);

                // 寫入 JSON 文件
                File.WriteAllText(newFilePath, jsonContent);
            }
            catch (JsonException jsonEx)
            {
                throw new InvalidOperationException("Failed to serialize data to JSON. Ensure the data format is correct.", jsonEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while writing the JSON file: {filePath}", ex);
            }
        }

        /// <summary>
        /// 將製版資料匯出為指定 Excel 檔案
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="Datas"></param>
        /// <param name="targetDate"></param>
        public static bool Export_Plate(string fileName, List<Dictionary<string, object>> Datas, DateTime? targetDate)
        {
            if (Datas.Count == 0)
            {
                return false;
            }

            string date_str = targetDate.HasValue ? $"{targetDate?.ToString("yyyy/MM/dd") ?? ""} - {targetDate?.ToString("yyyy/MM/dd") ?? ""}" : "";
            DateTime nowDate = DateTime.Now;
            // 建立工作簿
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Sheet1");
            #region 設定樣式
            // 建立樣式並轉型為 XSSFCellStyle
            //表頭 無框 16
            XSSFCellStyle title_none_16 = (XSSFCellStyle)workbook.CreateCellStyle();
            title_none_16 = SetCellStyle_border(title_none_16, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None);
            title_none_16 = SetCellStyle_color(title_none_16, new XSSFColor(new byte[] { 255, 255, 255 }));
            title_none_16 = SetCellStyle_font(workbook, title_none_16, false, 16, "新細明體", null);
            //表頭 無框 14 B
            XSSFCellStyle title_none_14_B = (XSSFCellStyle)workbook.CreateCellStyle();
            title_none_14_B = SetCellStyle_border(title_none_14_B, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None);
            title_none_14_B = SetCellStyle_color(title_none_14_B, new XSSFColor(new byte[] { 255, 255, 255 }));
            title_none_14_B = SetCellStyle_font(workbook, title_none_14_B, true, 14, "新細明體", null);
            //表頭 無框 9
            XSSFCellStyle title_none_9 = (XSSFCellStyle)workbook.CreateCellStyle();
            title_none_9 = SetCellStyle_border(title_none_9, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None);
            title_none_9 = SetCellStyle_color(title_none_9, new XSSFColor(new byte[] { 255, 255, 255 }));
            title_none_9 = SetCellStyle_font(workbook, title_none_9, false, 9, "新細明體", null);
            //表頭 無框 8 B
            XSSFCellStyle title_none_8_B = (XSSFCellStyle)workbook.CreateCellStyle();
            title_none_8_B = SetCellStyle_Alignment(title_none_8_B, HorizontalAlignment.Left, VerticalAlignment.Center);
            title_none_8_B = SetCellStyle_border(title_none_8_B, BorderStyle.Hair, BorderStyle.Hair, BorderStyle.Hair, BorderStyle.Hair);
            title_none_8_B = SetCellStyle_font(workbook, title_none_8_B, true, 8, "新細明體", null);
            // 設定字型格式 9
            XSSFCellStyle font_9_Style = (XSSFCellStyle)workbook.CreateCellStyle();
            font_9_Style = SetCellStyle_Alignment(font_9_Style, HorizontalAlignment.General, VerticalAlignment.Center);
            font_9_Style = SetCellStyle_border(font_9_Style, BorderStyle.Hair, BorderStyle.Hair, BorderStyle.Hair, BorderStyle.Hair);
            font_9_Style = SetCellStyle_font(workbook, font_9_Style, false, 9, "新細明體", null);
            //標頭白
            XSSFCellStyle title_white = (XSSFCellStyle)workbook.CreateCellStyle();
            title_white = SetCellStyle_Alignment(title_white, HorizontalAlignment.Center, VerticalAlignment.Center);
            title_white = SetCellStyle_border(title_white, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Medium, BorderStyle.Medium);
            title_white = SetCellStyle_color(title_white, new XSSFColor(new byte[] { 255, 255, 255 }));
            title_white = SetCellStyle_font(workbook, title_white, false, 8, null, null);
            //標頭(補充)
            XSSFCellStyle additional_white = (XSSFCellStyle)workbook.CreateCellStyle();
            additional_white = SetCellStyle_Alignment(additional_white, HorizontalAlignment.Center, VerticalAlignment.Center);
            additional_white = SetCellStyle_font(workbook, additional_white, false, 12, null, null);
            additional_white = SetCellStyle_color(additional_white, new XSSFColor(new byte[] { 255, 255, 255 }));
            // 設定日期格式
            XSSFCellStyle dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            IDataFormat format = workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy/MM/dd");
            #endregion

            // 取出所有欄位（假設所有 dictionary 欄位相同）
            var headers = Datas[0].Keys.ToList();
            headers.Add("description"); // 新稿欄位

            int addition_columns = 26;//設定空白用
            int page_limit = 35; // 每頁可放置資料的行數
            int fixed_rows = 6; // 每頁固定占用的行數
            decimal pages_count = (decimal)Datas.Count / page_limit;
            int pages = (int)Math.Ceiling(pages_count); // 計算當前頁數

            int final_row = 0;

            // 依頁數放資料
            for (int i = 0; i < pages; i++)
            {
                #region 固定頁面標題
                //每頁第一列
                int now_row = i * (page_limit + fixed_rows);

                IRow row1 = sheet.CreateRow(now_row++);
                row1.Height = 440;//設定列高
                ICell row1_Cell = row1.CreateCell(0);
                row1_Cell.SetCellValue($"                                                      立墩股份有限公司");
                row1_Cell.CellStyle = title_none_16;
                //設定格式
                for (int j = 1; j < addition_columns; j++)
                {
                    ICell rowCell = row1.CreateCell(j);
                    rowCell.CellStyle = additional_white; // 設定樣式為無邊框
                }

                IRow row2 = sheet.CreateRow(now_row++);
                row2.Height = 360;//設定列高
                ICell row2_Cell = row2.CreateCell(0);
                row2_Cell.SetCellValue($"                                                         生產憑單新版表");
                row2_Cell.CellStyle = title_none_14_B;
                //設定格式
                for (int j = 1; j < addition_columns; j++)
                {
                    ICell rowCell = row2.CreateCell(j);
                    rowCell.CellStyle = additional_white; // 設定樣式為無邊框
                }

                IRow row3 = sheet.CreateRow(now_row++);
                ICell row3_Cell = row3.CreateCell(0);
                row3_Cell.SetCellValue($"TO： 三力企業                                       聯絡人： 黃先生     TEL： 04-23502492                            FAX： 04-23506051");
                row3_Cell.CellStyle = title_none_9;
                //設定格式
                for (int j = 1; j < addition_columns; j++)
                {
                    ICell rowCell = row3.CreateCell(j);
                    rowCell.CellStyle = additional_white; // 設定樣式為無邊框
                }

                IRow row4 = sheet.CreateRow(now_row++);
                ICell row4_Cell = row4.CreateCell(0);
                row4_Cell.SetCellValue($"憑單期間： {date_str}                                                    憑單起迄：  -   製表日期:{nowDate.ToString("yyyy/MM/dd_HH:mm:ss")}  頁次:{i + 1}");
                row4_Cell.CellStyle = title_none_9;
                //設定格式
                for (int j = 1; j < addition_columns; j++)
                {
                    ICell rowCell = row4.CreateCell(j);
                    rowCell.CellStyle = additional_white; // 設定樣式為無邊框
                }

                //放置標題
                IRow row5 = sheet.CreateRow(now_row++);
                for (int j = 0; j < headers.Count; j++)
                {
                    switch (headers[j])
                    {
                        case "isNew":
                            row5.CreateCell(j).SetCellValue("新稿");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "poNumber":
                            row5.CreateCell(j).SetCellValue("憑單單號");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "receiptCode":
                            row5.CreateCell(j).SetCellValue("認稿碼");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "format":
                            row5.CreateCell(j).SetCellValue("規格");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "partName":
                            row5.CreateCell(j).SetCellValue("品名(版面名稱)");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "partNumber":
                            row5.CreateCell(j).SetCellValue("產品編號");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "moldQuantity":
                            row5.CreateCell(j).SetCellValue("模數");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "color":
                            row5.CreateCell(j).SetCellValue("色數");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "knifeMoldNumber":
                            row5.CreateCell(j).SetCellValue("刀模編號");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        case "description":
                            row5.CreateCell(j).SetCellValue("說明");
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                        default:
                            row5.CreateCell(j).SetCellValue(headers[j]);
                            row5.GetCell(j).CellStyle = title_none_8_B;
                            break;
                    }
                }
                //設定格式
                for (int j = headers.Count; j < addition_columns; j++)
                {
                    ICell rowCell = row5.CreateCell(j);
                    rowCell.CellStyle = additional_white; // 設定樣式為無邊框
                }
                #endregion

                #region 放置內容
                for (int j = i * page_limit; j < Datas.Count; j++)
                {
                    if (j > (i + 1) * page_limit - 1)
                    {
                        break;
                    }
                    IRow row = sheet.CreateRow(now_row++);
                    for (int k = 0; k < headers.Count; k++)
                    {
                        ICell cell = row.CreateCell(k);
                        if (!Datas[j].ContainsKey(headers[k]))
                        {
                            cell.SetCellValue("");
                        }
                        else
                        {
                            if (string.Equals(headers[k], "moldQuantity"))
                            {
                                bool pars = decimal.TryParse((Datas[j][headers[k]] == null) ? "" : Datas[j][headers[k]].ToString(), out decimal moldQuantity);
                                cell.SetCellValue((pars) ? (int)moldQuantity : 0);
                            }
                            else
                            {
                                cell.SetCellValue((Datas[j][headers[k]] == null) ? "" : Datas[j][headers[k]].ToString());
                            }
                        }
                        cell.CellStyle = font_9_Style;
                    }
                    for (int k = headers.Count; k < 26; k++)
                    {
                        ICell rowCell = row.CreateCell(k);
                        rowCell.CellStyle = additional_white; // 設定樣式為無邊框
                    }
                }
                #endregion

                IRow row6 = sheet.CreateRow(now_row++);
                ICell row6_Cell = row6.CreateCell(0);
                row6_Cell.CellStyle = font_9_Style;
                //設定格式
                for (int j = 1; j < headers.Count; j++)
                {
                    ICell rowCell = row6.CreateCell(j);
                    rowCell.CellStyle = font_9_Style;
                }
                //設定格式
                for (int j = headers.Count; j < addition_columns; j++)
                {
                    ICell rowCell = row6.CreateCell(j);
                    rowCell.CellStyle = additional_white; // 設定樣式為無邊框
                }

                final_row = now_row; // 記錄最後一行的索引
            }

            //對資料下方儲存格設定格式
            for (int i = final_row; i <= 1000; i++)
            {
                IRow row = sheet.CreateRow(i);
                for (int j = 0; j < addition_columns; j++)
                {
                    ICell cell = row.CreateCell(j);
                    cell.CellStyle = additional_white; // 設定樣式為無邊框
                }
            }

            #region 調整欄寬
            for (int colIndex = 0; colIndex < headers.Count; colIndex++)
            {
                int colWidth = 4;// 預設寬度為 4 個字元
                // 設定欄寬
                if (colIndex == 0)
                {
                    colWidth = 4 * 256;//寬度為 4 個字元
                }
                else if (colIndex == 1)
                {
                    colWidth = 10 * 256; // 寬度為 10 個字元
                }
                else if (colIndex == 2)
                {
                    colWidth = 8 * 256; // 寬度為 8 個字元
                }
                else if (colIndex == 3)
                {
                    colWidth = 8 * 256; // 寬度為 8 個字元
                }
                else if (colIndex == 4)
                {
                    colWidth = 20 * 256; // 寬度為 20 個字元
                }
                else if (colIndex == 5)
                {
                    colWidth = 16 * 256; // 寬度為 16 個字元
                }
                else if (colIndex == 6)
                {
                    colWidth = 4 * 256; // 寬度為 4 個字元
                }
                else if (colIndex == 7)
                {
                    colWidth = 4 * 256; // 寬度為 4 個字元
                }
                else if (colIndex == 8)
                {
                    colWidth = 8 * 256; // 寬度為 8 個字元
                }
                else if (colIndex == 9)
                {
                    colWidth = 8 * 256; // 寬度為 8 個字元
                }

                sheet.SetColumnWidth(colIndex, colWidth);
            }
            #endregion

            // 儲存成檔案
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            return true;
        }

        /// <summary>
        /// 將PODetail Excel資料匯出為指定 Excel 檔案 
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="Datas"></param>
        /// <param name="input"></param>
        public static bool Export_PODetail(string fileName, List<Dictionary<string, object>> Datas, Dictionary<string, object> input)
        {
            if (Datas.Count == 0)
            {
                return false;
            }

            DateTime nowDate = DateTime.Now;
            // 建立工作簿
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Sheet1");
            #region 設定樣式
            // 建立樣式並轉型為 XSSFCellStyle
            // 無框 16
            XSSFCellStyle none_16 = (XSSFCellStyle)workbook.CreateCellStyle();
            none_16 = SetCellStyle_border(none_16, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None);
            none_16 = SetCellStyle_font(workbook, none_16, false, 16, "新細明體", null);
            //表頭 無框 14 B
            XSSFCellStyle title_none_14_B = (XSSFCellStyle)workbook.CreateCellStyle();
            title_none_14_B = SetCellStyle_border(title_none_14_B, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None);
            title_none_14_B = SetCellStyle_font(workbook, title_none_14_B, true, 14, "新細明體", null);
            //表頭 無框 9
            XSSFCellStyle title_none_9 = (XSSFCellStyle)workbook.CreateCellStyle();
            title_none_9 = SetCellStyle_border(title_none_9, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None);
            title_none_9 = SetCellStyle_color(title_none_9, new XSSFColor(new byte[] { 255, 255, 255 }));
            title_none_9 = SetCellStyle_font(workbook, title_none_9, false, 9, "新細明體", null);
            //表頭 無框 8 B
            XSSFCellStyle title_none_8_B = (XSSFCellStyle)workbook.CreateCellStyle();
            title_none_8_B = SetCellStyle_Alignment(title_none_8_B, HorizontalAlignment.Left, VerticalAlignment.Center);
            title_none_8_B = SetCellStyle_border(title_none_8_B, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None);
            title_none_8_B = SetCellStyle_font(workbook, title_none_8_B, true, 8, "新細明體", null);
            // 設定字型格式 9
            XSSFCellStyle font_9_Style = (XSSFCellStyle)workbook.CreateCellStyle();
            font_9_Style = SetCellStyle_Alignment(font_9_Style, HorizontalAlignment.General, VerticalAlignment.Center);
            font_9_Style = SetCellStyle_border(font_9_Style, BorderStyle.None, BorderStyle.None, BorderStyle.None, BorderStyle.None);
            font_9_Style = SetCellStyle_font(workbook, font_9_Style, false, 9, "新細明體", null);
            // 設定日期格式
            XSSFCellStyle dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            IDataFormat format = workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy/MM/dd");
            #endregion

            // 取出所有欄位（假設所有 dictionary 欄位相同）
            var headers = Datas[0].Keys.ToList();

            int datas_count = Datas.Count;
            IRow row0 = sheet.CreateRow(0);
            for (int j = 0; j < headers.Count; j++)
            {
                switch (headers[j])
                {
                    case "partNumber":
                        row0.CreateCell(j).SetCellValue("料號");
                        row0.GetCell(j).CellStyle = title_none_14_B;
                        break;
                    case "partName":
                        row0.CreateCell(j).SetCellValue("品名");
                        row0.GetCell(j).CellStyle = title_none_14_B;
                        break;
                    case "number":
                        row0.CreateCell(j).SetCellValue("工單編號");
                        row0.GetCell(j).CellStyle = title_none_14_B;
                        break;
                    case "orderQuantity":
                        row0.CreateCell(j).SetCellValue("工單需求數");
                        row0.GetCell(j).CellStyle = title_none_14_B;
                        break;
                    case "print":
                        row0.CreateCell(j).SetCellValue("印刷");
                        row0.GetCell(j).CellStyle = title_none_14_B;
                        break;
                    case "rolling":
                        row0.CreateCell(j).SetCellValue("軋型");
                        row0.GetCell(j).CellStyle = title_none_14_B;
                        break;
                    case "pulling":
                        row0.CreateCell(j).SetCellValue("拔紙");
                        row0.GetCell(j).CellStyle = title_none_14_B;
                        break;
                    default:
                        row0.CreateCell(j).SetCellValue(headers[j]);
                        row0.GetCell(j).CellStyle = title_none_14_B;
                        break;
                }
            }
            // 放資料
            for (int i = 0; i < datas_count; i++)
            {
                IRow row = sheet.CreateRow(i + 1);
                //第一列放置標題

                #region 放置內容
                for (int k = 0; k < headers.Count; k++)
                {
                    ICell cell = row.CreateCell(k);
                    if (!Datas[i].ContainsKey(headers[k]))
                    {
                        cell.SetCellValue("");
                    }
                    else
                    {
                        if (decimal.TryParse((Datas[i][headers[k]] == null) ? "" : Datas[i][headers[k]].ToString(), out decimal moldQuantity))
                        {
                            cell.SetCellValue((double)moldQuantity);
                        }
                        else
                        {
                            cell.SetCellValue((Datas[i][headers[k]] == null) ? "" : Datas[i][headers[k]].ToString());
                        }
                    }
                    cell.CellStyle = none_16;
                }
                #endregion
            }

            int finalRow = datas_count + 4;
            IRow final_row = sheet.CreateRow(finalRow);
            ICell final_row_Cell = final_row.CreateCell(0);
            final_row_Cell.SetCellValue($"搜尋條件");
            final_row_Cell.CellStyle = title_none_14_B;

            int finalRow1 = datas_count + 5;
            IRow final_row1 = sheet.CreateRow(finalRow1);
            final_row1.Height = 1600;
            ICell final_row_Cell1 = final_row1.CreateCell(0);
            final_row_Cell1.SetCellValue($"料號:{input["partNumber"]}\n客戶名稱:{input["customer"]}\n規格:{input["formats"]}\n開始時間:{input["startTime"]}\n結束時間:{input["endTime"]}");
            final_row_Cell1.CellStyle = none_16;
            FileUtils fileUtils = new FileUtils();
            fileUtils.AutoSizeColumnsWithoutHeader(sheet, headers.Count);

            // 儲存成檔案
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            return true;
        }

        /// <summary>
        /// 將生產日報表匯出為指定 Excel 檔案
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="Datas"></param>
        public static bool Export_Daily(string fileName, List<Dictionary<string, object?>> Datas)
        {
            if (Datas.Count == 0)
            {
                return false;
            }

            DateTime nowDate = DateTime.Now;
            // 建立工作簿
            IWorkbook workbook = new XSSFWorkbook();
            ISheet sheet = workbook.CreateSheet("Sheet1");
            #region 設定樣式
            // 建立樣式並轉型為 XSSFCellStyle
            //標頭紅粗
            XSSFCellStyle title_red_bold = (XSSFCellStyle)workbook.CreateCellStyle();
            title_red_bold = SetCellStyle_Alignment(title_red_bold, HorizontalAlignment.Center, VerticalAlignment.Center);
            title_red_bold = SetCellStyle_font(workbook, title_red_bold, true, null, null, IndexedColors.Red.Index);

            // 設定日期格式
            XSSFCellStyle dateStyle = (XSSFCellStyle)workbook.CreateCellStyle();
            IDataFormat format = workbook.CreateDataFormat();
            dateStyle.DataFormat = format.GetFormat("yyyy-MM-dd hh:mm:ss");
            XSSFCellStyle dateStyle1 = (XSSFCellStyle)workbook.CreateCellStyle();
            IDataFormat format1 = workbook.CreateDataFormat();
            dateStyle1.DataFormat = format1.GetFormat("yyyy/MM/dd");
            #endregion

            // 取出所有欄位（假設所有 dictionary 欄位相同）
            List<string> headers = new List<string>{
                "actualReportDate", "productionUnitNumber", "partName", "productionQuantity", "fixedCapacity", "boardQuantity", "poNumber", "outputBatchNumber", "format",
                "workflow", "processTemplateNumber", "processTemplateName", "shifts", "shiftsDes", "reporterName", "reportStartTime", "reportEndTime", "faultyReason"
            };

            #region 固定頁面標題
            IRow row1 = sheet.CreateRow(0);
            for (int j = 0; j < headers.Count; j++)
            {
                switch (headers[j])
                {
                    case "actualReportDate":
                        row1.CreateCell(j).SetCellValue("生產日");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "productionUnitNumber":
                        row1.CreateCell(j).SetCellValue("機台編號");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "partName":
                        row1.CreateCell(j).SetCellValue("品名");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "productionQuantity":
                        row1.CreateCell(j).SetCellValue("產出量");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "fixedCapacity":
                        row1.CreateCell(j).SetCellValue("定容量");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "boardQuantity":
                        row1.CreateCell(j).SetCellValue("板數");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "poNumber":
                        row1.CreateCell(j).SetCellValue("憑單單號");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "outputBatchNumber":
                        row1.CreateCell(j).SetCellValue("物料批號");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "format":
                        row1.CreateCell(j).SetCellValue("產品型號");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "workflow":
                        row1.CreateCell(j).SetCellValue("製程序");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "processTemplateNumber":
                        row1.CreateCell(j).SetCellValue("製程 NO");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "processTemplateName":
                        row1.CreateCell(j).SetCellValue("製程說明");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "shifts":
                        row1.CreateCell(j).SetCellValue("班別");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "shiftsDes":
                        row1.CreateCell(j).SetCellValue("班別說明");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "reporterName":
                        row1.CreateCell(j).SetCellValue("操作員");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "reportStartTime":
                        row1.CreateCell(j).SetCellValue("實際起始 (時:分:秒)");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "reportEndTime":
                        row1.CreateCell(j).SetCellValue("截止時間 (時:分:秒)");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    case "faultReason":
                        row1.CreateCell(j).SetCellValue("無效代碼");
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                    default:
                        row1.CreateCell(j).SetCellValue(headers[j]);
                        row1.GetCell(j).CellStyle = title_red_bold;
                        break;
                }
            }
            #endregion

            #region 放置內容
            for (int j = 0; j < Datas.Count; j++)
            {
                IRow row = sheet.CreateRow(j + 1);
                for (int k = 0; k < headers.Count; k++)
                {
                    ICell cell = row.CreateCell(k);
                    if (!Datas[j].ContainsKey(headers[k]))
                    {
                        cell.SetCellValue("尚未結算");
                        continue;
                    }
                    string? value_string = Datas[j][headers[k]]?.ToString();
                    // 檢查是否為日期格式
                    if (DateTime.TryParse(value_string, out DateTime dateTime))
                    {
                        if (headers[k] == "actualReportDate")
                        {
                            cell.CellStyle = dateStyle1; // 設定日期格式樣式
                        }
                        else
                        {
                            cell.CellStyle = dateStyle; // 設定日期格式樣式
                        }

                        cell.SetCellValue(dateTime);
                    }
                    else if (decimal.TryParse(value_string, out decimal decimalValue))
                    {
                        cell.SetCellValue((double)decimalValue);
                    }
                    else
                    {
                        cell.SetCellValue(value_string ?? "");
                    }
                }
            }
            #endregion

            #region 調整欄寬
            FileUtils fileUtils = new FileUtils();
            fileUtils.AutoSizeColumnsWithoutHeader(sheet, headers.Count + 1);
            #endregion

            // 儲存成檔案
            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
            {
                workbook.Write(fs);
            }
            return true;
        }

        /// <summary>
        /// 自動調整欄寬（不包含表頭）
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="colCount"></param>
        private void AutoSizeColumnsWithoutHeader(ISheet sheet, int colCount)
        {
            for (int colIndex = 0; colIndex < colCount; colIndex++)
            {
                sheet.AutoSizeColumn(colIndex);
                // 若太窄可補足最小值
                int maxWidth = (int)sheet.GetColumnWidth(colIndex);
                sheet.SetColumnWidth(colIndex, Math.Max(maxWidth + 2, 10 * 256));
            }
        }

        /// <summary>
        /// 設定格式(對齊)
        /// </summary>
        /// <param name="cellStyle"></param>
        /// <param name="alignment"></param>
        /// <param name="horizontal"></param>
        /// <returns></returns>
        private static XSSFCellStyle SetCellStyle_Alignment(XSSFCellStyle cellStyle, HorizontalAlignment alignment, VerticalAlignment horizontal)
        {
            // 對齊方式
            cellStyle.Alignment = alignment;
            cellStyle.VerticalAlignment = horizontal;
            return cellStyle;
        }

        /// <summary>
        /// 設定格式(邊框)
        /// </summary>
        /// <param name="cellStyle"></param>
        /// <param name="top"></param>
        /// <param name="bottom"></param>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private static XSSFCellStyle SetCellStyle_border(XSSFCellStyle cellStyle, BorderStyle top, BorderStyle bottom, BorderStyle left, BorderStyle right)
        {
            //邊框設定
            cellStyle.BorderTop = top;
            cellStyle.BorderBottom = bottom;
            cellStyle.BorderLeft = left;
            cellStyle.BorderRight = right;
            return cellStyle;
        }

        /// <summary>
        /// 設定格式(背景色)
        /// </summary>
        /// <param name="cellStyle"></param>
        /// <param name="color">new XSSFColor(new byte[] { R, G, B })</param>
        /// <returns></returns>
        private static XSSFCellStyle SetCellStyle_color(XSSFCellStyle cellStyle, XSSFColor color)
        {
            //自訂顏色（RGB）
            cellStyle.FillPattern = FillPattern.SolidForeground;
            cellStyle.SetFillForegroundColor(color);
            return cellStyle;
        }

        /// <summary>
        /// 設定格式(字型)
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="cellStyle"></param>
        /// <param name="bold"></param>
        /// <param name="fontHeight"></param>
        /// <param name="fontname"></param>
        /// <param name="fontColor"> IndexedColors.Red.Index </param>
        /// <returns></returns>
        private static XSSFCellStyle SetCellStyle_font(IWorkbook workbook, XSSFCellStyle cellStyle, bool bold, double? fontHeight, string? fontname, short? fontColor)
        {
            // 設定字型
            IFont font = workbook.CreateFont();
            font.IsBold = bold;
            if (fontHeight.HasValue)
            {
                font.FontHeightInPoints = fontHeight.Value;
            }
            if (!string.IsNullOrEmpty(fontname))
            {
                font.FontName = fontname;
            }
            if (fontColor.HasValue)
            {
                font.Color = fontColor.Value; //IndexedColors.Red.Index
            }
            cellStyle.SetFont(font);
            return cellStyle;
        }

        /// <summary>
        /// 將 Excel 欄位索引轉換為字母（A-Z, AA-ZZ）
        /// </summary>
        /// <param name="index">程式會內加1(因起始值為1)</param>
        /// <returns></returns>
        private static string ColumnIndexToLetter(int index)
        {
            index++;

            string column = "";
            while (index > 0)
            {
                index--; // Excel 是 1-based，但計算是 0-based
                column = (char)('A' + (index % 26)) + column;
                index /= 26;
            }
            return column;
        }

    }
}
