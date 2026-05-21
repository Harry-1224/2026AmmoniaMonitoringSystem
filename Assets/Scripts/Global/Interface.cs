public interface IObject
{
    void OnFunctionCalled(object obj = null);
}
public interface IUiObject
{
    public void OnClick();
    public void OnHoverEnter();
    public void OnHoverExit();
    public void OnPointerDown();
    public void OnPointerUp();
    public void OnDrag();
}
public interface INetworkSevice
{
    public (bool success, string message) ConnectNetwork() ;
    public (bool success, string message) DisconnectNetwork();
    public ushort[] CallData(byte slaveId, ushort address, ushort numInputs);
    public void SendData(byte slaveId, ushort startAddress, ushort[] setData);
}