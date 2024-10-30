﻿using LitJson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using QFramework;

public class LoginUI : MonoBehaviour, IController {
    public Button standAloneBtn;
    public InputField userNameInput;
    public InputField passwordInput;
    public Toggle rememberToggle; 
    public Button loginBtn;
    public Button registerBtn;

    public IArchitecture GetArchitecture() {
        return CrazyCar.Interface;
    }

    private void Start() {
        standAloneBtn.onClick.AddListener(() => {
            this.GetSystem<ISoundSystem>().PlaySound(SoundType.Button_Low);
            this.SendCommand<EnableStandAloneCommand>();
        });
        rememberToggle.isOn = this.GetModel<IUserModel>().RememberPassword == 1;
        if (rememberToggle.isOn) {
            userNameInput.text = this.GetModel<IUserModel>().Name.Value;
            passwordInput.text = this.GetModel<IUserModel>().Password;
        }

        loginBtn.onClick.AddListener(() => {
            this.GetSystem<ISoundSystem>().PlaySound(SoundType.Button_Low);
            if (userNameInput.text == "" || passwordInput.text == "") {
                WarningAlertInfo alertInfo = new WarningAlertInfo("Please enter the content");
                UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
                return;
            }

            this.SendCommand(new LoginCommand(userNameInput.text, passwordInput.text, rememberToggle.isOn));
        });

        registerBtn.onClick.AddListener(() => {
            this.GetSystem<ISoundSystem>().PlaySound(SoundType.Button_Low);
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.RegisterUI));
            gameObject.SetActiveFast(false);
        });
    }
}
