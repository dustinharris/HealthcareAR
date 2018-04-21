using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenShare : MonoBehaviour {

	void Update(){
		GetComponent<Toggle>().isOn = SettingsUI.self.isScreenSharing;
		gameObject.SetActive(SettingsUI.self.selectedUser.isSupportScreenShare);
	}

	public void ChangeValue(){
		SettingsUI.self.ScreenSharing(GetComponent<Toggle>().isOn);
	}
}
