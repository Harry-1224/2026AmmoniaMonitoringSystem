using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Analytics;
using static UnityEditor.Progress;

public class Datas
{
    public string Name;
    public float Value;
    public string Group;
    public List<string> LoggedData = new List<string>();
}

public class DataManager : ManagerBase
{
    // DataManager
    //  - 싱글톤 패턴으로 구현하여 어디서든 접근 가능하도록 함
    //  - 데이터 관리 시스템 구현, NetworkManager로부터 데이터를 수신하여 저장 및 관리

    public string SavePath = "";

    public bool isDataLogged = false;

    private Dictionary<string , ExperimentWrapper> experimentDefine = new Dictionary<string, ExperimentWrapper>();
    private List<ExperimentInfo> experimentInfos = new List<ExperimentInfo>();
    public Dictionary<string, InstrumentInfo> InstrumentInfos = new Dictionary<string, InstrumentInfo>();
    public Dictionary<string, Datas> DataDictionary = new Dictionary<string, Datas>();
    private Dictionary<string, Datas> dataBuffer = new();

    private DocumentController documentController = new DocumentController();

    protected override void Update()
    {
        while (true)
        {
            Dictionary<string, Datas> changedData = null;

            lock (dataChangedLock)
            {
                if (recivedDataQueue.Count == 0)
                    break;

                changedData = recivedDataQueue.Dequeue();
            }

            OnDataChanged?.Invoke(changedData);
        }
    }
    
    protected override void Intialize()
    {
        base.Intialize();

        //Excel 파일에서 데이터 로드
        if (!documentController.LoadDocument())
            return;

        InstrumentInfos = documentController.InstrumentInfos;
        experimentInfos = documentController.ExperimentInfos;
        experimentDefine = documentController.ExperimentDefines;

        InitializeDataDictionary();
    }

    protected override void EventSubscriber()
    {
        Manager.Network.OnDataReceived += OnDataReceived;
        Manager.Logging.OnLoggingTimingActed += LoggingTimingActedHandler;
        Manager.Experiment.ExperimentStateChange += ExperimentStateChangeHandler;
    }
    protected override void EventUnsubscriber()
    {
        Manager.Network.OnDataReceived -= OnDataReceived;
        Manager.Logging.OnLoggingTimingActed -= LoggingTimingActedHandler;
    }

    public override void SetSceneControlManager(SceneControlManager sceneManager)
    {
        base.SetSceneControlManager(sceneManager);
    }
    public bool ExportLoggedData(string Name = null)
    {
        if(Name == null)    return documentController.ExportLoggedDataToCsv(DataDictionary);
        else return documentController.ExportLoggedDataToCsv(DataDictionary, Name);
    }

    private void InitializeDataDictionary()
    {
        DataDictionary.Clear();

        foreach (var item in InstrumentInfos)
        {
            string key = item.Key;
            InstrumentInfo info = item.Value;

            DataDictionary[key] = new Datas
            {
                Name = key,
                Value = 0
            };
        }

        Debug.Log($"[DataManager] DataDictionary 초기화 완료 : {DataDictionary.Count}");
    }

    public void ClearLoggedData()
    {
        isDataLogged = false;

        foreach (var item in DataDictionary)
        {
            item.Value.LoggedData.Clear();
        }

        Debug.Log("[DataManager] LoggedData 초기화 완료");
    }

