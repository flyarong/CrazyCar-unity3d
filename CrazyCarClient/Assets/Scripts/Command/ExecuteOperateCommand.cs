﻿using LitJson;
using System.Text;
using UnityEngine;
using Utils;
using QFramework;

public enum ControllerType {
    Horizontal = 0,
    Vertical,
    Speed,
    Attack
}


/// <summary>
/// 此处用于执行操作的命令，不仅处理直接来自用户操作的命令，还处理来自服务器的命令。
/// </summary>
public class ExecuteOperateCommand : AbstractCommand {
    private int uid;
    private ControllerType controllerType;
    private int value;

    public ExecuteOperateCommand(int uid, ControllerType controllerType, int value) {
        this.uid = uid;
        this.controllerType = controllerType;
        this.value = value;
    }

    protected override void OnExecute() {
        if (this.GetModel<IGameModel>().CurGameType == GameType.Match && 
            uid == this.GetSystem<IPlayerManagerSystem>().SelfPlayer.userInfo.uid) {
            this.SendCommand(new PostPlayerOperatMsgCommand(controllerType, value));
        }

        MPlayer mPlayer;
        if (uid == this.GetSystem<IPlayerManagerSystem>().SelfPlayer.userInfo.uid) {
            mPlayer = this.GetSystem<IPlayerManagerSystem>().SelfPlayer;
        } else {
            mPlayer = this.GetSystem<IPlayerManagerSystem>().peers[uid];
        }
        
        if (controllerType == ControllerType.Horizontal) {
            mPlayer.hInput = value;
        } else if (controllerType == ControllerType.Vertical) {
            mPlayer.vInput = value;
        } else if (controllerType == ControllerType.Speed){
            if (value > 0) {
                if (mPlayer.currentForce > 0 && mPlayer.isGround && !mPlayer.isDrifting &&
                    mPlayer.rig.velocity.sqrMagnitude > 10) {
                    mPlayer.StartDrift();
                }
            } else {
                if (mPlayer.isDrifting) {
                    mPlayer.Boost(mPlayer.boostForce);
                    this.SendEvent(new StartDriftEvent(mPlayer.userInfo.uid));
                    mPlayer.StopDrift();
                }
            }
        } else if (controllerType == ControllerType.Attack) {
            mPlayer.Attack(value);
        }
    }
}