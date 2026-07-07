using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
public class InstrumentInfo
{
    public int NO { get; set; }
    public string Tag { get; set; }
    public string Function { get; set; }
    public string PointType { get; set; }
    public EDataCategory Type { get; set; }
    public string InstrumentType { get; set; }
    public string Measurement { get; set; }
    public string Group { get; set; }
    public string System { get; set; }
    public string DataType { get; set; }
    public float RangeMin { get; set; }
    public float RangeMax { get; set; }
    public float PLCMin { get; set; }
    public float PLCMax { get; set; }
    public int Address { get; set; }
    public bool Useable { get; set; }
    public string Description { get; set; }
    public string Note { get; set; }
}

public class DocumentController
{
    public Dictionary<string, InstrumentInfo> InstrumentInfos { get; private set; }
        = new Dictionary<string, InstrumentInfo>();

    public Dictionary<string, ExperimentWrapper> ExperimentDefines { get; private set; }
        = new Dictionary<string, ExperimentWrapper>();

    public List<ExperimentInfo> ExperimentInfos { get; private set; }
        = new List<ExperimentInfo>();

    public bool LoadDocument()
    {
        try
        {
            string path = Path.Combine(Application.streamingAssetsPath, "DataTable.xlsx");

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateOpenXmlReader(stream))
            {
                var dataSet = reader.AsDataSet();
                var tables = dataSet.Tables;

                InstrumentInfos = LoadTable<InstrumentInfo>(
                    tables["IO_List"],
                    "Tag"
                );

                ExperimentInfos = LoadTable<ExperimentInfo>(
                    tables["ExperimentInfo"],
                    "No"
                ).Values.ToList();

                ExperimentDefines = LoadTable<ExperimentWrapper>(
                    tables["ExperimentDefineTable"],
                    "Group"
                );
            }

            Debug.Log("Excel Load Complete");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Excel Load Failed: {ex.Message}");
            return false;
        }
    }

    public bool ExportLoggedDataToCsv(Dictionary<string, Datas> dataDictionary, string fileName = null)
    {
        try
        {
            if (dataDictionary == null || dataDictionary.Count == 0)
            {
                Debug.LogWarning("[DocumentController] Export할 데이터가 없습니다.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"Log_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            }
            else
            {
                fileName = $"{fileName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            }

                string folderPath = Path.Combine(Application.streamingAssetsPath, "Logs");

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);

            // 시간 기준 데이터 정리
            Dictionary<string, Dictionary<string, string>> table
                = new Dictionary<string, Dictionary<string, string>>();

            foreach (var item in dataDictionary)
            {
                string tag = item.Key;
                Datas data = item.Value;

                foreach (string log in data.LoggedData)
                {
                    string[] split = log.Split(',');

                    if (split.Length < 2)
                        continue;

                    string time = split[0];
                    string value = split[1];

                    if (!table.ContainsKey(time))
                    {
                        table[time] = new Dictionary<string, string>();
                    }

                    table[time][tag] = value;
                }
            }

            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                // Header 작성
                List<string> headers = new List<string>
            {
                "Time"
            };

                headers.AddRange(dataDictionary.Keys);

                writer.WriteLine(string.Join(",", headers));

                // Time 순서대로 작성
                foreach (var row in table.OrderBy(x => x.Key))
                {
                    List<string> values = new List<string>
                {
                    row.Key
                };

                    foreach (var tag in dataDictionary.Keys)
                    {
                        if (row.Value.TryGetValue(tag, out string value))
                        {
                            values.Add(value);
                        }
                        else
                        {
                            values.Add("");
                        }
                    }

                    writer.WriteLine(string.Join(",", values));
                }
            }

            Debug.Log($"[DocumentController] CSV Export 완료: {filePath}");

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DocumentController] CSV Export 실패: {ex.Message}");
            return false;
        }
    }

    public Dictionary<string, T> LoadTable<T>(DataTable table, string keyColumnName) where T : new()
    {
        var dict = new Dictionary<string, T>();

        if (table == null)
        {
            Debug.LogError("[DocumentController] Table이 null입니다.");
            return dict;
        }

        var headers = new List<string>();

        for (int col = 0; col < table.Columns.Count; col++)
        {
            headers.Add(table.Rows[0][col].ToString());
        }

        for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
        {
            var row = table.Rows[rowIndex];

            T obj = new T();
            string key = null;

            for (int col = 0; col < headers.Count; col++)
            {
                string header = headers[col];
                var prop = typeof(T).GetProperty(header);

                if (prop == null)
                    continue;

                object value = row[col];

                if (value == null || string.IsNullOrEmpty(value.ToString()))
                    continue;

                object convertedValue = ConvertValue(prop.PropertyType, value);

                prop.SetValue(obj, convertedValue);

                if (header == keyColumnName)
                {
                    key = value.ToString();
                }
            }

            if (!string.IsNullOrEmpty(key))
            {
                dict[key] = obj;
            }
        }

        return dict;
    }

    private object ConvertValue(Type targetType, object value)
    {
        if (value == null)
            return null;

        string stringValue = value.ToString().Trim();

        if (targetType.IsEnum)
            return Enum.Parse(targetType, stringValue);

        if (targetType == typeof(int))
        {
            if (int.TryParse(stringValue, out int intResult))
                return intResult;

            if (double.TryParse(stringValue, out double doubleResult))
                return (int)doubleResult;

            Debug.LogError($"Int 변환 실패: {stringValue}");
            return 0;
        }

        if (targetType == typeof(float))
        {
            if (float.TryParse(stringValue, out float floatResult))
                return floatResult;

            if (double.TryParse(stringValue, out double doubleResult))
                return (float)doubleResult;

            Debug.LogError($"Float 변환 실패: {stringValue}");
            return 0f;
        }

        if (targetType == typeof(bool))
        {
            if (bool.TryParse(stringValue, out bool boolResult))
                return boolResult;

            if (int.TryParse(stringValue, out int intBool))
                return intBool != 0;

            return false;
        }

        if (targetType == typeof(string))
            return stringValue;

        return Convert.ChangeType(value, targetType);
    }

    public bool SaveSchedulesToExsh( List<ExperimentWrapper> schedules, string fileName = null )
    {
        try
        {
            if (schedules == null)
            {
                Debug.LogWarning("[DocumentController] 저장할 Schedule이 없습니다.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = $"SavedSchedule.exsh";
            }

            if (!fileName.EndsWith(".exsh"))
            {
                fileName += ".exsh";
            }

            string folderPath = Path.Combine(
                Application.streamingAssetsPath,
                "Schedules"
            );

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);

            string json = JsonConvert.SerializeObject(
                schedules,
                Formatting.Indented
            );

            File.WriteAllText(filePath, json);

            Debug.Log($"[DocumentController] Schedule 저장 완료 : {filePath}");

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DocumentController] Schedule 저장 실패 : {ex.Message}");
            return false;
        }
    }
    public List<ExperimentWrapper> LoadSchedulesFromExsh(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Debug.LogError($"[DocumentController] 파일 없음 : {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);

            return JsonConvert.DeserializeObject<
                List<ExperimentWrapper>
            >(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DocumentController] Schedule Load 실패 : {ex.Message}");
            return null;
        }
    }
}