﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

class WebConnector {

	private static string PrivateKey = "ion32gew9va09)HJ)(N#G()VENIDOSK><P[lp{>:MOF!RFWQ";

	public static string GetDeviceId() {
		return SystemInfo.deviceUniqueIdentifier;
	}

	private static WWW CreateWWW(string method, WWWForm form) {
		form.AddField("DeviceId", GetDeviceId());
		String data = System.Text.Encoding.UTF8.GetString(form.data);
		MD5 md5 = MD5.Create();
		string builtString = data+"&key="+PrivateKey;
		string hash = GetMd5Hash(md5, builtString);
		form.AddField("Signature", hash);
		Debug.Log("Sending post to " + method);
		return new WWW(Config.Server + Config.IndexPath + method, form);
	}

	private static string GetMd5Hash(MD5 md5Hash, string input) {

		// Convert the input string to a byte array and compute the hash. 
		byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

		// Create a new Stringbuilder to collect the bytes 
		// and create a string.
		StringBuilder sBuilder = new StringBuilder();

		// Loop through each byte of the hashed data  
		// and format each one as a hexadecimal string. 
		for (int i = 0; i < data.Length; i++) {
			sBuilder.Append(data[i].ToString("x2"));
		}

		// Return the hexadecimal string. 
		return sBuilder.ToString();
	}

	internal static WWW SendMissionAccomplished(MissionType MissionType, string MissionName, int Round, MissionStatus ms, Dictionary<ScoreType, Result> actualResults, bool getScores) {
		int interventions = (int)actualResults[ScoreType.Interventions].Value;
		float time = actualResults[ScoreType.Time].Value;
		WWWForm form = new WWWForm();
		form.AddField("MissionType", MissionType.ToString());
		form.AddField("MissionName", MissionName);
		form.AddField("MissionStatus", ms.ToString());
		form.AddField("Interventions", interventions);
		form.AddField("Time", ""+ (int)( time*1000) );
		form.AddField("Round", Round);
		form.AddField("GetScores", getScores ? 1 : 0);
		WWW www = CreateWWW("/save", form);
		return www;
	}

	internal static WWW GetResults(Mission m, MissionStatus ms) {
		WWWForm form = new WWWForm();
		form.AddField("MissionType", m.MissionType.ToString());
		form.AddField("MissionName", m.Name);
		form.AddField("MissionStatus", ms.ToString());
		return CreateWWW("/getResults", form);
	}

	internal static WWW LoadMission(MissionType mt, bool toRepeat) {
		WWWForm form = new WWWForm();
		form.AddField("MissionType", mt.ToString());
		form.AddField("ToRepeat", toRepeat ? 1 : 0);
		return CreateWWW("/load", form);
	}

	internal static WWW ChangeName(string name) {
		WWWForm form = new WWWForm();
		form.AddField("Name", name);
		return CreateWWW("/changeName", form);
	}

	internal static WWW RestartLevels() {
		WWWForm form = new WWWForm();
		return CreateWWW("/restartLevels", form);
	}

	internal static WWW GetInitialData() {
		WWWForm form = new WWWForm();
		return CreateWWW("/getInitialData", form);
	}
}
