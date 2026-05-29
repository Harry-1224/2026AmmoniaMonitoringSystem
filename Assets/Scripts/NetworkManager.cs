using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ENetworkState
{
    Disconnected,
    Connecting,
    Connected,
    ConnectFail,
}

public enum EDataAddressRange
{
    SlaveID = 0, 
    AddressStart = 0,
    AddressEnd = 50,
    OutputAddressStart = 30,
    OutputAddressEnd = 50,
    AnalogInputAddressStart = 0,
    AnalogInputAddressEnd = 39,
    AnalogOutputAddressStart = 100,
    AnalogOutputAddressEnd = 139,
    DigitalInputAddressStart = 40,
    DigitalInputAddressEnd = 50,
    DigitalOutputAddressStart = 140,
    DigitalOutputAddressEnd = 150,
}
public class WriteRequest
{
    public string Type;
    public ushort Address;
    public ushort Data;
}

public class NetworkManager : ManagerBase
{
    // NetworkManager
    //  - 싱글톤 패턴으로 구현하여 어디서든 접근 가능하도록 함
    //  - 초기화 시 네트워크 전용 스레드 생성하여 네트워크 연결 및 데이터 수신 처리
    //  - 네트워크 연결 시 OnNetworkConnected 이벤트 발생
    //  - 네트워크 연결 실패 시 OnNetworkConnectionFailed 이벤트 발생
    //  - 네트워크 연결이 끊어졌을 때 OnNetworkDisconnected 이벤트 발생
    //  - 네트워크로부터 데이터를 수신했을 때 OnDataReceived 이벤트 발생

    // 초기화
    protected override void Intialize()
    {
        base.Intialize();
        modbusService = new ModbusService(SlaveID, IpAddress, Port);
        //StartNetworkLoop();
    }

    protected override void Update()
    {

        lock (NetworkEventActions)
        {
            while (NetworkEventActions.Count > 0)
            {
                NetworkEventActions.Dequeue()?.Invoke();
            }
        }
    }

    // 프로그램 종료 시 네트워크 루프 종료
    private void OnApplicationQuit()
    {
        StopNetwork();
    }

    protected override void EventSubscriber()
    {
        Manager.Network.OnTryNetworkConnect += HandleTryNetworkConnect;
        Manager.Network.OnNetworkConnected += HandleNetworkConnected;
        Manager.Network.OnNetworkDisconnected += HandleNetworkDisconnected;
        Manager.Network.OnNetworkConnectionFailed += HandleNetworkConnectionFailed;
        Manager.Network.OnNetworkError += HandleNetworkError;
    }
    protected override void EventUnsubscriber()
    {
        base.EventUnsubscriber();

        Manager.Network.OnTryNetworkConnect -= HandleTryNetworkConnect;
        Manager.Network.OnNetworkConnected -= HandleNetworkConnected;
        Manager.Network.OnNetworkDisconnected -= HandleNetworkDisconnected;
        Manager.Network.OnNetworkConnectionFailed -= HandleNetworkConnectionFailed;
        Manager.Network.OnNetworkError -= HandleNetworkError;
    }

    #region Singleton
    public static NetworkManager Instance { get; private set; }
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

    #region Networking System

    public event Action OnNetworkConnected;
    public event Action OnNetworkDisconnected;
    public event Action OnNetworkConnectionFailed;
    public event Action<string> OnTryNetworkConnect;
    public event Action<string> OnNetworkError;
    // TODO : 데이터 수신 예제 - 이벤트 정리할것.
    public event Action OnDataCall;
    public event Action OnDataSet;
    public event Action<ushort[]> OnDataReceived;

    private readonly Queue<Action> NetworkEventActions = new Queue<Action>();


    private ModbusService modbusService;

    private byte SlaveID  = 0;
    private string IpAddress = "192.168.1.2";
    private int Port = 502;
    public ENetworkState NetworkState { get; private set; } = ENetworkState.Disconnected;
    public bool isConnected { get; private set; } = false;

    private Task networkTask;
    private CancellationTokenSource networkCTS;

    private ConcurrentQueue<WriteRequest> writeQueue = new();
    private ushort[] GetDatas = new ushort[(int)EDataAddressRange.AddressEnd];


    public void SetParameters(byte slaveID, string ipAddress, int port)
    {
        SlaveID = slaveID;
        IpAddress = ipAddress;
        Port = port;
    }

