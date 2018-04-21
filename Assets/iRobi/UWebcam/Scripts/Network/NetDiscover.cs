using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

public class NetDiscover : MonoBehaviour {

	CustomNetworkDiscovery discovery; 
	NetworkManager manager;

	public static NetDiscover self;  // Static instance of NetDiscovery.

	public GameObject ServerPrefab;  // Prefab of the servers list element.
	public Transform panel;  // Panel of servers list.

	public Dictionary<string, ServerListElement> ServersList = new Dictionary<string, ServerListElement>();  // Contains list of available servers.

	public static bool disconnectByServer;  // Use to differ lost connection and close connection.

	public Transform Loader;

	// Text of connection status.
	public Text ConnectionUItext;  // Text of connection status.
	public Text ConnectionUIip;  // Ip address of connection status.


	void Start () {
		self = this; // Set instance.
		manager = GetComponent<NetworkManager> ();
		InvokeRepeating("Spin",0,0.1f);
	}

	void Update(){
		panel.GetComponent<GridLayoutGroup> ().cellSize = new Vector2 (panel.GetComponent<RectTransform>().rect.width, 60);  // Set GridLayout params.
	}

	public void StartClientCorroutines(string address){  // Client corroutine that calls to connect client to server.
		manager.networkAddress = address;  // Set address to NetworkManager.
		NetworkClient client = manager.StartClient ();  // Start client.
		StartCoroutine (NetDiscover.self.connectionCorroutine(client));  // Start client connect corroutine to check client's connection.
	}

	public void StartDiscovery(){  // Start NetworkDiscovery. Called from UI.
		discovery = gameObject.AddComponent<CustomNetworkDiscovery> ();

		//Set NetworkDiscovery params.
		discovery.showGUI = false;
		discovery.useNetworkManager = true;

		// Register handler to our custom NetworkDiscovery instance. Registred function will be called when NetworkDiscovery recieve broadcast.
		discovery.RegisterHandler (RecieveBroadcast); 

		discovery.Initialize (); // Init discovery.
		discovery.StartAsClient (); // Start to listen broadcasts.
	}

	public IEnumerator connectionCorroutine(NetworkClient client){  // Corroutine that called to check connection of client.
		int seconds = 0; // Seconds of timeout.
		while (true) {
			if(client.isConnected){
				// If client connected we print to connection status menu text and address of server.
				ConnectionUItext.text = "Connected to:"; // Info text.
				ConnectionUIip.text = client.connection.address;  // Address of server.
				Loader.gameObject.SetActive(false);
				StartCoroutine (checkConnection (client));  // Start corroutine to check connected client's status.
				break; // Leave corroutine if client is connected.
			}

			// If client can't connect about 'seconds' seconds we print:
			ConnectionUItext.text = "Try connect to:";  // Connection Info.
			ConnectionUIip.text = client.serverIp;  // Ip address of server that we trying to connect.
			Loader.gameObject.SetActive (true);

			seconds++; // Implement seconds.
			if(seconds > 10){
				// If client can't connect we stop this corroutine and redirect to servers discovery menu.
				mainUI.self.SetActiveUI(panel.parent.gameObject);
				break;
			}

			yield return new WaitForSeconds(1); // Wait for 1 second and them try again.
		}
		yield return null;
	}

	void Spin()
	{
		Loader.Rotate(new Vector3(0,0,-45));
	}


	
	public IEnumerator checkConnection(NetworkClient client){  // Corroutine that called when client connected and we need to monitor his connection.
		string address = "";
		while (true) {
			if(!client.isConnected){
				// if clinet isn't connected,
				StopClient(); // stop client,
				if(!disconnectByServer) // and if server doesnt't close connect,
					StartClientCorroutines(address); // try again.
				else{ 
					// If server close connection:
					ConnectionUItext.text = "Server Close Connection"; // Print info text to the connection status panel.
					ConnectionUIip.text = "";
				}
				break; // Leave corroutine.
			}
			address = client.connection.address; // Set clients server address.
			yield return null;
		}
	}

	public void Stop(){  // Stop discovery function. Called from UI.
		StartCoroutine (StopDiscoveryCorroutine ()); // Start corroutine that stop discovery.

		ServersList.Clear (); // Clear servers list.
		DestroyImmediate (discovery); // Destroy NetworkDiscovery instance.
	}

	public void StopClient(){
		// Stop Client function, calls from 'checkConnection' if we have promblem with connection.
		StopAllCoroutines ();
		manager.StopClient ();
	}


	void RecieveBroadcast(string address, string data){ // Handler-function that we rigeter to our custom Network Discovery.
		var serverAdressRaw = address.Split(':'); // Convert raw IP to normal(Raw ip have 'ff::'-like prefix);
		var serverAdress = serverAdressRaw[serverAdressRaw.Length - 1]; // Get Last element of array returned by split.

		// We have 'AutoDestroy' script that Destroy prefab of server if server doesn't send broadcast on 3 seconds.
		// By this function we zero timer of prefab.
		if (!ServersList.ContainsKey(serverAdress)) {
			//If we haven't this server on our servers list we instantiate new 'serverPrefab'.

			var serverListElement = (Instantiate (ServerPrefab) as GameObject).GetComponent<ServerListElement>();
			serverListElement.ip.text = serverAdress; // Set server address to prefab.
			serverListElement.discover = this;  // Set instance of discover to prefab.
			serverListElement.transform.parent = panel; // Set parent to prefab.
			serverListElement.transform.localScale = Vector3.one;
			ServersList.Add(serverAdress, serverListElement); // Add prefab to our serverPrefabs list.
		} else {
			//If we have this server on our servers list we zero his timer.
			ServersList[serverAdress].time = 0;
		}
	}

	private IEnumerator StopDiscoveryCorroutine(){ // Stop discovery corroutine.
		// We create this corroutine because we have problem with stop manager and discovery in one frame
		// and now we wait one frame and them stop listen broadcasts.
		yield return new WaitForEndOfFrame ();
		discovery.StopBroadcast ();
	}

	void OnDisabled(){
		Stop ();
	}
}
