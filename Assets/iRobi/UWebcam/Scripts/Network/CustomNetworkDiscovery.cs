using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CustomNetworkDiscovery : NetworkDiscovery {

	public delegate void Handler(string fromAddress, string data); // Handler function's delegate.

	List<Handler> Handlers = new List<Handler>();  // List of handlers.

	public override void OnReceivedBroadcast (string fromAddress, string data)  // Overrided function.
	{	
		base.OnReceivedBroadcast (fromAddress, data); 

		// Call all handlers after base function done.
		foreach (var func in Handlers) {
			func(fromAddress, data);
		}
	}

		

	public void RegisterHandler(Handler Func){
		// Public function for register new handlers.
		Handlers.Add (Func);
	}
}
