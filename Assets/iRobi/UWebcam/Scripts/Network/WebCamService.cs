using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Collections;
using System.Drawing;
using UnityEngine;
using System.IO;
using System;
using zlib;

public class WebCamService : MonoBehaviour {
	static WebCamService self;
	static public float timeOut=4f;
	static public int MicrophoneFrequency = 8000;
	static private float camTime;
	static private float screenTime;
	static private float micTime;
	static private byte[] loadingTextureBytes;
	static private byte[] NoCameraFoundTextureBytes;
	static private Texture2D textureData;
	private static WebCamTexture WebCamTex;
	private static bool isStarted=false;
	private static byte[] camBytes;
	private static byte[] screenBytes;
	private static byte[] micBytes = new byte[0];
	private static int _quality=50;
	private static int paramId;
	private static ScreenThread screenThread;
	private static AudioClip mic_clip;
	private static AudioSource audiPlayer;

	static public bool isSupportScreenShare
	{
		get { 
			return Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor; 
		}
	}

	void Start () {
	}

	void Update () {
		camTime+=Time.deltaTime;
		screenTime+=Time.deltaTime;
		micTime+=Time.deltaTime;
		if(WebCamTex!=null)
		if (camTime > timeOut) {
//			DestroyWebCamService ();
			StopWebCam();
		} else if(isStarted) {
			if (WebCamTex.isPlaying) {
				textureData.SetPixels32 (WebCamTex.GetPixels32 ());
				camBytes = textureData.EncodeToJPG (_quality);
			}
			else
			camBytes = textureData.EncodeToJPG ();
		}

		if(screenThread!=null)
		if (screenTime > timeOut) {
			StopScreenShare ();
		} else {
			screenBytes = screenThread.screenBytes;
		}

		if (micTime > timeOut) 
			StopMicrophone ();

		if (camTime > timeOut && screenTime > timeOut&&micTime>timeOut) {
			DestroyWebCamService ();
		}

	}

	static public byte[] webCamTexture()
	{
		if (WebCamTexture.devices.Length == 0) {
			if (NoCameraFoundTextureBytes == null)
				NoCameraFoundTextureBytes = (Resources.Load ("NoCameraFound") as Texture2D).EncodeToJPG ();
			return NoCameraFoundTextureBytes;
		}
		if (self == null)
			CreateInstance ();
		if (!WebCamTex)
			CreateWebCamTextureWithParams (0, 640, 480, 15, 70);
		camTime = 0;
		return camBytes;
	}


	static public byte[] webCamTexture(int device, int width,int height, int fps, int quality)
	{
		if (WebCamTexture.devices.Length == 0) {
			if (NoCameraFoundTextureBytes == null)
				NoCameraFoundTextureBytes = (Resources.Load ("NoCameraFound") as Texture2D).EncodeToJPG ();
			return NoCameraFoundTextureBytes;
		}
		int summary = device + width + height + fps + quality;
		if (self == null)
			CreateInstance ();
		if (!WebCamTex || summary != paramId) {
			CreateWebCamTextureWithParams (device, width, height, fps, quality);
			paramId = summary;
		}
		camTime = 0;
		return camBytes;
	}

	static public byte[] ScreenTexture()
	{
		
		return ScreenTexture(new Rect(0,0,Screen.currentResolution.width,Screen.currentResolution.height),70);
	}

	static public byte[] ScreenTexture(Rect rect, int quality)
	{
		if(!isSupportScreenShare)
		{
			System.Exception ex = new System.Exception ("Screen share is not supported on this platform");
			throw ex;
//			return Texture2D.blackTexture.EncodeToJPG();
		}
		if (screenThread == null) {
			screenThread = new ScreenThread ();
			screenThread.Start ();
		}
		screenThread.quality = quality;
		screenThread.rect = rect;

		if (self == null)
			CreateInstance ();

		screenTime = 0;
		if (screenBytes==null)
			return loadingTextureBytes;
		else
		return screenBytes;
	}

	static public byte[] GetMicrophoneData()
	{
		return GetMicrophoneData(0);
	}

	static public byte[] GetMicrophoneData(int device)
	{
		return GetMicrophoneData (Microphone.devices [device]);
	}


	static private int lastSample;

	static public byte[] GetMicrophoneData(string device)
	{
		if (!mic_clip) {
			mic_clip = Microphone.Start (device, true, 100, MicrophoneFrequency);
		}
		if (self == null)
			CreateInstance ();
		micTime = 0;

		int pos = Microphone.GetPosition (device);
		int diff = pos - lastSample;

		if (diff > 0) {
			float[] samples = new float[diff * mic_clip.channels];
			mic_clip.GetData (samples, lastSample);
			micBytes = ToByteArray (samples);
			micBytes = CompressData (micBytes);
		}

		lastSample = pos;

		return micBytes;
	}

	static public void PlayMicrophoneData(byte[] data)
	{
		if (self == null)
			CreateInstance ();
		if (data.Length > 0) {

			if (!audiPlayer) {
				audiPlayer = self.gameObject.AddComponent<AudioSource> ();
				audiPlayer.spatialBlend = 0;
			} 
			data = DecompressData (data);
			float[] SpecData = ToFloatArray (data);
			audiPlayer.clip = AudioClip.Create ("Microphone AudioClip", SpecData.Length, 1, MicrophoneFrequency, false);
			audiPlayer.clip.SetData (SpecData, 0);
			if (!audiPlayer.isPlaying)
				audiPlayer.Play ();
		}
		micTime = 0;
	}


