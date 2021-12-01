﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using System;
using TFramework;

public class TimeTrialGameUI : MonoBehaviour, IController {
    public CountDownAnim countDownAnim;
    public Text limitTimeText;

    private Coroutine limitTimeCor;

    private void OnEnable() {
        if (!this.GetModel<IGameControllerModel>().SceneLoaded.Value) {
            return;
        }

        countDownAnim.PlayAnim(3, () => {
            this.GetModel<ITimeTrialModel>().StartTime.Value = Util.GetTime() / 1000;
            this.GetSystem<IPlayerManagerSystem>().SelfPlayer.vInput = 1;
            Debug.Log("++++++ StartTime = " + this.GetModel<ITimeTrialModel>().StartTime);
            limitTimeCor = StartCoroutine(Util.CountdownCor(this.GetModel<ITimeTrialModel>().SelectInfo.Value.limitTime,
                () => {
                    this.GetModel<ITimeTrialModel>().IsArriveLimitTime.Value = true;
                    Debug.Log("++++++ arrive limit time ");
                }, limitTimeText));
        });       
    }

    private void Start() {       
        limitTimeText.text = this.GetModel<ITimeTrialModel>().SelectInfo.Value.limitTime.ToString();

        this.RegisterEvent<CompleteTimeTrialEvent>(OnCompleteTimeTrial);
    }

    private void OnCompleteTimeTrial(CompleteTimeTrialEvent e) {
        StopCoroutine(limitTimeCor);
        this.SendCommand(new ShowResultUICommand());
    }

    private void OnDestroy() {
        this.UnRegisterEvent<CompleteTimeTrialEvent>(OnCompleteTimeTrial);
    }

    public IArchitecture GetArchitecture() {
        return CrazyCar.Interface;
    }
}
