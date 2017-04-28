using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SwingNetwork : MonoBehaviour
{
    public bool server=true;
    
    private const int SERVER_PORT=12128;
    private const short MESSAGE_SWINGINFO=1231;

    Dictionary<string,SwingInfo> swingInfos=new Dictionary<string,SwingInfo>();
    
    public Dictionary<string,SwingInfo> GetSwings()
    {
        return swingInfos;
    }
    
    public SwingInfo GetSwingInfoObject()
    {
        return messageInfo;
    }
    SwingInfo messageInfo=null;
    
    public void OnSwingStatus(NetworkReader reader)
    {        
        messageInfo.Deserialize(reader);
        print("Got message:"+messageInfo.swingID);
        messageInfo.Touch();
        if(swingInfos.ContainsKey(messageInfo.swingID))
        {
            // swap in the deserialized message
            SwingInfo tmp=swingInfos[messageInfo.swingID];
            swingInfos[messageInfo.swingID]=messageInfo;
            messageInfo=tmp;
        }else
        {
            swingInfos[messageInfo.swingID]=messageInfo;
            messageInfo=new SwingInfo();            
        }
    }


    public class SwingInfo : MessageBase
    {
        public SwingInfo()
        {
            lastTime=Time.time;
        }
        
        private float lastTime;        
        
        public void Touch()
        {
            lastTime=Time.time;
        }
        
        public float Age()
        {
            return Time.time-lastTime;
        }
        
        public string swingID;
        public string riderID;
        public float swingBattery;
        public float headsetBattery;
        public bool inSession;
        public float rideTime;
        public float swingAngle;
        public int connectionState;
    }
    
    
    int connectionID=-1;
    int hostID;
    int channelID;
    bool connected=false;


    NetworkWriter writer;
    NetworkReader reader;
    byte[]buffer=new byte[1024];
	// Use this for initialization
	void Start () {
        writer=new NetworkWriter(new byte[1024]);
        messageInfo=new SwingInfo();
        InitTransport();
	}
    
    void InitTransport()
    {
        NetworkTransport.Init();
        ConnectionConfig connection_config = new ConnectionConfig();
        channelID = connection_config.AddChannel(QosType.Reliable);

        // Create a topology based on the connection config.
        HostTopology topology = new HostTopology(connection_config, 16);
        if(server)
        {
            hostID = NetworkTransport.AddHost(topology,SERVER_PORT);
        }else
        {
            hostID = NetworkTransport.AddHost(topology,SERVER_PORT+1);
        }
        print("Init:"+hostID);
        connectionID=-1;    
        if(server)
        {
            byte error=0;
            print("Start discovery:"+NetworkTransport.StartBroadcastDiscovery(hostID,SERVER_PORT+1,123,1,1,null,0,1000,out error));
        }else
        {
            byte error=0;
            NetworkTransport.SetBroadcastCredentials(hostID,123,1,1,out error);
        }

    }
    
    bool pauseStatus=false;
    void OnApplicationPause(bool paused)
    {
        if(paused==false && paused!=pauseStatus)
        {
            InitTransport();
        }
        pauseStatus=paused;
    }
	
    float timeSinceSent=50f;
	// Update is called once per frame
	void Update () 
    {

        int outConnectionID;
        int outChannelId;

        int receivedSize;
        byte error;
        NetworkEventType evt = NetworkTransport.ReceiveFromHost(hostID, out outConnectionID, out outChannelId, buffer, buffer.Length, out receivedSize, out error);
        if(server)
        {
            if(evt==NetworkEventType.ConnectEvent)
            {
                print("Server got connection:"+outConnectionID);
            }
            if(evt== NetworkEventType.DataEvent)
            {
                reader=new NetworkReader(buffer);
                OnSwingStatus(reader);
                print("Server got data:"+messageInfo.swingID);
            }

        }else
        {
            switch(evt)
            {
                case NetworkEventType.BroadcastEvent:
                {
                        string address;
                        int port;                        
                        NetworkTransport.GetBroadcastConnectionInfo(hostID,out address,out port,out error);
                        //print("Client doing connection:"+connectionID+":"+address);
                        if(connectionID==-1)
                        {
                            connectionID=NetworkTransport.Connect(hostID,address,SERVER_PORT,0,out error);
                            connected=false;
                        }
                }
                break;
            case NetworkEventType.ConnectEvent:
                print("Client connected:"+outConnectionID);
                connected=true;
                connectionID=outConnectionID;
                break;
            case NetworkEventType.DisconnectEvent:
                if(outConnectionID==connectionID)
                {
                    connected=false;
                    connectionID=-1;
                    print("Client disconnected");
                }
                break;
            }
        }
        if(!server && connected && connectionID!=0)
        {
            messageInfo.swingID="10010037";
            timeSinceSent+=Time.deltaTime;
            // only send every 5 seconds if not in session
            if(timeSinceSent>5f || (messageInfo.inSession && timeSinceSent>.5f) )
            {
                writer.SeekZero();
                messageInfo.Serialize(writer);
                NetworkTransport.Send(hostID,connectionID,channelID,writer.AsArray(),writer.Position,out error);
//                print("Send:"+writer.Position+":"+error);
                timeSinceSent=0f;
            }
        }
	}
    
    
}
