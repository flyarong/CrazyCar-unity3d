using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QFramework;
using System.Text;
using LitJson;
using Utils;
using System;

public interface IMatchRoomSystem : ISystem {
    public void MatchRoomCreate();
    public void MatchRoomJoin();
    public void MatchRoomStatus();
    public void MatchRoomEixt();
    public void MatchRoomStart();
    public void MatchRoomClose();
    public void OnCreateMsg(JsonData recJD);
    public void OnJoinMsg(JsonData recJD);
    public void OnStatusMsg(JsonData recJD);
    public void OnExitMsg(JsonData recJD);
    public void OnStartMsg(JsonData recJD);
}

public class MatchRoomSystem : AbstractSystem, IMatchRoomSystem {
    private void MatchRoomConnect(Action succ) {
        this.GetSystem<INetworkSystem>().Connect(RequestUrl.matchRoomWSUrl, RequestUrl.kcpMatchRoomUrl, RequestUrl.matchRoomKCPPort);
        this.GetSystem<INetworkSystem>().ConnectSuccAction = () => {
            Debug.Log("MatchRoom Connect Succ");
            succ?.Invoke();
        };
    }

    public void MatchRoomCreate() {
        MatchRoomConnect(() => {
            this.GetModel<IMatchModel>().IsHouseOwner = true;
            StringBuilder sb = new StringBuilder();
            JsonWriter w = new JsonWriter(sb);
            w.WriteObjectStart();
            w.WritePropertyName("msg_type");
            w.Write((int)MsgType.MatchRoomCreate);
            w.WritePropertyName("timestamp");
            w.Write(Util.GetTime());
            w.WritePropertyName("room_id");
            w.Write(this.GetModel<IMatchModel>().RoomId);
            w.WritePropertyName("uid");
            w.Write(this.GetModel<IUserModel>().Uid);
            w.WritePropertyName("eid");
            w.Write(this.GetModel<IUserModel>().EquipInfo.Value.eid);
            w.WritePropertyName("token");
            w.Write(this.GetModel<IGameModel>().Token);
            w.WriteObjectEnd();
            Debug.Log("MatchRoomCreate : " + sb.ToString());
            this.GetSystem<INetworkSystem>().SendMsgToServer(sb.ToString());
        });
    }

    public void MatchRoomEixt() {
        StringBuilder sb = new StringBuilder();
        JsonWriter w = new JsonWriter(sb);
        w.WriteObjectStart();
        w.WritePropertyName("msg_type");
        w.Write((int)MsgType.MatchRoomExit);
        w.WritePropertyName("timestamp");
        w.Write(Util.GetTime());
        w.WritePropertyName("room_id");
        w.Write(this.GetModel<IMatchModel>().RoomId);
        w.WritePropertyName("uid");
        w.Write(this.GetModel<IUserModel>().Uid);
        w.WritePropertyName("eid");
        w.Write(this.GetModel<IUserModel>().EquipInfo.Value.eid);
        w.WritePropertyName("is_house_owner");
        w.Write(this.GetModel<IMatchModel>().IsHouseOwner ? 1 : 0);
        w.WriteObjectEnd();
        Debug.Log("MatchRoomEixt : " + sb.ToString());
        this.GetSystem<INetworkSystem>().SendMsgToServer(sb.ToString());
    }

    public void MatchRoomJoin() {
        MatchRoomConnect(() => {
            this.GetModel<IMatchModel>().IsHouseOwner = false;
            StringBuilder sb = new StringBuilder();
            JsonWriter w = new JsonWriter(sb);
            w.WriteObjectStart();
            w.WritePropertyName("msg_type");
            w.Write((int)MsgType.MatchRoomJoin);
            w.WritePropertyName("timestamp");
            w.Write(Util.GetTime());
            w.WritePropertyName("room_id");
            w.Write(this.GetModel<IMatchModel>().RoomId);
            w.WritePropertyName("uid");
            w.Write(this.GetModel<IUserModel>().Uid);
            w.WritePropertyName("eid");
            w.Write(this.GetModel<IUserModel>().EquipInfo.Value.eid);
            w.WritePropertyName("token");
            w.Write(this.GetModel<IGameModel>().Token);
            w.WriteObjectEnd();
            Debug.Log("MatchRoomJoin : " + sb.ToString());
            this.GetSystem<INetworkSystem>().SendMsgToServer(sb.ToString());
        });
    }

