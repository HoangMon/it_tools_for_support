using System.Net.Http;
using System.Text;
using System.Text.Json;
using Ernastie.Models;

namespace Ernastie.Services;

public class DeviceSyncPayload
{
    public string Action { get; set; } = "upload";
    public string MachineName { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string Cpu { get; set; } = string.Empty;
    public string Ram { get; set; } = string.Empty;
    public string ProductKey { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public bool BothSheets { get; set; } = false;
    public string Timestamp { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}

public class GoogleSheetService
{
    private readonly string _configFile;

    public GoogleSheetService()
    {
        _configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sheet_config.json");
    }

    public DeviceConfigModel LoadConfig()
    {
        if (File.Exists(_configFile))
        {
            try
            {
                var json = File.ReadAllText(_configFile);
                var cfg = JsonSerializer.Deserialize<DeviceConfigModel>(json);
                if (cfg != null) return cfg;
            }
            catch { }
        }

        return new DeviceConfigModel
        {
            MachineNames = new List<string> { Environment.MachineName, "PC-ADMIN", "PC-ACCOUNTANT", "PC-IT-SUPPORT" },
            Companies = new List<string> { "Công ty TNHH Mẫu", "Văn phòng Chính", "Chi nhánh 1" }
        };
    }

    public void SaveConfig(DeviceConfigModel config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFile, json);
        }
        catch { }
    }

    public async Task<bool> CheckConnectionAsync(string appsScriptUrl)
    {
        if (string.IsNullOrWhiteSpace(appsScriptUrl)) return false;
        try
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = true };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
            var res = await client.GetAsync(appsScriptUrl);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message)> UploadDeviceInfoAsync(string appsScriptUrl, DeviceSyncPayload payload, Action<int>? progress = null)
    {
        if (string.IsNullOrWhiteSpace(appsScriptUrl))
        {
            return (false, "Chưa cấu hình Link Apps Script URL! Vui lòng nhập link ở phần Liên kết Google Sheet.");
        }

        try
        {
            progress?.Invoke(20);
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true,
                MaxAutomaticRedirections = 5
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(35) };

            progress?.Invoke(40);
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            progress?.Invoke(65);
            var response = await client.PostAsync(appsScriptUrl, content);

            progress?.Invoke(90);
            if (response.IsSuccessStatusCode)
            {
                var respBody = await response.Content.ReadAsStringAsync();
                progress?.Invoke(100);
                return (true, $"Đã cập nhật thông tin thiết bị lên Google Sheet thành công!\nPhản hồi: {respBody}");
            }
            else
            {
                progress?.Invoke(0);
                return (false, $"Lỗi máy chủ Google (HTTP {(int)response.StatusCode} {response.ReasonPhrase}).");
            }
        }
        catch (Exception ex)
        {
            progress?.Invoke(0);
            return (false, $"Lỗi truyền dữ liệu tới Google Apps Script: {ex.Message}");
        }
    }

    public async Task<(bool Success, List<DeviceSyncPayload> Devices, string Message)> FetchDevicesAsync(string appsScriptUrl)
    {
        if (string.IsNullOrWhiteSpace(appsScriptUrl))
        {
            return (false, new List<DeviceSyncPayload>(), "Chưa nhập Link Apps Script URL!");
        }

        try
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = true, MaxAutomaticRedirections = 5 };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };

            string url = appsScriptUrl.Contains('?') ? $"{appsScriptUrl}&action=getDevices" : $"{appsScriptUrl}?action=getDevices";
            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return (false, new List<DeviceSyncPayload>(), $"Lỗi HTTP: {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var list = new List<DeviceSyncPayload>();

            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    list.Add(ParseDeviceElement(item));
                }
            }
            else if (doc.RootElement.TryGetProperty("devices", out var devArr) && devArr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in devArr.EnumerateArray())
                {
                    list.Add(ParseDeviceElement(item));
                }
            }

            return (true, list, $"Đã tải về thành công {list.Count} thiết bị từ Google Sheet.");
        }
        catch (Exception ex)
        {
            return (false, new List<DeviceSyncPayload>(), $"Lỗi đọc dữ liệu Google Sheet: {ex.Message}");
        }
    }

    private static DeviceSyncPayload ParseDeviceElement(JsonElement item)
    {
        var d = new DeviceSyncPayload();
        if (item.TryGetProperty("MachineName", out var m)) d.MachineName = m.GetString() ?? "";
        if (item.TryGetProperty("Company", out var c)) d.Company = c.GetString() ?? "";
        if (item.TryGetProperty("SerialNumber", out var s)) d.SerialNumber = s.GetString() ?? "";
        if (item.TryGetProperty("ProductKey", out var p)) d.ProductKey = p.GetString() ?? "";
        if (item.TryGetProperty("Cpu", out var cpu)) d.Cpu = cpu.GetString() ?? "";
        if (item.TryGetProperty("Ram", out var r)) d.Ram = r.GetString() ?? "";
        if (item.TryGetProperty("OsVersion", out var os)) d.OsVersion = os.GetString() ?? "";
        if (item.TryGetProperty("Manufacturer", out var mf)) d.Manufacturer = mf.GetString() ?? "";
        if (item.TryGetProperty("Model", out var mdl)) d.Model = mdl.GetString() ?? "";
        if (item.TryGetProperty("MacAddress", out var mac)) d.MacAddress = mac.GetString() ?? "";
        return d;
    }

    public string GenerateAppsScriptTemplate()
    {
        return @"// =========================================================================
// GOOGLE APPS SCRIPT CHO ERNASTIE SUPPORT TOOL (QUẢN LÝ THIẾT BỊ IT SUPPORT)
// Hướng dẫn cài đặt:
// 1. Mở Google Sheet > Extensions (Tiện ích mở rộng) > Apps Script
// 2. Dán toàn bộ mã nguồn bên dưới vào file Code.gs
// 3. Bấm Deploy (Triển khai) > New deployment (Triển khai mới)
// 4. Chọn Type: Web app
//    - Execute as: Me (tài khoản của bạn)
//    - Who has access: Anyone (Bất kỳ ai)
// 5. Sao chép 'Web app URL' và dán vào ô 'Link triển khai Apps Script' trong Tool!
// =========================================================================

function doGet(e) {
  var action = (e && e.parameter && e.parameter.action) ? e.parameter.action : '';
  var sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
  
  if (action === 'getDevices') {
    var data = sheet.getDataRange().getValues();
    var devices = [];
    // Bỏ qua dòng tiêu đề row 0
    for (var i = 1; i < data.length; i++) {
      var row = data[i];
      if (row[1] || row[7]) { // Có tên máy hoặc serial
        devices.push({
          Timestamp: row[0] ? row[0].toString() : '',
          MachineName: row[1] ? row[1].toString() : '',
          Company: row[2] ? row[2].toString() : '',
          OsVersion: row[3] ? row[3].toString() : '',
          Cpu: row[4] ? row[4].toString() : '',
          Ram: row[5] ? row[5].toString() : '',
          ProductKey: row[6] ? row[6].toString() : '',
          SerialNumber: row[7] ? row[7].toString() : '',
          Manufacturer: row[8] ? row[8].toString() : '',
          Model: row[9] ? row[9].toString() : '',
          MacAddress: row[10] ? row[10].toString() : '',
          Status: row[11] ? row[11].toString() : 'Active'
        });
      }
    }
    return ContentService.createTextOutput(JSON.stringify({ status: 'ok', devices: devices }))
      .setMimeType(ContentService.MimeType.JSON);
  }
  
  return ContentService.createTextOutput(JSON.stringify({ status: 'ok', message: 'Ernastie Apps Script Backend is Running!', time: new Date() }))
    .setMimeType(ContentService.MimeType.JSON);
}

function doPost(e) {
  try {
    var sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
    var data = JSON.parse(e.postData.contents);
    
    // Nếu sheet rỗng, tạo dòng tiêu đề
    if (sheet.getLastRow() === 0) {
      sheet.appendRow([
        'Thời Gian', 'Tên Máy', 'Công Ty', 'Hệ Điều Hành',
        'CPU', 'RAM', 'Windows Product Key', 'Số Serial / ServiceTag',
        'Hãng Sản Xuất', 'Model Máy', 'MAC Address', 'Trạng Thái'
      ]);
      sheet.getRange(1, 1, 1, 12).setFontWeight('bold').setBackground('#E2E8F0');
    }
    
    var serial = data.SerialNumber || '';
    var machine = data.MachineName || '';
    var updated = false;
    
    // Tìm kiếm xem máy đã có trong Sheet chưa để CẬP NHẬT thay vì trùng lặp
    var values = sheet.getDataRange().getValues();
    for (var i = 1; i < values.length; i++) {
      var rowSerial = values[i][7] ? values[i][7].toString() : '';
      var rowMachine = values[i][1] ? values[i][1].toString() : '';
      
      if ((serial && rowSerial === serial) || (!serial && machine && rowMachine === machine)) {
        // Cập nhật dòng hiện có
        sheet.getRange(i + 1, 1, 1, 12).setValues([[
          new Date(),
          data.MachineName || '',
          data.Company || '',
          data.OsVersion || '',
          data.Cpu || '',
          data.Ram || '',
          data.ProductKey || '',
          data.SerialNumber || '',
          data.Manufacturer || '',
          data.Model || '',
          data.MacAddress || '',
          data.Status || 'Active'
        ]]);
        updated = true;
        break;
      }
    }
    
    if (!updated) {
      sheet.appendRow([
        new Date(),
        data.MachineName || '',
        data.Company || '',
        data.OsVersion || '',
        data.Cpu || '',
        data.Ram || '',
        data.ProductKey || '',
        data.SerialNumber || '',
        data.Manufacturer || '',
        data.Model || '',
        data.MacAddress || '',
        data.Status || 'Active'
      ]);
    }
    
    return ContentService.createTextOutput(JSON.stringify({
      result: 'success',
      action: updated ? 'updated' : 'inserted',
      machine: data.MachineName
    })).setMimeType(ContentService.MimeType.JSON);
  } catch (err) {
    return ContentService.createTextOutput(JSON.stringify({ result: 'error', message: err.toString() }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}";
    }
}
