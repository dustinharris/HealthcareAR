using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SettingsUI : MonoBehaviour {
	// Script controls side-bar and configuring cams.
	public Text fpsText;
	public Text qualityText;

	public Slider fpsSlider;
	public Slider qualitySlider;

	public GameObject devicePrefab; // Prefab of the toggle.
	public GameObject deviceScrollView; // Panel that contains ToggleGroup.

	private int[] fpsChoice = new int[]{10,20,30}; // Available fps choces.
	private GridLayoutGroup grid;

	private Dictionary<int, string> devicesTmp; // Selected user's devices.
	private Dictionary<GameObject, int> Toggles = new Dictionary<GameObject, int>();

	static public SettingsUI self; // Static instance of this script.

	public NetworkUser selectedUser;

	public Image qualityImage;
	public bool isScreenSharing;

	public GameObject ScreenToggle;
	public Canvas canvas;

	private Color qualityDefaultColor;

	Animator QualityAnimator;

	void Awake(){
		grid = deviceScrollView.GetComponent<GridLayoutGroup> ();
		self = this;
		qualityDefaultColor = qualityImage.color;
		QualityAnimator = qualityImage.GetComponent<Animator> ();
	}


	public void ChangeFPS(Slider slider){ // Function that calls from Slider.
		// Change label of slider
		this.fpsText.text = "" + fpsChoice [(int)slider.value];
	}

	public void ChangeQuality(Slider slider){ // Function that calls from Slider.
		// Change label of slider
		if ((int)slider.value > 50) {
			QualityAnimator.SetBool ("Red", true);
			Debug.LogWarning("Please note that increasing quality can lead to network errors");
		}
		else
			QualityAnimator.SetBool("Red", false);
		this.qualityText.text = "" + (int)slider.value;
	}

	public void ResetSettings(){
		isScreenSharing = selectedUser.ScreenShare; 
		devicesTmp = selectedUser.devices;
		fpsText.text = selectedUser.Fps + "";
		fpsSlider.value = new List<int> (fpsChoice).IndexOf (selectedUser.Fps);
		qualityText.text = selectedUser.Quality + "";
		qualitySlider.value = selectedUser.Quality;

		OnDestroy();
		DrawDevices(selectedUser);
	}

	public void DoneSettings(){ // Calls from UI. Set setting on client.
		var user = Connections.selectedUsers [Connections.selectedUsers.Count - 1];
		user.RpcSetScreenShare(isScreenSharing); // Send RPC to client about screenshare.
		int id = -1;

		foreach(var toggle in Toggles){ // Identify active toggle.
			if(toggle.Key.GetComponent<Toggle>().isOn)
				id = toggle.Value;
		}
		

		user.SendParams(id, int.Parse(fpsText.text), int.Parse(qualityText.text)); // Send RPC with client new params.
	}

	public void ScreenSharing(bool value){ // Calls from UI.
		isScreenSharing = value;
	}

	public void Click(){ // Calls from UI, when user click on client prefab.
		OnDestroy(); // Clear toggles.
		selectedUser = Connections.selectedUsers [Connections.selectedUsers.Count - 1]; // Get selected user.

		ScreenToggle.SetActive(selectedUser.isSupportScreenShare);

		// Get user's params.
		isScreenSharing = selectedUser.ScreenShare; 
		devicesTmp = selectedUser.devices;
		fpsText.text = selectedUser.Fps + "";
		fpsSlider.value = new List<int> (fpsChoice).IndexOf (selectedUser.Fps);
		qualityText.text = selectedUser.Quality + "";
		qualitySlider.value = selectedUser.Quality;

		// Set scroll view.
		deviceScrollView.GetComponent<RectTransform> ().sizeDelta = new Vector2 (0, (devicesTmp.Count+1) * (devicePrefab.GetComponent<RectTransform>().rect.height+grid.spacing.y));
		
		DrawDevices (selectedUser); // Draw selected user's device 
	}

	private void DrawDevices(NetworkUser user){ // Function that draw selected user's device toggles.
		foreach (var device in devicesTmp) { 
			var prefab = Instantiate(devicePrefab); // Instantiate prefab.
			Transform transformPrefab = prefab.transform;
			transformPrefab.localScale =  canvas.transform.localScale;
			transformPrefab.SetParent(deviceScrollView.transform); // Set parent.
			prefab.GetComponent<Toggle>().group = deviceScrollView.GetComponent<ToggleGroup>(); // Set ToggleGroup.
			if(device.Key == user.Device && !user.ScreenShare)
				// Set device toggle depends of user's device.
				prefab.GetComponent<Toggle>().isOn = true;

			prefab.GetComponentInChildren<Text>().text = device.Value; // Set device name/
			Toggles.Add(prefab, device.Key); // Add this toggle into list.
		}
	}

	void OnDestroy(){
		Toggles.Clear ();
		var devTransform = deviceScrollView.transform;
		for (int i = 1; i< devTransform.childCount; i++) {
			Destroy(devTransform.GetChild(i).gameObject);
		}
	}


}
