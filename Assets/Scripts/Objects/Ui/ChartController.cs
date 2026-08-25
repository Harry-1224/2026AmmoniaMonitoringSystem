using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using XCharts.Runtime;

public class ChartController : UiObjectBase
{
    [SerializeField] RectTransform ButtonParent;
    [SerializeField] GameObject ButtonPrefab;
    [SerializeField] private string[] Tags = 
        { "AT101" , "AT102", "AT103", "TT101", "TT102"};

    [Header("Chart Settings")]
    [SerializeField] LineChart lineChart;
    private XAxis xAxis => lineChart.GetChartComponent<XAxis>(0);
    [SerializeField] private const int MaxPoints = 100;
    [SerializeField] private const int MaxXAxisLabels = 10;

    private HashSet<string> chartTags;
    private readonly Dictionary<string, Serie> seriesDictionary = new();

    protected override void Start()
    {
        
    }

    private GameObject TestOBJ;
    protected override void OnEnable()
    {
        base.OnEnable();

        EventSubscriber();
        LoadChartHistory();
    }

    protected override void Initialize()
    {
        chartTags = new HashSet<string>(Tags);
        lineChart.RemoveAllSerie();

        foreach (var tag in chartTags)
        {
            var serie = lineChart.AddSerie<Line>(tag);

            seriesDictionary[tag] = serie;

            var button = Instantiate(ButtonPrefab, ButtonParent);
            var buttonComp = button.GetComponent<GraphLegendButton>();

            buttonComp.SetButtonTag(tag);
            buttonComp.OnClickButton += OnLegendButtonClicked;
        }

        var legend = lineChart.GetChartComponent<Legend>(0);

        if (legend == null)
            legend = lineChart.AddChartComponent<Legend>();

        legend.show = true;
        legend.data = Tags.ToList();

        base.Initialize();
    }

    private void OnLegendButtonClicked(string tag, bool isActive)
    {
        if (!seriesDictionary.TryGetValue(tag, out var serie))
            return;

        serie.show = isActive;
    }

    private void UpdateChart(Dictionary<string, Datas> PLCData)
    {
        var time = DateTime.Now.ToString("HH:mm:ss.fff");
        AddXLabel(time);

        foreach (var tag in chartTags)
        {
            if (!seriesDictionary.TryGetValue(tag, out var serie))
                continue;

            if (!PLCData.TryGetValue(tag, out var data))
                continue;

            AddYValue(serie, data.Value);
        }
    }

    private void AddXLabel(string label)
    {
        lineChart.AddXAxisData(label);

        if (xAxis.data.Count > MaxPoints)
        {
            xAxis.RemoveData(0);
        }
    }

    private void AddYValue(Serie serie, float value) // Data
    {
        lineChart.AddData(serie.index, value);
        if (serie.dataCount > MaxPoints)
        {
            serie.RemoveData(0);
        }
    }

    private bool TryParseLog(string log, out DateTime time, out float value)
    {
        time = default;
        value = default;

        var split = log.Split(',');

        if (split.Length != 2)
            return false;

        if (!DateTime.TryParseExact(
                split[0],
                "yyyy-MM-dd HH:mm:ss.fff",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out time))
        {
            return false;
        }

        return float.TryParse(
            split[1],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private void LoadChartHistory()
    {
        lineChart.ClearData();

        var firstTag = chartTags.FirstOrDefault();

        if (string.IsNullOrEmpty(firstTag))
            return;

        var firstData = Manager.Data.CallData<Datas>(firstTag);

        if (firstData?.LoggedData == null || firstData.LoggedData.Count == 0)
            return;

        int startIndex = Mathf.Max(
            0,
            firstData.LoggedData.Count - MaxPoints);

        for (int i = startIndex; i < firstData.LoggedData.Count; i++)
        {
            if (!TryParseLog(
                firstData.LoggedData[i],
                out var time,
                out _))
            {
                continue;
            }

            AddXLabel(time.ToString("HH:mm:ss.fff"));

            foreach (var tag in Tags)
            {
                if (!seriesDictionary.TryGetValue(tag, out var serie))
                    continue;

                var data = Manager.Data.CallData<Datas>(tag);

                if (data == null || i >= data.LoggedData.Count)
                    continue;

                if (TryParseLog(
                    data.LoggedData[i],
                    out _,
                    out var value))
                {
                    AddYValue(serie, value);
                }
            }
        }
    }

    private void LoadTestHistory()
    {
        lineChart.ClearData();

        foreach (var tag in chartTags)
        {
            if (!seriesDictionary.TryGetValue(tag, out var serie))
                continue;

            var data = testDataSource.GetData(tag);

            if (data == null)
                continue;

            int startIndex = Mathf.Max(
                0,
                data.LoggedData.Count - MaxPoints);

            for (int i = startIndex; i < data.LoggedData.Count; i++)
            {
                if (!TryParseLog(
                        data.LoggedData[i],
                        out var time,
                        out var value))
                {
                    continue;
                }

                if (tag == Tags[0])
                    AddXLabel(time.ToString("HH:mm:ss.fff"));

                AddYValue(serie, value);
            }
        }
    }

    protected override void EventSubscriber()
    {
        base.EventSubscriber();

        Manager.Data.OnDataChanged += UpdateChart;
    }

    protected override void EventUnsubscriber()
    {
        base.EventUnsubscriber();

        Manager.Data.OnDataChanged -= UpdateChart;
    }
}
