﻿using UnityEngine;
using System.Collections;

public class PanelButtons : MonoBehaviour {

	public GameObject ButtonTop, ButtonTopText, ButtonBottom, ButtonBottomText ;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	internal GameObject CopyMeIn(GameObject parent) {
		GameObject go = Instantiate(gameObject) as GameObject;
		go.transform.parent = parent.transform;
		go.GetComponent<RectTransform>().offsetMin = new Vector2(); //we want it to fit into anchors exactly as they were set in editor
		go.GetComponent<RectTransform>().offsetMax = new Vector2(); //we want it to fit into anchors exactly as they were set in editor
		return go;
	}
}
