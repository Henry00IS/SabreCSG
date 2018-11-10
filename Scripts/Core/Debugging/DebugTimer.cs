#if UNITY_EDITOR || RUNTIME_CSG
using UnityEngine;
using System.Collections;
using System;

namespace Sabresaurus.SabreCSG
{
	internal static class DebugTimer
	{
#if SABRE_CSG_DEBUG
		static DateTime startTime;
		static DateTime lastEvent = DateTime.MinValue;
#endif

		[System.Diagnostics.Conditional("SABRE_CSG_DEBUG")]
	    public static void StartTimer()
		{
#if SABRE_CSG_DEBUG
            startTime = DateTime.UtcNow;
			lastEvent =  DateTime.UtcNow;
	        Debug.Log("Started timer");
#endif
	    }

		[System.Diagnostics.Conditional("SABRE_CSG_DEBUG")]
	    public static void LogEvent(string message)
	    {
#if SABRE_CSG_DEBUG
            Debug.Log("Event " + (DateTime.UtcNow - startTime) + " " + (DateTime.UtcNow - lastEvent) + " - " + message);
	        lastEvent = DateTime.UtcNow;
#endif
	    }
	}
}
#endif