using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class oldSwingNetwork : MonoBehaviour
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
    
    public void OnSwingStatus(NetworkMessage netMsg)
    {        
        messageInfo.Deserialize(netMsg.reader);
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
    
    
    NetworkServerSimple mServer=null;
    NetworkClient mClient=null;
    bool mConnecting=false;
    
    public void ConnectClient(string address)
    {
        if(!mClient.isConnected && !mConnecting)
        {
            print("Trying connect");
            mConnecting=true;
            mClient.Connect(address,SERVER_PORT);
        }
    }

    public void OnConnected(NetworkMessage msg)
    {
        mConnecting=false;
        Debug.Log("Connected to server");
    }

    public void OnDisconnected(NetworkMessage msg)
    {
        mConnecting=false;
        Debug.Log("Disconnected from server");
    }

    public void OnError(NetworkMessage msg)
    {
        mConnecting=false;
    }

    
	// Use this for initialization
	void Start () {
        NetworkTransport.Init();
        ConnectionConfig connection_config = new ConnectionConfig();
        int channelId = connection_config.AddChannel(QosType.Reliable);

        // Create a topology based on the connection config.
        HostTopology topology = new HostTopology(connection_config, 32);
        int hostId = NetworkTransport.AddHost(topology);
        
        messageInfo=new SwingInfo();
            
        GetComponent<NetworkDiscovery>().Initialize();
        if(server)
        {
            mServer=new NetworkServerSimple();
            mServer.RegisterHandler(MESSAGE_SWINGINFO,OnSwingStatus);
            print("Listen:"+mServer.Listen(SERVER_PORT));
            GetComponent<NetworkDiscovery>().StartAsServer();
        }else
        {
            mClient=new NetworkClient();
            mClient.RegisterHandler(MsgType.Connect, OnConnected);
            mClient.RegisterHandler(MsgType.Disconnect, OnDisconnected);
            mClient.RegisterHandler(MsgType.Error, OnError);            
            GetComponent<NetworkDiscovery>().StartAsClient();
        }
	}
	
    float timeSinceSent=0f;
	// Update is called once per frame
	void Update () 
    {
        if(mServer!=null)
        {
            mServer.Update();
        }else if(mClient!=null)
        {
            if( mClient.isConnected)
            {

                messageInfo.swingID="10010037";
                timeSinceSent+=Time.deltaTime;
                // only send every 5 seconds if not in session
                if(timeSinceSent>5f || (messageInfo.inSession && timeSinceSent>.5f) )
                {
                    mClient.Send(MESSAGE_SWINGINFO,messageInfo);
                    timeSinceSent=0f;
                }
            }else
            {
                foreach(string addr in GetComponent<NetworkDiscovery>().broadcastsReceived.Keys)
                {
                    ConnectClient(addr);
                    break;
                }
            }
        }
        
	}
    
    
}