    #region Singleton
    public static DataManager Instance { get; private set; }
    protected override void SetSingleton()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }

    #endregion

    #region Monitoring System

    public event Action<Dictionary<string, Datas>> OnDataChanged;
    public event Action<List<(string tag, float value)>> OnChangeData;

    public T CallData<T>(string dataName = null)
    {
        // 데이터 불러오는 로직 구현
        if (typeof(T) == typeof(InstrumentInfo))
        {
            InstrumentInfos.TryGetValue(dataName, out InstrumentInfo data);
            if (data != null)
            {
                return (T)(object)data;
            }
            else
            {
                Debug.LogError($"[CallData] Data not found: {dataName}");
                return default(T);
            }
        }
        else if (typeof(T) == typeof(Dictionary<string, InstrumentInfo>))
        {
            if (string.IsNullOrEmpty(dataName))
            {
                return (T)(object)new Dictionary<string, InstrumentInfo>(InstrumentInfos);
            }

            return (T)(object)InstrumentInfos
                .Where(x => x.Value.Function == dataName)
                .ToDictionary(x => x.Key, x => x.Value);
        }
        else if (typeof(T) == typeof(Dictionary<string, ExperimentWrapper>))
        {
            var result = new Dictionary<string, ExperimentWrapper>();

            //List<ExperimentInfo> endSequence = experimentInfos.Where(x => x.Group == "Type_End").ToList();

            foreach (var kv in experimentDefine)
            {
                //if(kv.Key == "Type_End") continue;

                var schedule = kv.Value;

                result[kv.Key] = new ExperimentWrapper
                {
                    Name = schedule.Name,
                    Group = schedule.Group,
                    Timer = schedule.Timer,
                    Experiments = experimentInfos
                        .Where(x => x.Group == schedule.Group)
                        .ToList()
                };

                //result[kv.Key].Experiments.AddRange(endSequence);
                result[kv.Key].Experiments = result[kv.Key].Experiments
                    .OrderBy(x => x.Process)
                    .ToList();
            }

            return (T)(object)result;
        }
        else if (typeof(T) == typeof(Dictionary<string, Datas>))
        {
            if (dataName == null) return (T)(object)DataDictionary;

            var result = InstrumentInfos.Values
                .Where(x => x.Group == dataName)
                .Where(x => DataDictionary.ContainsKey(x.Tag))
                .ToDictionary(
                    x => x.Tag,
                    x => DataDictionary[x.Tag]);

            return (T)(object)result;
        }

        else if (typeof(T) == typeof(Datas))
        {
            DataDictionary.TryGetValue(dataName, out Datas data);

            if (data != null) return (T)(object)data;
            else
            {
                Debug.LogError($"[CallData] Data not found : {dataName}");
                return default(T);
            }
        }
        else if (typeof(T) == typeof(List<string>))
        {

            DataDictionary.TryGetValue(dataName, out Datas data);
            if (data != null)
            {
                return (T)(object)data.LoggedData;
            }
            else
            {
                Debug.LogError($"[CallData] Data not found: {dataName}");
                return default(T);
            }
        }
        else
        {
            Debug.LogError($"[{dataName}] Unsupported data type");
            return default(T);
        }
    }


    public List<InstrumentInfo> SortInstrumentInfoByAddress()
    {
        // TODO : Instrument Info를 Address순으로 정렬하여 List형태로 return
        return new List<InstrumentInfo>();
    }


    #endregion

    #region ExperimentSystem
    private void ExperimentStateChangeHandler(EExperimentStateMachine state)
    {
        if (state == EExperimentStateMachine.Stopping)
        {
            ExportLoggedData();
            //지금까지 로깅된 데이터 리셋
            ClearLoggedData();
        }
    }
    public bool SaveSchedulesToExsh( List<ExperimentWrapper> schedules, string fileName = null)
    {
        return documentController.SaveSchedulesToExsh(schedules, fileName);
    }

    public List<ExperimentWrapper> LoadSchedulesFromExsh(string filePath)
    {
        return documentController.LoadSchedulesFromExsh(filePath);
    }

    #endregion

    #region LoggingSystem
    private void LoggingTimingActedHandler(DateTime loggingTime)
    {
        isDataLogged = true;

        foreach (var item in DataDictionary)
        {
            Datas data = item.Value;

            string log = $"{loggingTime:yyyy-MM-dd HH:mm:ss.fff},{data.Value}";

            data.LoggedData.Add(log);
        }
        //Debug.Log($"[DataManager] Logging 저장 완료 : {loggingTime:HH:mm:ss.fff}");
    }

    #endregion

    #region NetworkingSystem
    private bool isReceived = false;
    private bool isFirstReceived = true;

    private Queue<Dictionary<string, Datas>> recivedDataQueue = new Queue<Dictionary<string, Datas>>();
    private readonly object dataChangedLock = new();

    private void OnDataReceived(ushort[] _datas)
    {
        if (isReceived) return;

        isReceived = true;

        try
        {
            float value = 0;

            foreach (var info in InstrumentInfos)
            {
                InstrumentInfo _info = info.Value;

                value = ConvertPLCToData(_info, _datas);

                if (!DataDictionary.TryGetValue(_info.Tag, out var data))
                {
                    data = new Datas();
                    data.Name = _info.Tag;
                    DataDictionary[_info.Tag] = data;
                }

                bool isChanged = data.Value != value;

                if (isFirstReceived || isChanged)
                {
                    data.Value = value;

                    if (!dataBuffer.TryGetValue(_info.Tag, out var buffer))
                    {
                        buffer = new Datas();
                        buffer.Name = _info.Tag;
                        dataBuffer[_info.Tag] = buffer;
                    }

                    buffer.Value = value;
                }
            }

            if (dataBuffer.Count > 0)
            {
                var snapshot = dataBuffer.ToDictionary(
                    x => x.Key,
                    x => new Datas
                    {
                        Name = x.Value.Name,
                        Value = x.Value.Value
                    }
                );

                lock (dataChangedLock)
                {
                    recivedDataQueue.Enqueue(snapshot);
                }

                dataBuffer.Clear();
            }

            isFirstReceived = false;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] Error processing received data: {ex.Message}");
        }
        finally
        {
            isReceived = false;
        }
    }

    private float ConvertPLCToData(InstrumentInfo info, ushort[] rawData)
    {
        //1. 유효성 검사
        if (info == null || rawData == null || rawData.Length == 0)
            return 0;

        ushort raw = 0;

        // 2. 타입별 처리
        if (info.PointType == "AI" || info.PointType == "AO")
        {
            // 범위 체크
            if (info.Address < 0 || info.Address >= rawData.Length)
            {
                Debug.LogError($"[ConvertPLCToData] Address Out of Range : {info.Address}");
                return 0;
            }

            raw = rawData[info.Address];

            float plcMin = info.PLCMin;
            float plcMax = info.PLCMax;

            // 3. 0 division 방지
            float plcRange = plcMax - plcMin;
            if (plcRange == 0)
            {
                Debug.LogError($"[ConvertPLCToData] PLC Range is Zero : {info.Tag}");
                return 0;
            }

            float realMin = info.RangeMin;
            float realMax = info.RangeMax;

            // 4. float 캐스팅 강제
            float normalized = ((float)raw - plcMin) / plcRange;

            float value = normalized * (realMax - realMin) + realMin;

            return value;
        }
        else if (info.PointType == "DI" || info.PointType == "DO")
        {
            // 주소 → 워드 / 비트 분리
            int wordIndex = info.Address / 10;
            int bitIndex = info.Address % 10;

            // 범위 체크
            if (wordIndex < 0 || wordIndex >= rawData.Length)
            {
                Debug.LogError($"[ConvertPLCToData] Address Out of Range : {info.Address}");
                return 0;
            }

           raw = rawData[wordIndex];

            // 해당 비트 추출
            int bit = (raw >> bitIndex) & 1;

            return bit == 1 ? 1f : 0f;
        }
        else
        {
            Debug.LogError("[Convert PLC to Data] Type is not confirmed : " + info.Type);
            return 0;
        }
    }


    #endregion
}
