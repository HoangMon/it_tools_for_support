using System.Text.Json;
using Ernastie.Models;

namespace Ernastie.Services;

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
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var res = await client.GetAsync(appsScriptUrl);
            return res.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public string GenerateAppsScriptTemplate()
    {
        return @"// Google Apps Script Deploy Template (Dán vào Extensions > Apps Script của Google Sheet)
function doGet(e) {
  return ContentService.createTextOutput(JSON.stringify({ status: 'ok', time: new Date() }))
    .setMimeType(ContentService.MimeType.JSON);
}

function doPost(e) {
  try {
    var sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
    var data = JSON.parse(e.postData.contents);
    sheet.appendRow([
      new Date(),
      data.MachineName || '',
      data.Company || '',
      data.OsVersion || '',
      data.Cpu || '',
      data.Ram || '',
      data.ProductKey || '',
      data.SerialNumber || '',
      data.Status || 'Active'
    ]);
    return ContentService.createTextOutput(JSON.stringify({ result: 'success' }))
      .setMimeType(ContentService.MimeType.JSON);
  } catch (err) {
    return ContentService.createTextOutput(JSON.stringify({ result: 'error', message: err.toString() }))
      .setMimeType(ContentService.MimeType.JSON);
  }
}";
    }
}
