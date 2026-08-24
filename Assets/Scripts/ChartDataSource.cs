using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class ChartTestDataSource : MonoBehaviour
{
    [SerializeField]
    private string[] Tags =
    {
        "AT101",
        "AT102",
        "AT103",
        "TT101",
        "TT102"
    };

    [Header("History")]
    [SerializeField] private int historyCount = 50;
    [SerializeField] private float historyInterval = 1f;

    [Header("Realtime")]
    [SerializeField] private float updateInterval = 1f;
    [SerializeField] private float noise = 0.5f;

    private readonly Dictionary<string, Datas> dataDictionary = new();

    private float timer;

    public event Action<Dictionary<string, Datas>> OnDataChanged;

    private void Awake()
    {
        CreateHistory();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer < updateInterval)
            return;

        timer = 0f;

        GenerateRealtimeData();
    }

    private void CreateHistory()
    {
        DateTime startTime =
            DateTime.Now.AddSeconds(-historyCount * historyInterval);

        foreach (var tag in Tags)
        {
            var data = new Datas
            {
                Name = tag
            };

            float value = GetInitialValue(tag);

            for (int i = 0; i < historyCount; i++)
            {
                DateTime time =
                    startTime.AddSeconds(i * historyInterval);

                value += UnityEngine.Random.Range(-noise, noise);

                data.Value = value;

                data.LoggedData.Add(
                    $"{time:yyyy-MM-dd HH:mm:ss.fff}," +
                    $"{value.ToString(CultureInfo.InvariantCulture)}"
                );
            }

            dataDictionary[tag] = data;
        }
    }

    private void GenerateRealtimeData()
    {
        var changedData = new Dictionary<string, Datas>();

        DateTime now = DateTime.Now;

        foreach (var tag in Tags)
        {
            if (!dataDictionary.TryGetValue(tag, out var data))
                continue;

            float value =
                data.Value + UnityEngine.Random.Range(-noise, noise);

            data.Value = value;

            data.LoggedData.Add(
                $"{now:yyyy-MM-dd HH:mm:ss.fff}," +
                $"{value.ToString(CultureInfo.InvariantCulture)}"
            );

            changedData[tag] = new Datas
            {
                Name = tag,
                Value = value
            };
        }

        OnDataChanged?.Invoke(changedData);
    }

    private float GetInitialValue(string tag)
    {
        return tag switch
        {
            "AT101" => 20f,
            "AT102" => 40f,
            "AT103" => 60f,
            "TT101" => 30f,
            "TT102" => 50f,
            _ => 0f
        };
    }

    public Datas GetData(string tag)
    {
        dataDictionary.TryGetValue(tag, out var data);
        return data;
    }
}