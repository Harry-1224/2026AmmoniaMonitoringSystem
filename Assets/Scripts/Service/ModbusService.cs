using NModbus;
using System;
using System.Net;
using System.Net.Sockets;
using static UnityEngine.Rendering.DebugUI;

public class ModbusService : INetworkSevice
{
    public ModbusService(byte slaveID, string ipAddress, int port) 
    {
        SetSlaveID(slaveID);
        SetIpAddress(ipAddress);
        SetPortNumber(port);
    }  

    public byte slaveID { get; private set; } = 0; // 슬레이브 ID
    public string IpAdress { get; private set; }
    public int portNumber { get; private set; } = 502;

    //Tcp network연결
    public TcpClient client = null;

    //Modbus 프로토콜의 변수
    private ModbusFactory factory = null;
    private IModbusMaster master = null;


    public void SetSlaveID(byte setSlaveID) => slaveID = setSlaveID;
    public void SetSlaveID(int setSlaveID) => slaveID = (byte)setSlaveID;
    public void SetIpAddress(string setIpAddress) => IpAdress = setIpAddress;
    public void SetIpAddress(string firstAddress, string secondAddress, string thredAddress, string fourthAddress) => IpAdress = $"{firstAddress}.{secondAddress}.{thredAddress}.{fourthAddress}";
    public void SetIpAddress(int firstAddress, int secondAddress, int thredAddress, int fourthAddress) => IpAdress = $"{firstAddress}.{secondAddress}.{thredAddress}.{fourthAddress}";
    public void SetPortNumber(int setPortNumber) => portNumber = setPortNumber;

    public (bool success, string message) ConnectNetwork()
    {
        if (IpAdress == null || portNumber == 0)
        {
            return(false, "[ModbusService.ConnectNetwork]IpAddress or Port Number is null ");
        }

        try
        {
            client = new TcpClient(IpAdress, portNumber);
            CreateMaster();
        }
        catch (SocketException ex)
        {
            client = null;
            return (false, $"[ModbusService] : {ex.Message} / Slave ID: {slaveID}, IP: {IpAdress}, Port: {portNumber}");
        }

        return (true, "[ModbusService] : Network Connecting Success");
    }

    public (bool success, string message) DisconnectNetwork()
    {
        try
        {
            master?.Dispose();
            master = null;

            client?.Close();
            client = null;

            return (true, "[ModbusService] : Network Disconnecting Success");
        }
        catch (Exception ex)
        {
            return (false, "[ModbusService] : " + ex.Message);
        }
    }
    public ushort[] CallData(byte slaveId, ushort address, ushort numInputs)
    {

        ushort[] data;
        if (master == null) throw new Exception("[CallData] Modbus Master is null.");
        try
        {
            data = master.ReadHoldingRegisters(slaveId, address, numInputs);
        }
        catch (Exception ex)
        {
            throw ex;
        }

        return data;
    }
    public void SendData(byte slaveId, ushort startAddress, ushort[] setData)
    {

        if (master == null) throw new Exception("[SendData] Modbus Master is null.");
        try
        {
            master.WriteMultipleRegisters(slaveId, startAddress, setData);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    private void CreateMaster()
    {
        if (client == null)
        {
            throw new Exception("[CreateMaster] Client is null.");
        }
        try
        {
            factory = new ModbusFactory();
            master = factory.CreateMaster(client);
        }
        catch (Exception ex)
        {
            //Modbus 통신에 대한 예외 이벤트 생성
            master = null;

            throw ex;
        }
    }
}
