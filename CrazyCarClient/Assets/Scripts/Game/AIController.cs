﻿using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using PathCreation;
using Utils;
using LitJson;

public class AIInfo{
    public int id;
    public UserInfo userInfo;
    public float startMoveTime;
    public int needPassTimes;
    public bool isFinishGame = false;
    public bool isStartGame = false;
    public Vector3 startPos;  
    public PathCreator pathCreator;  
    public float distanceTravelled = 0;
    public MPlayer mPlayer;
    
    public void InitAI(float startMoveTime, int needPassTimes, Vector3 startPos, PathCreator pathCreator){
        this.startMoveTime = startMoveTime;
        this.needPassTimes = needPassTimes;
        this.startPos = startPos;
        this.pathCreator = pathCreator;
    }
}

public class AIController : MonoBehaviour, IController {
    public MPlayer mPlayerPrefab;
    private int id = 0;
    private Dictionary<int, AIInfo> aiInfoDic = new Dictionary<int, AIInfo>();

    private bool playerFinishTimeTrial = false;

    private void OnEnable() {
        playerFinishTimeTrial = false;
    }

    private void Start() {
        this.RegisterEvent<MakeAIPlayerEvent>(OnMakeAIPlayer).UnRegisterWhenGameObjectDestroyed(gameObject);
        this.RegisterEvent<EndTimeTrialEvent>(OnEndTimeTrial).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private void OnEndTimeTrial(EndTimeTrialEvent e) {
        playerFinishTimeTrial = true;
    }

    private async void OnMakeAIPlayer(MakeAIPlayerEvent e)
    {
        AIInfo aiInfo = new AIInfo();
        aiInfo.id = id++;
        var userInfo = await this.GetSystem<INetworkSystem>().GetUserInfo(4);
        aiInfo.userInfo = userInfo;
        aiInfo.startMoveTime = e.aiInfo.startMoveTime;
        aiInfo.needPassTimes = e.aiInfo.needPassTimes;
        aiInfo.isStartGame = e.aiInfo.isStartGame;
        aiInfo.startPos = e.aiInfo.startPos;
        aiInfo.pathCreator = e.aiInfo.pathCreator;
        aiInfo.mPlayer = Instantiate(mPlayerPrefab, aiInfo.startPos, Quaternion.identity);
        aiInfo.mPlayer.transform.SetParent(transform, false);
        aiInfo.mPlayer.userInfo = aiInfo.userInfo;
        aiInfo.mPlayer.GetComponent<MPlayerStyle>().ChangeEquip(aiInfo.userInfo.equipInfo.eid,
            aiInfo.userInfo.equipInfo.rid);
        aiInfo.mPlayer.GetComponent<MPlayerStyle>().SetNameText(aiInfo.userInfo.name, aiInfo.userInfo.isVIP);
        aiInfoDic.Add(aiInfo.id, aiInfo);

        Util.DelayExecuteWithSecond(aiInfo.startMoveTime, () => {
            aiInfoDic[aiInfo.id].isStartGame = true;
        });
    }

    private void Update() {
        if (this.GetModel<IGameModel>().CurGameType == GameType.TimeTrial) {
            if (playerFinishTimeTrial) {
                return;
            }
        }

        foreach (var item in aiInfoDic)
        {
            if (item.Value.isStartGame)
            {    
                if (item.Value.isFinishGame){
                    break;
                }           
                if (item.Value.needPassTimes == item.Value.mPlayer.passEndSignTimes){
                    WarningAlertInfo alertInfo = new WarningAlertInfo("AI玩家" + item.Value.userInfo.name + "获胜");
                    UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
                    item.Value.isFinishGame = true;
                    this.SendCommand<EndTimeTrialCommand>();
                } else{
                    item.Value.distanceTravelled += item.Value.userInfo.equipInfo.power / 4 * Time.deltaTime;
                    item.Value.mPlayer.transform.position = item.Value.pathCreator.path.GetPointAtDistance(item.Value.distanceTravelled);
                    item.Value.mPlayer.transform.rotation = Quaternion.Euler(item.Value.pathCreator.path.GetRotationAtDistance(item.Value.distanceTravelled).eulerAngles + Util.pathRotateOffset);
                }
            }
        }
    }

    public IArchitecture GetArchitecture()
    {
        return CrazyCar.Interface;
    }
}
