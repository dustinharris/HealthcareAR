using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class mainUI : MonoBehaviour {

	public static mainUI self; // Static instance of this script.
	public GameObject StartUI, InitUI, ConnectMenuUI, ConnectedUI; // UIs.
	List<GameObject> UIs; // List of UIs.

	void Start () {
		UIs = new List<GameObject> (){StartUI, InitUI, ConnectMenuUI, ConnectedUI}; // Create list of UIs.
		StartUI.SetActive (true); // Activate StartUI.
		InitUI.SetActive (false);
		ConnectMenuUI.SetActive (false);
		ConnectedUI.SetActive (false);
		self = this;

		Screen.sleepTimeout = SleepTimeout.NeverSleep;
	}

	public void SetActiveUI(GameObject obj){
		// This function set 'obj' ui as active and disable others. Calls from UI.
		obj.SetActive (true);
		for (int i = 0; i < UIs.Count; i++) {
			if (UIs [i] != obj) {
				UIs [i].SetActive (false);
			}
		}
	}

	public void Exit(){
		Application.Quit ();
	}
}
