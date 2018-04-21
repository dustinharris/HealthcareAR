using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Button : MonoBehaviour {

	bool selected;
	Outline outLine;
	Color black;
	Color red;

	void Start(){
		outLine = GetComponent<Outline> ();
		black = new Color(0,0,0, 0.6f);
		red = new Color(1,0,0,0.7f);
	}


	public void FullScreen(){
		Connections.self.container.gameObject.SetActive(false);
		Connections.self.fullScreenPanel.SetActive(true);
		fullScreen.currently = Connections.users[gameObject];
		Connections.users[gameObject].isFullScreen = true;
		Connections.users[gameObject].RpcSetFullScreen(true);
	}


	public void Select(){

		foreach(var user in Connections.selectedUsers){
			user.client.transform.GetComponentInChildren<Image>().color = black;
		}
		Connections.selectedUsers.Clear();

		transform.GetChild(0).GetComponent<Image>().color = red;
		Connections.selectedUsers.Add(Connections.users[gameObject]);
		if(SettingsUI.self)
			SettingsUI.self.Click ();
	}

}
