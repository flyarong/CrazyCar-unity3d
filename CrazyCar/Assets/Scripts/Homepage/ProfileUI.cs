﻿using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using TFramework;

public class ProfileUI : MonoBehaviour, IController {
    public Button closeBtn;
    public Image avatarImage;
    public Image vipImage;
    public InputField userNameInput;
    public Button userNameBtn;
    public InputField passwordInput;
    public Button passwordBtn;
    public Text travelTimesText;
    public Text avatarText;
    public Text mapsText;
    public Text starText;

    public IArchitecture GetArchitecture() {
        return CrazyCar.Interface;
    }

    private void OnEnable() {
        UserInfo userInfo = this.GetModel<IPlayerInfoModel>().GetPlayerInfoByUid(GameController.manager.userInfo.uid);        avatarImage.sprite = GameController.manager.resourceManager.GetAvatarResource(GameController.manager.userInfo.aid);
        vipImage.gameObject.SetActiveFast(userInfo.isVIP);
        userNameInput.text = userInfo.name;
        passwordInput.text = PlayerPrefs.GetString(PrefKeys.password);
        starText.text = userInfo.star.ToString();
        travelTimesText.text = userInfo.travelTimes.ToString();
        avatarText.text = userInfo.avatarNum.ToString();
        mapsText.text = userInfo.mapNum.ToString(); 
    }

    private void Start() {
        closeBtn.onClick.AddListener(() => {
            UIManager.manager.HidePage(UIPageType.ProfileUI);
        });
        userNameBtn.onClick.AddListener(() => {
            if (userNameInput.text == GameController.manager.userInfo.name) {
                GameController.manager.warningAlert.ShowWithText(I18N.manager.GetText("Consistent with the original nickname"));
            } else {

            }
        });
        passwordBtn.onClick.AddListener(() => {
            if (passwordInput.text == PlayerPrefs.GetString(PrefKeys.password)) {
                GameController.manager.warningAlert.ShowWithText(I18N.manager.GetText("Consistent with the original password"));
            } else if (passwordInput.text.Length < 6) {
                GameController.manager.warningAlert.ShowWithText(I18N.manager.GetText("The password must contain more than six characters"));
            } else {
                StringBuilder sb = new StringBuilder();
                JsonWriter w = new JsonWriter(sb);
                w.WriteObjectStart();
                w.WritePropertyName("password");
                w.Write(passwordInput.text);
                w.WriteObjectEnd();
                Debug.Log("++++++ " + sb.ToString());
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                StartCoroutine(Util.POSTHTTP(url: NetworkController.manager.HttpBaseUrl + RequestUrl.modifyPersonalInfoUrl,
                    data: bytes, token: GameController.manager.token, 
                    succData: (data) => {
                        GameController.manager.warningAlert.ShowWithText(I18N.manager.GetText("Modify Successfully"));
                        PlayerPrefs.SetString(PrefKeys.password, passwordInput.text);
                    },
                    code: (code) => {
                        if (code == 423) {
                            GameController.manager.warningAlert.ShowWithText(I18N.manager.GetText("Fail To Modify"));
                        } else if (code == 404) {
                            GameController.manager.warningAlert.ShowWithText(I18N.manager.GetText("Information Error"));
                        } 
                    }));
            }
        });
    }
}