    public bool StartNetworkLoop()
    {
        if(NetworkState != ENetworkState.Connecting) NetworkState = ENetworkState.Connecting;

        if (networkCTS != null) return false;

        networkCTS = new CancellationTokenSource();

        networkTask = Task.Run(() => NetworkLoop(networkCTS.Token));

        return true;
    }
    public async void StopNetwork() 
    {
        // TODO : 네트워크 연결 종료 로직 구현
        if (networkCTS == null)
            return;

        networkCTS.Cancel();

        try
        {
            await networkTask;
        }
        catch 
        { 

        }

        networkCTS.Dispose();
        networkCTS = null;

        ClearAllEvents();
    }
    public void ReserveDateWriteing(string type, ushort address, ushort data)
    {
        writeQueue.Enqueue(new WriteRequest
        {
            Type = type,
            Address = address,
            Data = data
        });
    }

    /*
    // NOTE : 네트워크 상태 머신 LOOP
    private async Task NetworkLoop(CancellationToken token)
    {
        int Failed = 0;

        while (!token.IsCancellationRequested)
        {
            switch (NetworkState)
            {
                case ENetworkState.Connected:
                    // TODO : 연결 성공 시 CONNETED FAIL COUNT 초기화, 데이터 수신 루프 시작
                    Failed = 0;
                    try
                    {
                        NetworkAction();
                    }
                    catch(Exception ex)
                    {
                        isConnected = false;
                        NetworkState = ENetworkState.Disconnected;
                    }

                    break;
                case ENetworkState.Connecting:
                    // TODO : 네트워크 연결 시도 로직 구현, 타임아웃 및 예외 처리 포함
                    bool success = TryConnect();
                    if (success)
                    {
                        NetworkState = ENetworkState.Connected;
                        isConnected = true;
                        OnNetworkConnected?.Invoke();
                    }
                    else
                    {
                        OnNetworkConnectionFailed?.Invoke();
                        NetworkState = ENetworkState.Disconnected;
                        await Task.Delay(50, token);
                    }
                    break;
                case ENetworkState.Disconnected:
                    // TODO : 3번 이상 실패 시 Setting 상태로 전환하여 사용자에게 설정 변경 유도

                    if(Failed >= 3) NetworkState = ENetworkState.ConnectFail;
                    else NetworkState = ENetworkState.Connecting;

                    Failed++;

                    await Task.Delay(50, token);
                    break;
                case ENetworkState.ConnectFail:
                    // TODO : 외부에서 네트워크 연결 재시도 까지 대기하는 상태
                    Failed = 0;
                    await Task.Delay(50, token);
                    break;
            }
            await Task.Delay(10, token);
        }
    }
    */

    private async Task NetworkLoop(CancellationToken token)
    {
        int Failed = 0;

        while (!token.IsCancellationRequested)
        {
            switch (NetworkState)
            {
                case ENetworkState.Connected:

                    Failed = 0;

                    try
                    {
                        NetworkAction();
                    }
                    catch (Exception ex)
                    {
                        isConnected = false;

                        NetworkState = ENetworkState.Disconnected;

                        OnNetworkDisconnected?.Invoke();

                        OnNetworkError?.Invoke(
                            $"NetworkAction Error : {ex.Message}"
                        );
                    }

                    break;

                case ENetworkState.Connecting:

                    try
                    {

                        bool success = TryConnect();

                        if (success)
                        {
                            NetworkState = ENetworkState.Connected;

                            isConnected = true;

                            OnNetworkConnected?.Invoke();
                        }
                        else
                        {
                            OnNetworkConnectionFailed?.Invoke();

                            OnNetworkError?.Invoke(
                                $"Connection Failed ({Failed + 1}/3)"
                            );

                            NetworkState = ENetworkState.Disconnected;

                            await Task.Delay(50, token);
                        }
                    }
                    catch (Exception ex)
                    {
                        NetworkState = ENetworkState.Disconnected;

                        OnNetworkError?.Invoke(
                            $"TryConnect Exception : {ex.Message}"
                        );
                    }

                    break;

                case ENetworkState.Disconnected:

                    if (Failed >= 3)
                    {
                        NetworkState = ENetworkState.ConnectFail;

                        OnNetworkError?.Invoke(
                            "Network Connect Failed. Enter ConnectFail State."
                        );
                    }
                    else
                    {
                        NetworkState = ENetworkState.Connecting;
                    }

                    Failed++;

                    await Task.Delay(50, token);

                    break;

                case ENetworkState.ConnectFail:

                    Failed = 0;

                    await Task.Delay(50, token);

                    break;
            }

            await Task.Delay(10, token);
        }
    }
    // NOTE : LOOP 내부에서 작동 - 연결 시도
    private bool TryConnect()
    {
        // TODO : TCP 연결 시도 로직 구현, 타임아웃 및 예외 처리 포함
        OnTryNetworkConnect?.Invoke($"Trying to connect to {IpAddress}:{Port} with SlaveID {SlaveID}");
        (bool result , string msg) = modbusService.ConnectNetwork();
        if (!result)
        {
            OnNetworkError?.Invoke(msg);
        }
        return result;
    }