	static private void CreateWebCamTextureWithParams(int device, int width,int height, int fps, int quality)
	{
		if(WebCamTex)
		WebCamTex.Stop ();
		Destroy (WebCamTex);
		WebCamTex = null;
		WebCamTex = new WebCamTexture (WebCamTexture.devices [Mathf.Clamp(device,0,WebCamTexture.devices.Length-1)].name, width, height, fps);
		WebCamTex.Play ();
		isStarted = true;
		textureData.Resize(WebCamTex.width,WebCamTex.height);
		_quality = quality;
	}

	private static void CreateInstance()
	{
		GameObject webcamserviceInstance = new GameObject ("WebCamService");
		self = webcamserviceInstance.AddComponent<WebCamService> ();
		if(loadingTextureBytes==null)
			loadingTextureBytes = (Resources.Load ("Loading") as Texture2D).EncodeToJPG ();
		if(textureData==null)
			textureData = new Texture2D(8,8);
		camBytes = loadingTextureBytes;
	}

	void DestroyWebCamService()
	{
		Destroy (self.gameObject);
		isStarted = false;
		if(WebCamTex)
		WebCamTex.Stop ();
		if (screenThread!=null) {
			screenThread.Abort ();
			screenThread = null;
		}
		Destroy (WebCamTex);
		camBytes = loadingTextureBytes;
		screenBytes = loadingTextureBytes;
		StopMicrophone ();
	}

	void StopWebCam()
	{
		isStarted = false;
		WebCamTex.Stop ();
		Destroy (WebCamTex);
	}

	void StopScreenShare()
	{
		screenThread.Abort ();
		screenThread = null;
	}

	void StopMicrophone()
	{
		if (mic_clip != null)
			Destroy (mic_clip);
		if(audiPlayer!=null)
			Destroy (audiPlayer);
		micBytes = new byte[0];
	}

	void OnDestroy()
	{
		if (screenThread != null) {
			screenThread.Abort ();
			screenThread = null;
		}
	}

	private static byte[] ToByteArray(float[] floatArray) {
		int len = floatArray.Length * 4;
		byte[] byteArray = new byte[len];
		int pos = 0;
		foreach (float f in floatArray) {
			byte[] data = System.BitConverter.GetBytes(f);
			System.Array.Copy(data, 0, byteArray, pos, 4);
			pos += 4;
		}
		return byteArray;
	}

	private static float[] ToFloatArray(byte[] byteArray) {
		int len = byteArray.Length / 4;
		float[] floatArray = new float[len];
		for (int i = 0; i < byteArray.Length; i+=4) {
			floatArray[i/4] = System.BitConverter.ToSingle(byteArray, i);
		}
		return floatArray;
	}

	private static byte[] CompressData(byte[] inData)
	{
		using (MemoryStream outMemoryStream = new MemoryStream())
		using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream, zlibConst.Z_DEFAULT_COMPRESSION))
		using (Stream inMemoryStream = new MemoryStream(inData))
		{
			CopyStream(inMemoryStream, outZStream);
			outZStream.finish();
			return outMemoryStream.ToArray();
		}
	}

	private static byte[] DecompressData(byte[] inData)
	{
		using (MemoryStream outMemoryStream = new MemoryStream())
		using (ZOutputStream outZStream = new ZOutputStream(outMemoryStream))
		using (Stream inMemoryStream = new MemoryStream(inData))
		{
			CopyStream(inMemoryStream, outZStream);
			outZStream.finish();
			return outMemoryStream.ToArray();
		}
	}

	private static void CopyStream(System.IO.Stream input, System.IO.Stream output)
	{
		byte[] buffer = new byte[2000];
		int len;
		while ((len = input.Read(buffer, 0, 2000)) > 0)
		{
			output.Write(buffer, 0, len);
		}
		output.Flush();
	} 
}

public class ScreenThread : W_Thread {
	private Bitmap screenshot;
	public byte[] screenBytes;
	public int quality;
	public Rect rect;
	protected override void ThreadFunction()
	{
		// Do your threaded task. DON'T use the Unity API here
		try{
			DetailCap();
		}
		catch{}

	}
	void DetailCap()
	{
		Size screenSz = new Size((int)rect.width,(int)rect.height);
		screenshot = new Bitmap(screenSz.Width, screenSz.Height);
		System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(screenshot);
		gr.CopyFromScreen(new Point((int)rect.x,(int)rect.y), Point.Empty, screenSz);

		int W = Lerp ((int)rect.width / 4, (int)rect.width, quality);
		int H = Lerp ((int)rect.height / 4, (int)rect.height, quality);

		Bitmap b2 = new Bitmap(W, H);
		System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(b2);
		g.DrawImage(screenshot, new RectangleF(0, 0, b2.Width, b2.Height));

		MemoryStream ms = new MemoryStream();

		EncoderParameters encoderParameters = new EncoderParameters(1);
		encoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 40L);

		b2.Save (ms,GetEncoder(ImageFormat.Jpeg),encoderParameters);

		screenBytes = ms.ToArray ();
	}

	int Lerp(int a, int b, int t)
	{
		int diff = b - a;
		t = Mathf.Clamp (t, 0, 100);
		return a + diff * t / 100;
	}

	private ImageCodecInfo GetEncoder(ImageFormat format)
	{

		ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

		foreach (ImageCodecInfo codec in codecs)
		{
			if (codec.FormatID == format.Guid)
			{
				return codec;
			}
		}
		return null;
	}
}

public class W_Thread
{

	private System.Threading.Thread m_Thread = null;
	public bool isRun=false;

	public virtual void Start()
	{
		m_Thread = new System.Threading.Thread(Run);
		m_Thread.Start();
		isRun = true;
	}
	public virtual void Abort()
	{
		isRun = false;
		m_Thread.Abort();
	}

	protected virtual void ThreadFunction() { }

	private void Run()
	{
		ThreadFunction();
		if(isRun)
			Run (); 
	}


}
