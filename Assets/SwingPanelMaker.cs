﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwingPanelMaker : MonoBehaviour {

    SwingNetwork net;

    public GameObject panelObj;
    public Canvas canvas;
    
    
    Dictionary<int,GameObject> mPanels=new Dictionary<int,GameObject>();
    
	// Use this for initialization
	void Start () {
		net = GetComponent<SwingNetwork>();
	}
	
	// Update is called once per frame
	void Update () {


        Dictionary<string,SwingNetwork.SwingInfo> swings=net.GetSwings();
        foreach(SwingNetwork.SwingInfo info in swings.Values)
        {
            string idStr=info.swingID;
            int swingNum=int.Parse(idStr.Substring(1,3));
            int netNum=int.Parse(idStr.Substring(4,3));
            char netChar=(char)('A'+netNum);
            
            if(!mPanels.ContainsKey(swingNum))
            {
                mPanels[swingNum]=Instantiate(panelObj);
                mPanels[swingNum].transform.parent=canvas.transform;
                RectTransform t = (RectTransform)mPanels[swingNum].transform;
                
                int xPos=swingNum%4;
                int yPos=2-swingNum/4;
                
                t.anchorMin=new Vector2(xPos*0.25f,yPos*.3f);
                t.anchorMax=new Vector2((xPos+1)*.25f,(yPos+1)*.3f);
                t.offsetMin=new Vector2(2,2);
                t.offsetMax=new Vector2(2,2);
                
            }
            string panelText=string.Format("<b>{0}{1}</b>\nRider:{2}\nSwing battery:{3,3:P0}\nHeadset battery:{4,3:P0}\nIn session:{5}\nRide time:{6,5:N1}\nSwing angle:{7,4:N1}\nConnection state:{8,2}",            
                netChar,swingNum,info.riderID,info.swingBattery*0.01f,info.headsetBattery*0.01f,info.inSession,info.rideTime,info.swingAngle,info.connectionState);
            // update the panel for this message
            mPanels[swingNum].transform.GetChild(0).GetComponent<Text>().text=panelText;
			mPanels [swingNum].transform.GetChild (0).GetComponent<Text> ().fontSize = 16;
            
            Image panelBG=mPanels[swingNum].GetComponent<Image>();
            if(info.headsetBattery<20 || info.swingBattery<20 || (info.connectionState&1) == 0)
            {
				if ((info.connectionState & 1) == 0) {
					print ("bad");
					mPanels [swingNum].GetComponent<Image> ().color = new Color (1.0f, 0.0f, 0.0f);
				} else {
					mPanels [swingNum].GetComponent<Image> ().color = new Color (1.0f, 0.45f, 0.0f);
				}

            }else if(info.inSession==false)
            {
				print ("in session");
				mPanels[swingNum].GetComponent<Image>().color=new Color(1f,1f,.5f);
            }else
            {
				print ("out of session");
				mPanels[swingNum].GetComponent<Image>().color=new Color(.5f,1f,.5f);
            }
			//mPanels[swingNum].GetComponent<Image>().color=Color.HSVToRGB(swingNum*.05f,.5f,.8f);

            
        }
	}
}