    // NOTE : LOOP 내부에서 작동 - Connect시 작동
    private bool NetworkAction()
    {
        try
        {
    // 1. PLC에 데이터 작성
            ushort[] setData = ProcessWriteQueue();
    // 2. PLC에서 데이터 습득, 작성한 데이터가 잘 들어갔는지 파악
            ProcessReadData(setData);

        }
        catch (Exception ex) 
        {
            throw ex;
        }

        return true;
    }

    // NOTE : PLC에서 데이터 읽어오기
    private ushort[] ProcessReadData(ushort[] CompaereData = null)
    {
    // 1. 데이터 수신 시도이벤트 발생
        OnDataCall?.Invoke();
        try
        {
    // 2. 데이터 요청
            GetDatas = modbusService.CallData((byte)EDataAddressRange.SlaveID, (ushort)EDataAddressRange.AddressStart, (ushort)EDataAddressRange.AddressEnd);

            // * 매개변수가 null이 아닐경우 데이터 비교
        }
        catch (Exception ex)
        {
            throw ex;
        }

        if (GetDatas == null) throw new Exception("[NetworkManager] Non-Data");

    //3. 데이터 수신 이벤트 발생
        OnDataReceived?.Invoke(GetDatas);
        return GetDatas;
    }

    // NOTE : PLC에서 데이터 저장하기
    private ushort[] ProcessWriteQueue()
    {

    // 1. 데이터 송신 이벤트 발생 및 함수에 필요한 연산 준비
        if (writeQueue == null)
        {
            writeQueue = new ConcurrentQueue<WriteRequest>();
            return null;
        }
        if (writeQueue.IsEmpty) return null;

        OnDataSet?.Invoke();

        ushort[] data = GetDatas;

    // 2. Queue에 저장된 모든 요청 정리
        while (writeQueue.TryDequeue(out var req))
        {
    // 3. Queue에 들어간 데이터가 Digital일 경우 Address 조정 후 해당 자리 Bit값으로 데이터 Update
            if (req.Type == "DO")
            {
                int wordIndex = req.Address / 10;
                int bitIndex = req.Address % 10;

                if (wordIndex >= data.Length) continue;

                // float → int 변환
               int value = data[wordIndex];

                // 비트 ON/OFF
                if (req.Data > 0) // ON
                {
                    value |= (1 << bitIndex);
                }
                else // OFF
                {
                    value &= ~(1 << bitIndex);
                }

                // 다시 float로 저장
                data[wordIndex] = (ushort)value;
            }
    // 4. Queue에 들어간 데이터가 Digital가 아닐 경우 Address로 배열 자리를 찾아가 데이터 Update
            else
            {
                // 아날로그 / 일반 값
                if (req.Address < data.Length)
                    data[req.Address] = req.Data;
            }
        }
    // 5. 필요한 데이터 만큼만 잘라내어 Data Write
        modbusService.SendData((byte)EDataAddressRange.SlaveID, (ushort)EDataAddressRange.OutputAddressStart, data.Skip((int)EDataAddressRange.OutputAddressStart).Select(x => (ushort)x).ToArray());

        return data;
    }
    private void HandleTryNetworkConnect(string msg)
    {
        EnqueueMainThreadAction(() =>
        {
            Debug.Log($"[Network] Try Connect : {msg}");
        });
    }

    private void HandleNetworkConnected()
    {
        EnqueueMainThreadAction(() =>
        {
            Debug.Log("[Network] Connected");
        });
    }

    private void HandleNetworkDisconnected()
    {
        EnqueueMainThreadAction(() =>
        {
            Debug.LogWarning("[Network] Disconnected");
        });
    }

    private void HandleNetworkConnectionFailed()
    {
        EnqueueMainThreadAction(() =>
        {
            Debug.LogWarning("[Network] Connection Failed");
        });
    }

    private void HandleNetworkError(string msg)
    {
        EnqueueMainThreadAction(() =>
        {
            Debug.LogError(msg);
        });
    }

    private void EnqueueMainThreadAction(Action action)
    {
        lock (NetworkEventActions)
        {
            NetworkEventActions.Enqueue(action);
        }
    }


    /// <summary>
    /// 네트워크 관련 이벤트를 모두 초기화 하는 함수
    /// </summary>
    private void ClearAllEvents()
    {
        OnNetworkConnected = null;
        OnNetworkDisconnected = null;
        OnNetworkConnectionFailed = null;
        OnDataCall = null;
        OnDataSet = null;
    }


    #endregion

}
