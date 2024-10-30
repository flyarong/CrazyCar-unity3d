﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using System;
using Cysharp.Threading.Tasks;
using QFramework;

public class TimeTrialGameUI : MonoBehaviour, IController {
    public CountDownAnim countDownAnim;
    public Text limitTimeText;

    private Coroutine limitTimeCor;

    private void OnEnable() {
        if (!this.GetModel<IGameModel>().SceneLoaded.Value) {
            return;
        }

        countDownAnim.PlayAnim(3, () => {
            this.GetModel<ITimeTrialModel>().StartTime.Value = Util.GetTime() / 1000;
            this.SendCommand(new ExecuteOperateCommand(this.GetModel<IUserModel>().Uid, ControllerType.Vertical, 1));
            Debug.Log("++++++ StartTime = " + this.GetModel<ITimeTrialModel>().StartTime);
            limitTimeCor = StartCoroutine(Util.CountdownCor(this.GetModel<ITimeTrialModel>().SelectInfo.Value.limitTime,
                () => {
                    this.GetModel<ITimeTrialModel>().IsArriveLimitTime.Value = true;
                    Debug.Log("++++++ arrive limit time ");
                }, limitTimeText));
        });       
    }

    private void Start() {       
        MakeAI();
        limitTimeText.text = this.GetModel<ITimeTrialModel>().SelectInfo.Value.limitTime.ToString();

        this.RegisterEvent<EndTimeTrialEvent>(OnEndTimeTrial).UnRegisterWhenGameObjectDestroyed(gameObject);
    }

    private async UniTaskVoid MakeAI(){
        await UniTask.WaitForFixedUpdate();
        AIInfo aiInfo = new AIInfo();
        aiInfo.InitAI(3, this.GetModel<ITimeTrialModel>().SelectInfo.Value.times, 
            this.GetSystem<IPlayerManagerSystem>().SelfPlayer.GetComponent<Transform>().position + new Vector3(4, 0, 0), 
            this.GetModel<IMapControllerModel>().PathCreator);
        this.SendCommand(new MakeAIPlayerCommand(aiInfo));
    }

    private void OnEndTimeTrial(EndTimeTrialEvent e) {
        StopCoroutine(limitTimeCor);
        this.GetSystem<IPlayerManagerSystem>().SelfPlayer.isLockSpeed = true;
        if (this.GetModel<IGameModel>().StandAlone) {
            WarningAlertInfo alertInfo = new WarningAlertInfo("Game Over");
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
            Util.DelayExecuteWithSecond(2.0f, () => {
                this.SendCommand(new LoadSceneCommand(SceneID.Index));
            });           
        } else {
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.GameResultUI, UILevelType.UIPage));
        }
    }

    public IArchitecture GetArchitecture() {
        return CrazyCar.Interface;
    }
}
