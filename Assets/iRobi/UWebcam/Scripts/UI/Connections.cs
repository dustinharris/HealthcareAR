using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Connections : NetworkBehaviour {

	// This script contains Server part of application.


	public Transform container;  // Transform of the panel which we add prefabs.
	public GameObject sidePanel;  // Side panel.

	public static Dictionary<GameObject, NetworkUser> users = new Dictionary<GameObject, NetworkUser>();  // Currently connected users.
	public static List<NetworkUser> selectedUsers = new List<NetworkUser>();  // List of selected users. We use last.

	public static int count = 0;  // Count of connected users.
	public static Connections self;  // Static instance of this script.

	private GridLayoutGroup grid;
	private NetworkManager netManager; 
	private CustomNetworkDiscovery discovery;

	public GameObject fullScreenPanel;
	public GameObject prefab;


	void Start () {
		self = this;
		grid = container.GetComponent<GridLayoutGroup> ();
		netManager = GetComponent<NetworkManager> ();
	}
	

	void Update () {

		// If we have selected user, we activate our side panel.
		if (selectedUsers.Count > 0)
			sidePanel.SetActive (true);
		else
			sidePanel.SetActive (false);

		CalculateGrid();
	}

	public void StartServer(){ // Function that start send broadcast and start as server. Calls from UI.
		discovery = gameObject.AddComponent<CustomNetworkDiscovery>();
		discovery.showGUI = false;
		NetDiscover.disconnectByServer = false;

		discovery.Initialize ();  // Init discovery.
		discovery.StartAsServer ();  // Start broadcast.
		netManager.StartServer ();  // Start as server.
	}


	public void StopServer(){  // Function that stop broadcast and stop server.

		// When we stop server we destroy all client prefabs.
		foreach (var user in users) {
			Destroy(user.Value.gameObject);
		}
		netManager.StopServer(); // Stop as server.
		try{
		RpcServerStop ();  // Send to client info.
		}
		catch{
		}
		StartCoroutine (StopDiscoveryCorroutine());

		Destroy (discovery);
	}

	[ClientRpc]
	void RpcServerStop(){ // Rpc function that used to close connection by server.
		NetDiscover.disconnectByServer = true;
	}


	public GameObject DrawPrefab(GameObject prefab){ // Function that user to draw clients prefab on connect.
		GameObject client = Instantiate (prefab) as GameObject;
		client.transform.SetParent (container);
		client.transform.localScale = Vector3.one * 0.95f;
		count++;
		CalculateGrid ();  // Recalculate grid.
		return client;
	}

	public void Disconnect(GameObject client){ // Function that calls when client disconect from server.
		Destroy (client);
		count--;
		CalculateGrid (); // Recalculate grid.
	}

	void CalculateGrid(){

		// Function that calculate grid depends on connections count.

		float width = grid.GetComponent<RectTransform> ().rect.width;
		float height = grid.GetComponent<RectTransform> ().rect.height;

		if (count <= 2) {
			grid.cellSize = new Vector2 (width / count - grid.spacing.x, height/count - grid.spacing.y);
		} else if (count <= 4) {
			grid.cellSize = new Vector2 (width / 2 - grid.spacing.x, height / 2 - grid.spacing.y);
		} else if (count <= 6) {
			grid.cellSize = new Vector2 (width / 3 - grid.spacing.x, height / 3 - grid.spacing.y);
		} else if (count <= 9) {
			grid.cellSize = new Vector2 (width / 3 - grid.spacing.x, height / 3 - grid.spacing.y);
		} else if (count <= 12) {
			grid.cellSize = new Vector2 (width / 4 - grid.spacing.x, height / 4 - grid.spacing.y);
		} 

	}

	private IEnumerator StopDiscoveryCorroutine(){// Stop discovery corroutine.
		// We create this corroutine because we have problem with stop server and discovery in one frame
		// and now we wait one frame and them stop send broadcasts.
		yield return new WaitForEndOfFrame ();
		discovery.StopBroadcast ();
	}

	void OnDisabled(){
		StopServer ();
	}



}