    public void MatchRoomStatus() {
        StringBuilder sb = new StringBuilder();
        JsonWriter w = new JsonWriter(sb);
        w.WriteObjectStart();
        w.WritePropertyName("msg_type");
        w.Write((int)MsgType.MatchRoomStatus);
        w.WritePropertyName("timestamp");
        w.Write(Util.GetTime());
        w.WritePropertyName("room_id");
        w.Write(this.GetModel<IMatchModel>().RoomId);
        w.WritePropertyName("uid");
        w.Write(this.GetModel<IUserModel>().Uid);
        w.WritePropertyName("eid");
        w.Write(this.GetModel<IUserModel>().EquipInfo.Value.eid);
        w.WriteObjectEnd();
        Debug.Log("MatchRoomStatus : " + sb.ToString());
        this.GetSystem<INetworkSystem>().SendMsgToServer(sb.ToString());
    }

    public void MatchRoomStart() {
        MatchInfo info = this.GetModel<IMatchModel>().SelectInfo.Value;
        StringBuilder sb = new StringBuilder();
        JsonWriter w = new JsonWriter(sb);
        w.WriteObjectStart();
        w.WritePropertyName("msg_type");
        w.Write((int)MsgType.MatchRoomStart);
        w.WritePropertyName("timestamp");
        w.Write(Util.GetTime());
        w.WritePropertyName("room_id");
        w.Write(this.GetModel<IMatchModel>().RoomId);
        w.WritePropertyName("uid");
        w.Write(this.GetModel<IUserModel>().Uid);
        w.WritePropertyName("cid");
        w.Write(info.cid);
        w.WriteObjectEnd();
        Debug.Log("MatchRoomStart : " + sb.ToString());
        this.GetSystem<INetworkSystem>().SendMsgToServer(sb.ToString());
    }

    public void MatchRoomClose() {
        this.GetSystem<INetworkSystem>().CloseConnect();
    }

