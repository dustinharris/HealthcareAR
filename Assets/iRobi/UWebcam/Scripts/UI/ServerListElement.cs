using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class ServerListElement : MonoBehaviour {
	// This script contains code that set server's info into prefab.
	// In this script we control destroying of server's prefab.
	public float destroyTime;
	public Text ip;
	public NetworkManager netManager;
	public NetDiscover discover;

	[HideInInspector]
	public float time;

	void Start () {
		time = 0; // Set time to 0.
	}

	void Update () {
		time += Time.deltaTime; // Add deltaTime each frame.
		if (time > destroyTime) {
			Destroy(gameObject);
		}
	}

	public void ConnectClient(){ // Function that connect client to server. Calls from UI.
		NetDiscover.self.StartClientCorroutines (ip.text);
		mainUI.self.SetActiveUI (mainUI.self.ConnectedUI); // Change active UI.
	}

	void OnDestroy(){
		discover.ServersList.Remove(ip.text);
	}

	void OnDisable(){
		discover.ServersList.Remove(ip.text);
		Destroy(gameObject);
	}
}
