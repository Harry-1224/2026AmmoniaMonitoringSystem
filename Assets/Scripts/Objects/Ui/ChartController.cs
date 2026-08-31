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
    [SerializeField]
    private List<string> Tags = new() { "AT101", "AT102", "AT103", "TT101", "TT102" };
    [SerializeField] private int ButtonsPerPage = 9;
    [SerializeField] private GameObject PreviousButton;
    [SerializeField] private GameObject NextButton;

    private readonly List<GameObject> legendButtons = new();
    private int currentPage = 0;

    private int TotalPageCount =>
    Mathf.CeilToInt((float)legendButtons.Count / ButtonsPerPage);

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

    protected override void OnEnable()
    {
        base.OnEnable();

        EventSubscriber();
        LoadChartHistory();
    }

    protected override void Initialize()
    {
        legendButtons.Clear();
        foreach (var obj in legendButtons)
        {
            Destroy(obj);
        }
        
        Tags.Clear();
        foreach (var tag in Manager.Data.InstrumentInfos)
        {
            Tags.Add(tag.Key);
        }

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

            legendButtons.Add(button);
        }

        var legend = lineChart.GetChartComponent<Legend>(0);

        if (legend == null)
            legend = lineChart.AddChartComponent<Legend>();

        legend.show = false;
        legend.data = Tags.ToList();

        UpdateButtonPage();

        base.Initialize();
    }

    private void UpdateButtonPage()
    {
        int startIndex = currentPage * ButtonsPerPage;
        int endIndex = Mathf.Min(
            startIndex + ButtonsPerPage,
            legendButtons.Count
        );

        for (int i = 0; i < legendButtons.Count; i++)
        {
            bool isVisible = i >= startIndex && i < endIndex;
            legendButtons[i].SetActive(isVisible);
        }

        PreviousButton.SetActive(currentPage > 0);
        NextButton.SetActive(currentPage < TotalPageCount - 1);
    }

    // 체크박스 버튼 클릭
    private void OnLegendButtonClicked(string tag, bool isActive)
    {
        if (!seriesDictionary.TryGetValue(tag, out var serie))
            return;

        serie.show = isActive;
    }

    // History 로드 후에는 수신한 시간 및 최근 값만 추가
    private void UpdateChart(DateTime time, Dictionary<string, float> loggedData)
    {
        AddXLabel(time.ToString());

        foreach (var tag in chartTags)
        {
            if (!seriesDictionary.TryGetValue(tag, out var serie))
                continue;

            if (!loggedData.TryGetValue(tag, out var data))
                continue;

            AddYValue(serie, data);
        }
    }

    private void AddXLabel(string label) // Time
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

        // 시간은 한 번만 로드
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

                // 데이터 매니저에서 로깅된 데이터 로드
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

    public void OnPreviousPage()
    {
        if (currentPage <= 0)
            return;

        currentPage--;
        UpdateButtonPage();
    }

    public void OnNextPage()
    {
        if (currentPage >= TotalPageCount - 1)
            return;

        currentPage++;
        UpdateButtonPage();
    }

    protected override void EventSubscriber()
    {
        base.EventSubscriber();

        Manager.Data.OnDataLogged += UpdateChart;
    }

    protected override void EventUnsubscriber()
    {
        base.EventUnsubscriber();

        Manager.Data.OnDataLogged -= UpdateChart;
    }
}