    public void OnCreateMsg(JsonData recJD) {
        int code = (int)recJD["code"];
        if (this.GetModel<IUserModel>().Uid != (int)recJD["uid"]) {
            return;
        }
        Debug.Log("OnCreateMsg = " + code);
        if (code == 200) {
            this.SendEvent<MatchRoomCreateOrJoinSuccEvent>();
        } else if (code == 421) {
            WarningAlertInfo info = new WarningAlertInfo("Room already exists");
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, info));
            this.SendEvent<MatchRoomCreateOrJoinFailEvent>();
        } else if (code == 422) {
            WarningAlertInfo info = new WarningAlertInfo("The number of rooms has reached the upper limit");
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, info));
            this.SendEvent<MatchRoomCreateOrJoinFailEvent>();
        } else if (code == 423) {
            WarningAlertInfo info = new WarningAlertInfo("Token Past Due");
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, info));
            this.SendEvent<MatchRoomCreateOrJoinFailEvent>();
        }
    }

    public void OnExitMsg(JsonData recJD) {
        int code = (int)recJD["code"];
        Debug.Log("OnExitMsg = " + recJD.ToJson());
        if (code == 404) {
            InfoConfirmInfo info = new InfoConfirmInfo(content: "Without this room", success: () => {
                this.SendEvent<MatchRoomExitEvent>();
            }, type: ConfirmAlertType.Single);
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.InfoConfirmAlert, UILevelType.Alart, info));
        } else if (code == 200) {
            JsonData data = recJD["data"];
            int exitUid = (int)data["exit_uid"];
            if (exitUid == this.GetModel<IUserModel>().Uid) {
                this.SendEvent<MatchRoomExitEvent>();
            } else if (this.GetModel<IMatchModel>().MemberInfoDic[exitUid].isHouseOwner) {
                InfoConfirmInfo info = new InfoConfirmInfo(content: "The owner quits and the room dissolves", 
                    success: () => {
                        this.SendEvent<MatchRoomExitEvent>();
                    }, type: ConfirmAlertType.Single);
                UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.InfoConfirmAlert, UILevelType.Alart, info));
            } else {
                WarningAlertInfo alertInfo = new WarningAlertInfo("Members of the exit");
                UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
                JsonData players = data["players"];
                var infos = this.GetModel<IMatchModel>().MemberInfoDic;
                infos.Clear();
                for (int i = 0; i < players.Count; i++) {
                    MatchRoomMemberInfo info = new MatchRoomMemberInfo();
                    info.memberName = (string)players[i]["member_name"];
                    info.isHouseOwner = (bool)players[i]["is_house_owner"];
                    info.aid = (int)players[i]["aid"];
                    info.uid = (int)players[i]["uid"];
                    info.canWade = (bool)players[i]["can_wade"];
                    info.index = i;
                    infos.Add(info.uid, info);
                }
                this.SendEvent<MatchRoomUpdateStatusEvent>();
            }
        }
    }

    public void OnJoinMsg(JsonData recJD) {
        int code = (int)recJD["code"];
        Debug.Log("OnJoinMsg = " + recJD.ToJson());
        if (code == 200) {
            this.SendEvent<MatchRoomCreateOrJoinSuccEvent>();
        } else if (code == 404) {
            WarningAlertInfo alertInfo = new WarningAlertInfo("Without this room");
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
            this.SendEvent<MatchRoomCreateOrJoinFailEvent>();
        } else if (code == 422) {
            WarningAlertInfo alertInfo = new WarningAlertInfo("The room is full");
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
            this.SendEvent<MatchRoomCreateOrJoinFailEvent>();
        } else if (code == 423) {
            WarningAlertInfo alertInfo = new WarningAlertInfo("Token Past Due");
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
            this.SendEvent<MatchRoomCreateOrJoinFailEvent>();
        }
    }

    public void OnStatusMsg(JsonData recJD) {
        int code = (int)recJD["code"];
        Debug.Log("OnStatusMsg = " + recJD.ToJson());
        if (code == 200) {
            JsonData players = recJD["data"];
            var infos = this.GetModel<IMatchModel>().MemberInfoDic;
            infos.Clear();
            bool hasHouseOwner = false;
            for (int i = 0; i < players.Count; i++) {
                MatchRoomMemberInfo info = new MatchRoomMemberInfo();
                info.memberName = (string)players[i]["member_name"];
                info.isHouseOwner = (bool)players[i]["is_house_owner"];
                if (info.isHouseOwner) {
                    hasHouseOwner = true;
                }
                info.aid = (int)players[i]["aid"];
                info.uid = (int)players[i]["uid"];
                info.canWade = (bool)players[i]["can_wade"];
                info.index = i;
                infos.Add(info.uid, info);
            }
            if (hasHouseOwner) {
                this.SendEvent<MatchRoomUpdateStatusEvent>();
            } else {
                InfoConfirmInfo info = new InfoConfirmInfo(content: "The owner quits and the room dissolves", 
                    success: () => {
                        this.SendEvent<MatchRoomExitEvent>();
                    }, type: ConfirmAlertType.Single);
                UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.InfoConfirmAlert, UILevelType.Alart, info));
            }  
        } else if (code == 404) {
            InfoConfirmInfo info = new InfoConfirmInfo(content: "Without this room", 
                success: () => {
                    this.SendEvent<MatchRoomExitEvent>();
                }, type: ConfirmAlertType.Single);
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.InfoConfirmAlert, UILevelType.Alart, info));
        }
    }

    public void OnStartMsg(JsonData recJD) {
        Debug.Log("OnStartMsg = " + recJD.ToJson());
        int code = (int)recJD["code"];
        if (code == 200) {
            this.GetSystem<IDataParseSystem>().ParseSelectMatch(recJD["data"]);
            this.SendEvent<MatchRoomStartEvent>();
        } else {
            WarningAlertInfo alertInfo = new WarningAlertInfo("This map requires all player vehicles to be able to wade");
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
        }
    }

    protected override void OnInit() {

    }
}
