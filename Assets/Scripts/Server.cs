using UnityEngine.Networking;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    private const int USERNUMBER = 1;
    public int PORT;
    private const int WEB_PORT = 26001;
    private const int BYTE_SIZE = 1024;
    private byte reliableChannel;
    private int hostId;
    private int webHostId;
    private bool isStarted;
    private byte error;
    public NetworkDiscovery networkDiscovery;

    //=========
    public Text text,text1,text2;
    //=========

    #region MonoBehaviour
    private void Start(){
        DontDestroyOnLoad(gameObject);
        Init();
    }
    private void Update(){
        UpdateMessagePump();
    }
    #endregion MonoBehaviour
    public void Init(){
        NetworkTransport.Init();

        ConnectionConfig cc = new ConnectionConfig();
        reliableChannel = cc.AddChannel(QosType.Reliable);

        HostTopology topo = new HostTopology(cc,USERNUMBER);

        //Server only code
        hostId = NetworkTransport.AddHost(topo,PORT,null);
        webHostId = NetworkTransport.AddWebsocketHost(topo,WEB_PORT,null);

        Debug.Log(string.Format("Opening connection on port {0} and webport {1}",PORT,WEB_PORT));
        text.text = string.Format("Opening connection on port {0} and webport {1}",PORT,WEB_PORT);

        isStarted = true;
        networkDiscovery.isServer = true;
        networkDiscovery.isClient = false;
        networkDiscovery.Initialize();
    }
    public void Shutdown(){
        isStarted = false;
        NetworkTransport.Shutdown();
    }
    public void UpdateMessagePump(){
        if(!isStarted){
            return;
        }
        int recHostId; // Is this from Web? Or standalone
        int connectionId;//Which user is sending me this?
        int channelId;//Which lane is user sending that message from

        byte[] recBuffer = new byte[BYTE_SIZE];
        int dataSize;

        NetworkEventType type = NetworkTransport.Receive(out recHostId,out connectionId, out channelId,recBuffer,BYTE_SIZE,out dataSize,out error);
        switch(type){
            case NetworkEventType.Nothing:
                break;
            
            case  NetworkEventType.ConnectEvent:
                Debug.Log(string.Format("User {0} has connected!",connectionId));
                text1.text = string.Format("User {0} has connected!",connectionId);
                break;

            case  NetworkEventType.DisconnectEvent:
                Debug.Log(string.Format("User {0} has disconnected!",connectionId));
                break;
            
            case NetworkEventType.DataEvent:
                BinaryFormatter formatter = new BinaryFormatter();
                MemoryStream ms = new MemoryStream(recBuffer);
                NetMsg msg = (NetMsg)formatter.Deserialize(ms);

                OnData(connectionId,channelId,recHostId,msg);
                break;

            default:
            case NetworkEventType.BroadcastEvent:
                Debug.Log("Unexpected network event type");
                break;

        }

    }
#region OnData
    private void OnData(int connectionId, int channelId,int recHostId,NetMsg msg){
        switch(msg.OperationCode){
            case NetOP.None:
                Debug.Log("Unexpected Net Operation code");
                break;
            case NetOP.FLAG:
                CreateMessage(connectionId,channelId,recHostId,(NetCreateMessage)msg);
                break;
        }
    }   

    private void CreateMessage(int connectionId,int channelId,int recHostId,NetCreateMessage ncm){
        Debug.Log(string.Format("{0}",ncm.information));
        text2.text = string.Format("{0}",ncm.information);
    }
#endregion

   #region Send
    public void SendClient(int recHost,int cnnId,NetMsg msg){
        //This is where we hold our data
        byte[] buffer = new byte[BYTE_SIZE];

        //This is where you would crush your data into a byte[]
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream(buffer);
        formatter.Serialize(ms,msg);
        
        if(recHost == 0){
            NetworkTransport.Send(hostId,cnnId,reliableChannel,buffer,BYTE_SIZE,out error);
        }else{
            NetworkTransport.Send(webHostId,cnnId,reliableChannel,buffer,BYTE_SIZE,out error);
        }
    }
    #endregion
}