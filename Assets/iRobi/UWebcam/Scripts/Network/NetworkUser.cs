using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;


public class NetworkUser : NetworkBehaviour {

	Texture2D mainTexure;  // Texture2D used to save texture from WebCamService.
	RawImage rawImage;  // RawImage used to set mainTexture to clients prefab on server.
	RawImage fullScreenImage;  // RawImage used to set mainTexture to clients prefab on server.



	// NetworkUser fields.
	private int Width=640;  
	private int Height=480;
	public int Fps=20;
	public int Quality=50;
	public int Device=0;  // Currently used device.

	public bool isSupportScreenShare;
	public bool ScreenShare;  // Used to change screen share and camera mode.
	public Dictionary<int, string> devices = new Dictionary<int, string>();

	public GameObject clientPrefab;  // Prefab of the client panel.
	public GameObject client; // Concrete GameObject of NetworkUser(Instantiated 'clientPrefab').

	Coroutine ClientCorroutine;  // Client update corroutine. Send the texture from WebCamService to the server.
	Coroutine ClientAudioCorroutine;  // Client update corroutine. Send audio from WebCamService to the server.

	public bool isFullScreen;

	// Server fields.
	byte[] ReceivedBytes;  // Contains camera texture bytes from client.
	

	// Audio playing fields.
	AudioClip clip;
	int FREQUENCY = 8000;
	AudioSource audioSource;
	float playbackTime;



	void Start () {
		fullScreenImage = fullScreen.fullScreenImage;

		audioSource = GetComponent<AudioSource>();

		mainTexure = new Texture2D (8, 8);
		client = Connections.self.DrawPrefab (clientPrefab);
		Connections.users.Add (client, this);  // Add user to static users dictionary, that contains NetworkUser and Panel prefab.
		rawImage = client.GetComponent<RawImage> ();

		if(GetComponent<NetworkIdentity>().isServer)
			RpcGetDevices ();  // Rpc function that send cliend devices to server.
		else if(GetComponent<NetworkIdentity>().isLocalPlayer)
			ClientStart ();  // Start client functions.


		if(WebCamService.isSupportScreenShare){
			if(GetComponent<NetworkIdentity>().isClient)
				CmdSetScreenShareSupportOnServer();
			else
				RpcSetScreenShareSupportOnClient();
		}
		
		playbackTime = 0;
	}


	[ClientRpc]
	void RpcSetScreenShareSupportOnClient(){
		isSupportScreenShare = true;
	}

	[Command]
	void CmdSetScreenShareSupportOnServer(){
		isSupportScreenShare = true;
	}


	[ClientCallback]
	void ClientStart(){  // Client function, that starts clinet corroutines.
		ClientCorroutine = StartCoroutine (ClientUpdateCorroutine());  // Start client update corroutine that send camera texture to the server.
		ClientAudioCorroutine = StartCoroutine(ClientAudioUpdateCorroutine()); // Start client update corroutine that send audio to the server.
	}


	[ClientRpc]
	void RpcGetDevices(){  // Rpc function that send cliend devices to server.
		for (int i=0; i < WebCamTexture.devices.Length; i++) {
			CmdSendDevice(i, WebCamTexture.devices[i].name);  // Call [Command] function to send each device to server.
		}
	}

	[Command]
	void CmdSendDevice(int id, string name){  // Command-function that add device from client to server's list.
		this.devices.Add (id, name);  // Add divece to the 'device' dictionary.
	}


