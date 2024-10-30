﻿using LitJson;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using QFramework;

public class RegisterUI : MonoBehaviour, IController {
    public InputField userNameInput;
    public InputField passwordInput;
    public Button registerBtn;
    public Button closeBtn;

    public IArchitecture GetArchitecture() {
        return CrazyCar.Interface;
    }

    private void Start() {
        closeBtn.onClick.AddListener(() => {
            this.GetSystem<ISoundSystem>().PlaySound(SoundType.Close);
            UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.LoginUI));
            gameObject.SetActiveFast(false);
        });

        registerBtn.onClick.AddListener(() => {
            this.GetSystem<ISoundSystem>().PlaySound(SoundType.Button_Low);
            if (userNameInput.text == "" || passwordInput.text == "") {
                WarningAlertInfo alertInfo = new WarningAlertInfo("Please enter the content");
                UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
                return;
            }

            if (passwordInput.text.Length < 6) {
                WarningAlertInfo alertInfo = new WarningAlertInfo("The password must contain more than six characters");
                UIController.Instance.ShowPage(new ShowPageInfo(UIPageType.WarningAlert, UILevelType.Alart, alertInfo));
                return;
            }

            this.SendCommand(new RegisterCommand(userNameInput.text, passwordInput.text));
        });     
    }
}
