﻿using LitJson;
using System.Text;
using UnityEngine;
using Utils;
using QFramework;

public class BuyAvatarCommand : AbstractCommand {
    private AvatarInfo mAvatarInfo;

    public BuyAvatarCommand(AvatarInfo avatarInfo) {
        mAvatarInfo = avatarInfo;
    }

    protected override async void OnExecute() {
        StringBuilder sb = new StringBuilder();
        JsonWriter w = new JsonWriter(sb);
        w.WriteObjectStart();
        w.WritePropertyName("aid");
        w.Write(mAvatarInfo.aid);
        w.WriteObjectEnd();
        Debug.Log("++++++ " + sb.ToString());
        byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = await this.GetSystem<INetworkSystem>().Post(url: this.GetSystem<INetworkSystem>().HttpBaseUrl + RequestUrl.buyAvatarUrl,
        this.GetModel<IGameModel>().Token.Value,bytes);
        if (result.serverCode == 200) {
            this.GetModel<IUserModel>().Star.Value = (int)result.serverData["star"];
            mAvatarInfo.isHas = true;
            this.GetModel<IUserModel>().AvatarNum.Value++;
            this.SendEvent<UnlockAvatarEvent>();
        } 
    }
}