	[ClientCallback]
	IEnumerator ClientUpdateCorroutine () {  //  Client Update corroutine. Send Camera or Screen picture to server by [Command]-like functions.

		while (true) {
			if (ScreenShare) { 
				// Use 'Cmd_SendBytes' to send screen share bytes from WebCamService to server.
				Rect screen = new Rect (0, 0, Screen.currentResolution.width, Screen.currentResolution.height); // Initialize Rect of screen. You can set your rect of screen that you need to send.
				#if UNITY_EDITOR
				try{
					Cmd_SendBytes (WebCamService.ScreenTexture (screen, Mathf.Clamp(Quality-30,0, 20))); 
				}catch{

				}

				#else
				try{
					Cmd_SendBytes (WebCamService.ScreenTexture (screen, Quality)); 
				}catch{

				}
				#endif
				yield return new WaitForSeconds(.5f);
			} else { 
				// Use 'Cmd_SendBytes' to send camera bytes from WebCamService to server.
				// Send different resolution depending on connections count.
				try{
					if (Connections.count > 8) { 
						Cmd_SendBytes (WebCamService.webCamTexture (Device, 160, 120, Fps, Quality));
					} else if (Connections.count > 1) {
						Cmd_SendBytes (WebCamService.webCamTexture (Device, 320, 240, Fps, Quality));
					} else {
						Cmd_SendBytes (WebCamService.webCamTexture (Device, Width, Height, Fps, Quality));
					}
				}catch{

				}
				yield return new WaitForSeconds(.5f);
			}

		}

	}

	[ClientCallback]
	IEnumerator ClientAudioUpdateCorroutine () {  
		// Client Audio corroutine. Send audio bytes to server.

		while (true) {
			if(isFullScreen){
				try{
					Cmd_SendAudioBytes(WebCamService.GetMicrophoneData(""));
				}catch{

				}
			}
			yield return new WaitForSeconds(.5f);
		}
	}


	float[] ToFloatArray(byte[] byteArray) {
		int len = byteArray.Length / 4;
		float[] floatArray = new float[len];
		for (int i = 0; i < byteArray.Length; i+=4) {
			floatArray[i/4] = System.BitConverter.ToSingle(byteArray, i);
		}
		return floatArray;
	}

	[ClientRpc]
	public void RpcSetScreenShare(bool value){  
		// Rpc function that called from server to change ScreenShare mode.

		ScreenShare = value;
	}

	[ClientRpc]
	public void RpcSetFullScreen(bool value){  
		// Rpc function that called from server to change full screen mode.
		
		isFullScreen = value;
	}






	[ServerCallback]
	void Update () {  
		// MonoBehaivor's Update fucntion that runs only on server.
		if(ReceivedBytes!=null)
			if(fullScreen.currently == null || fullScreen.currently == this)
				mainTexure.LoadImage (ReceivedBytes);  // Convert bytes to image.
		if(isFullScreen)
			fullScreen.fullScreenImage.texture = mainTexure;  // Set texture to client panel at server.
		else
			rawImage.texture = mainTexure;  // Set texture to client panel at server.
	}






	[Command]
	void Cmd_SendBytes(byte[] bytes)
	{   
		// Function that called to send bytes from client to server.
		ReceivedBytes = bytes; 

	}

	[Command]
	void Cmd_SendAudioBytes(byte[] bytes){ 
		// Function that called to send audio bytes from client to server.
		WebCamService.PlayMicrophoneData(bytes);
	}

	[ClientRpc]
	void Rpc_SetCameraParams(int device, int fps, int quality)
	{
		// Rpc function that used to set clients params from server.

		this.Device = device;
		this.Fps = fps;
		this.Quality = quality;

		CmdSyncParams (device, fps, quality);  // Synchronize client and server params.
	}

	[Command]
	void CmdSyncParams(int device, int fps, int quality){
		// Function that called to synchronize params at server.

		this.Device = device;
		this.Fps = fps;
		this.Quality = quality;
	}

	[Server]
	public void SendParams(int device, int fps, int quality)
	{
		// Server funtion that set clients params on server.
		this.Device = device;
		this.Fps = fps;
		this.Quality = quality;
		Rpc_SetCameraParams (device, fps, quality);  // Call rpc funtion to set this params on client.
	}

	void OnDestroy(){
		try{
		Connections.self.fullScreenPanel.SetActive(false);
		Connections.self.container.gameObject.SetActive(true);
		StopClientCorroutine();  // Stop client's corroutine
		Connections.users.Remove (client);  // Remove client from static clients list.
		Connections.selectedUsers.Clear ();  // Remove client from selected list.
			Connections.self.Disconnect (client);  // Remove client's panel.
		}
		catch{
		}
	}

	[ClientCallback]
	void StopClientCorroutine(){
		StopCoroutine (ClientCorroutine);
	}
}







