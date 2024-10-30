﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using System.Text;
using LitJson;

public class PlayerCreateMsg {
    public int cid;
    public UserInfo userInfo = new UserInfo();
    public Vector3 pos = new Vector3();
    public Vector3 speed;
    public long timestamp;

    public PlayerCreateMsg(PlayerStateMsg playerStateMsg, UserInfo userInfo)
    {
        this.cid = playerStateMsg.cid;
        this.userInfo = userInfo;
        this.pos = playerStateMsg.pos;
        this.speed = playerStateMsg.speed;
        this.timestamp = playerStateMsg.timestamp;
    }

    public PlayerCreateMsg(PlayerOperatMsg playerOperatMsg, UserInfo userInfo) {
        this.cid = playerOperatMsg.cid;
        this.userInfo = userInfo;
        this.pos = Vector3.zero;
        this.speed = Vector3.zero;
        this.timestamp = playerOperatMsg.timestamp;
    }

    public PlayerCreateMsg(){
    }
}

public class PlayerStateMsg {
    public int cid;
    public int uid;
    public Vector3 pos = new Vector3();
    public Vector3 speed;
    public long timestamp;
}

public class PlayerOperatMsg {
    public int cid;
    public int uid;
    public ControllerType controllerType;
    public int value;
    public long timestamp;
}

public class PlayerCompleteMsg {
    public int cid;
    public int uid;
    public long completeTime;
}

public interface IPlayerManagerSystem : ISystem {
    public MPlayer SelfPlayer { get; set; }
    public MPlayer GetPlayerByUid(int uid);
    public Dictionary<int, MPlayer> peers { get; set; }
    public void RespondStateAction(PlayerStateMsg playerStateMsg);
    public void RespondOperatAction(PlayerOperatMsg playerOperatMsg);
    public void RemovePlayer(int uid);
}

public class PlayerManagerSystem : AbstractSystem, IPlayerManagerSystem {
    public Dictionary<int, MPlayer> peers { get; set; } = new Dictionary<int, MPlayer>();

    public MPlayer SelfPlayer { get; set; }

    public MPlayer GetPlayerByUid(int uid) {
        if (uid == SelfPlayer.userInfo.uid) {
            return SelfPlayer;
        }

        MPlayer player;
        if (peers.TryGetValue(uid, out player)) {
            return player;
        }

        return null;
    }

    public void RespondStateAction(PlayerStateMsg playerStateMsg) {
        if (playerStateMsg.uid == this.GetModel<IUserModel>().Uid.Value) {
            this.GetSystem<IPlayerManagerSystem>().SelfPlayer.ConfirmStatus(playerStateMsg);
        } else {
            AdjustPeerPlayer(playerStateMsg);
        }
    }

    public void RespondOperatAction(PlayerOperatMsg playerOperatMsg) {
        if (playerOperatMsg.uid == this.GetModel<IUserModel>().Uid.Value) {
            
        } else {
            AdjustPeerPlayer(playerOperatMsg);
        }
    }

    private async void AdjustPeerPlayer(PlayerStateMsg playerStateMsg) {
        MPlayer peer = null;
        
        if (!this.GetSystem<IPlayerManagerSystem>().peers.TryGetValue(playerStateMsg.uid, out peer)) {
            var userInfo = await this.GetSystem<INetworkSystem>().GetUserInfo(playerStateMsg.uid);
            if (!this.GetSystem<IPlayerManagerSystem>().peers.TryGetValue(playerStateMsg.uid, out peer))
            {
                PlayerCreateMsg playerCreateMsg = new PlayerCreateMsg(playerStateMsg, userInfo);
                this.SendEvent(new MakeNewPlayerEvent(playerCreateMsg));
            }
        } else {
            peer.AdjustPosition(playerStateMsg.pos, playerStateMsg.speed);
        }
    }

    private async void AdjustPeerPlayer(PlayerOperatMsg playerOperatMsg) {
        MPlayer peer = null;
        if (!this.GetSystem<IPlayerManagerSystem>().peers.TryGetValue(playerOperatMsg.uid, out peer)) {
            var userInfo = await this.GetSystem<INetworkSystem>().GetUserInfo(playerOperatMsg.uid);
            if (!this.GetSystem<IPlayerManagerSystem>().peers.TryGetValue(playerOperatMsg.uid, out peer)) {
                PlayerCreateMsg playerCreateMsg = new PlayerCreateMsg(playerOperatMsg, userInfo);
                this.SendEvent(new MakeNewPlayerEvent(playerCreateMsg));
            }
        } else {
            this.SendEvent(new PeerControllerEvent(playerOperatMsg));
        }
    }

    public void RemovePlayer(int uid) {
        if (uid == SelfPlayer.userInfo.uid) {
            return;
        }

        MPlayer b = null;
        if (!peers.TryGetValue(uid, out b)) {
            return;
        }

        peers.Remove(uid);
        b.DestroySelf();
    }

    protected override void OnInit() {
        
    }
}
