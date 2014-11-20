﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;


public class PanelBeforeMission : MonoBehaviour {

	public GameObject TextInfo, BuildingsList, TextMissionQuery;
	public GameObject PanelMinigame, PanelMainMenu;

	private Mission Mission;

	public void ReturnToMainMenu() {
		PanelMainMenu.SetActive(true);
		gameObject.SetActive(false);
	}

	public void MissionBriefing(Mission m){
		Mission = m;

		if (m.FailureQueries.Count > 0) {
			AchievQuery failureQuery = m.FailureQueries[0];
			TextMissionQuery.GetComponent<Text>().text = "Mission: " + m.Name + "\n Destroy all buildings before " + failureQuery.ScoreType + " " + failureQuery.Sign.Text() + " " + failureQuery.Value;
		} else if (m.SuccessQueries.Count > 0) {
			AchievQuery successQuery = m.SuccessQueries[0];
			TextMissionQuery.GetComponent<Text>().text = "Win when " + successQuery.ScoreType + " " + successQuery.Sign.Text() + " " + successQuery.Value;
		}

		TextInfo.GetComponent<Text>().text = m.BeforeMissionText == "" ? m.BeforeMissionBuildings[1].Description : m.BeforeMissionText;
		
		List<List<BuildingTemplate>> b = new List<List<BuildingTemplate>>();
		b.Add(m.BeforeMissionBuildings);
		BuildingsList.GetComponent<ScrollableList>().Build(b, null);
	}

	public void StartMinigameListener(){
		StartCoroutine( StartMinigame() );
	}

	private IEnumerator StartMinigame() {
		PanelMinigame.SetActive(true);
		PanelMinigame.GetComponent<PanelMinigame>().PrepareGame(Mission);
		yield return null;
		gameObject.SetActive(false);
	}

}
