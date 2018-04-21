using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class fullScreen : MonoBehaviour {

	public static RawImage fullScreenImage;
	public static fullScreen self;

	public static NetworkUser currently;

	void Awake(){
		fullScreenImage = GetComponent<RawImage>();
		self = this;
	}

	

	void Update () {
		GetComponent<RawImage>().texture = fullScreenImage.texture;
	}

	public void CloseFullScreen(){
		currently.isFullScreen = false;
		currently.RpcSetFullScreen(false);
		Connections.self.container.gameObject.SetActive(true);
		gameObject.SetActive(false);
		currently = null;
	}
}
