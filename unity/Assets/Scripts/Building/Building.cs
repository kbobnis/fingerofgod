using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using System.ComponentModel;
using System;

public delegate bool GoQuestion(Building go);

public class Building : MonoBehaviour{

	private Dictionary<Element, BuildingStatus> Statuses = new Dictionary<Element, BuildingStatus>();
	private BuildingTemplate Template;
	private Dictionary<Element, GoQuestion> FillRequirement = new Dictionary<Element, GoQuestion>();

	private int StartingPopulation, _Population, LastCheckedPopulation;
	private float _Health = 1f;
	private Vector3 startingPos;
	private Vector2 OriginalOffsetMin, OriginalOffsetMax;
	private bool UpdateHeightNeeded = true;

	public Dictionary<Side, Building> Neighbours = new Dictionary<Side, Building>();
	public List<Listener<ScoreType, float>> Listeners;

 
	public GameObject GameObjectBuilding, GameObjectFire, GameObjectWaterLevel, GameObjectSmokeAfterFire, GameObjectElectricity, GameObjectCrush, GameObjectHealthBar, GameObjectDie;

	private static Building GetNeighbour(Building b, Side s) {
		if (s == Side.Center) {
			return b;
		}
		Building tmp2 = null;
		if (b.Neighbours.ContainsKey(s)) {
			tmp2 = b.Neighbours[s];
		}
		return tmp2;
	}

	private static void TreatNeighboursWith(Building b, Element e, float startingValue = 1, GoQuestion goq = null) {
		if (startingValue > 1) {
			startingValue = 1;
		}
		if (startingValue <= 0 || b == null) {
			return; //we don't want to waste time for dead startingValue
		}

		Building left = GetNeighbour(b, Side.Left);
		if (left != null && (goq == null || goq(left))) {
			left.GetComponent<Building>().TreatWith(e, startingValue, false);
		}
		Building right = GetNeighbour(b, Side.Right);
		if (right != null && (goq == null || goq(right))) {
			right.TreatWith(e, startingValue, false);
		}

		Building up = GetNeighbour(b, Side.Up);
		if (up != null && (goq == null || goq(up))) {
			up.TreatWith(e, startingValue, false);
		}

		Building down = GetNeighbour(b, Side.Down);
		if (down != null && (goq == null || goq(down))) {
			down.TreatWith(e, startingValue, false);
		}
	}

	public int PopulationDelta{
		get {
			int delta = _Population - LastCheckedPopulation;
			LastCheckedPopulation = _Population;
			return delta;
		}
	}

	public int Population {
		get { return _Population;  }
	}

	public float Health{
		get { return _Health; }
		set {
			bool wasDead = _Health <= 0;
			_Health = value; 
			if (_Health < 0){
				_Health = 0;
			}
			InformListeners();
			if (!wasDead && _Health == 0){
				UpdateHeightNeeded = true;
				Die();
			}
		}
	}

	private void Die(){
		//int middleRow = 
		Statuses[Element.Die].Add(1, false);

		TreatYourselfWithYourPower();
		
		foreach (Element e in Statuses.Keys.ToList()) {
			if (Statuses[e].Value > 0 && Template.Stats[StatType.ContaminateDelta][e] > 0) {
				TreatNeighboursWith(this, e, Statuses[e].Value + Template.Stats[StatType.ContaminateDelta][e], (Building b) => { return b.Health > 0; });
			}
		}
	}

	private void TreatYourselfWithYourPower() {
		foreach (Element e in Template.Stats[StatType.AfterDeath].Keys.ToList()) {
			if (Template.Stats[StatType.AfterDeath][e] > 0) {
				TreatWith(e, Template.Stats[StatType.AfterDeath][e], true);
			}
		}
	}

	private void ExtinguishFireCheck(){
		if (Statuses[Element.Water].Value > 0 && Statuses[Element.Fire].Value > 0) {
			Statuses[Element.Fire].StopNow();
			Statuses[Element.SmokeAfterFire].Add(1, false);
		}
	}

	private void DryElectricityCheck() {
		if (Statuses[Element.Electricity].Value > 0 && Statuses[Element.Water].Value <= 0 && Health > 0) {
			Statuses[Element.Fire].Add(1, false);
		}
	}

	void FixedUpdate(){

		if (GetComponent<UpdateChecker>() != null) {
			GetComponent<UpdateChecker>().UpdateIsNow();
		}
		//check if prepared already
		if (Statuses.Count == 0) {
			return;
		}

		ExtinguishFireCheck();
		DryElectricityCheck();

		foreach (Element status in Statuses.Keys.ToList()) {
			Health -= Statuses[status].UpdateAndGetDamage(Health>0);
		}

		foreach (Element e in FillRequirement.Keys.ToList()) {
			if (Statuses[e].CanFill()) {
				Statuses[e].FilledNow(); //so it won't fill too quickly
				TreatNeighboursWith(this, e, Statuses[e].Value, FillRequirement[e]);
			}
		}
		UpdateImage();
	}

	public void InformListeners(){
		_Population = (int)(Health * StartingPopulation);
		int d = PopulationDelta;
		if (d != 0 && Listeners != null) {
			foreach(Listener<ScoreType, float> l in Listeners){
				l.Inform(ScoreType.Population, d);
			}
		}
	}

	private void UpdateImage(){
		Sprite s = Template.Image;
		if (Health <= 0){
			s = Template.ImageD;
		}
		if (s == null) {
			Debug.Log("Sprite has not yet been loaded");
			return; 
		}

		if (UpdateHeightNeeded) {
			UpdateHeightNeeded = false;

			Rect before = GameObjectBuilding.GetComponent<RectTransform>().rect;
			float scale = before.width / s.rect.width;
			float targetHeight = scale * s.rect.height;
			Vector2 oldOffsetMax = GameObjectBuilding.GetComponent<RectTransform>().offsetMax;
			GameObjectBuilding.GetComponent<RectTransform>().offsetMax = new Vector2(oldOffsetMax.x, targetHeight);
		}


		Sprite actualSprite = GameObjectBuilding.GetComponent<Image> ().sprite;
		if (actualSprite != s){
			GameObjectBuilding.GetComponent<Image> ().sprite = s;
		}

		//update health bar
		GameObjectHealthBar.GetComponent<Image> ().fillAmount = Health;
		GameObjectHealthBar.transform.parent.gameObject.SetActive (Health > 0 && Health < 1);
	}

	public void TreatWith(Element e, float startingValue, bool isSource){
		if (Statuses[e].Add(startingValue, isSource) > 0 && Health > 0) {
			TreatYourselfWithYourPower();
		}
	}

	internal void CreateFromTemplate(BuildingTemplate bt, float pan) {

		Template = bt;
		StartingPopulation = _Population = Template.Population;
		_Health = Template.StartingHealth;

		FillRequirement.Add(Element.Electricity, delegate(Building b){
			return b.Statuses[Element.Water].Value > 0 && Statuses[Element.Water].Value > 0 ;
		});

		FillRequirement.Add(Element.Water, delegate(Building b){
			return 
				(
					(
						(	 //side up check if is in water
							GetNeighbour(b, Side.Up) != null &&
							GetNeighbour(b, Side.Up).Statuses[Element.Water].Value > 0 &&
							(
								GetNeighbour(b, Side.Up).Health == 0 || 
								GetNeighbour(b, Side.Up).Statuses[Element.Water].IsSource()
							)
						)
						||
						(	 //side down check if is in water
							GetNeighbour(b, Side.Down) != null &&
							GetNeighbour(b, Side.Down).Statuses[Element.Water].Value > 0 &&
							(
								GetNeighbour(b, Side.Down).Health == 0 ||
								GetNeighbour(b, Side.Down).Statuses[Element.Water].IsSource()
							)
						)
						||
						(	 //side left check if is in water
							GetNeighbour(b, Side.Left) != null &&
							GetNeighbour(b, Side.Left).Statuses[Element.Water].Value > 0  &&
							(
								GetNeighbour(b, Side.Left).Health == 0 ||
								GetNeighbour(b, Side.Left).Statuses[Element.Water].IsSource()
							)
						)
						||
						(	 //side right check if is in water
							GetNeighbour(b, Side.Right) != null &&
							GetNeighbour(b, Side.Right).Statuses[Element.Water].Value > 0 &&
							(
								GetNeighbour(b, Side.Right).Health == 0 ||
								GetNeighbour(b, Side.Right).Statuses[Element.Water].IsSource()
							)
						)
					)
				)
				&&
				(
					b.Statuses[Element.Water].Value == 0
					|| 
					b.Statuses[Element.Water].Value <  Statuses[Element.Water].Value
				);
		});

		startingPos = GameObjectBuilding.GetComponent<RectTransform>().localPosition;

		Dictionary<Element, GameObject> elGo = new Dictionary<Element, GameObject>();
		elGo.Add(Element.Fire, GameObjectFire);
		elGo.Add(Element.Water, GameObjectWaterLevel);
		elGo.Add(Element.SmokeAfterFire, GameObjectSmokeAfterFire);
		elGo.Add(Element.Electricity, GameObjectElectricity);
		elGo.Add(Element.Crush, GameObjectCrush);
		elGo.Add(Element.Die, GameObjectDie);

		//this is telling the sprite how to resize
		Dictionary<Element, bool> resizeFromDown = new Dictionary<Element, bool>();
		resizeFromDown.Add(Element.Fire, true);
		resizeFromDown.Add(Element.Water, false);
		resizeFromDown.Add(Element.SmokeAfterFire, false);
		resizeFromDown.Add(Element.Electricity, true);
		resizeFromDown.Add(Element.Crush, false);
		resizeFromDown.Add(Element.Die, false);

		//initializing statuses
		foreach (Element e in new Element[] { Element.Fire, Element.Water, Element.SmokeAfterFire, Element.Electricity, Element.Crush, Element.Die }) {
			float fillSpeedF = bt.Stats[StatType.FillSpeed][e];
			float strikeDamageF = bt.Stats[StatType.StrikeDamage][e];
			float effectDamageF = bt.Stats[StatType.EffectDamage][e];
			float effectTimeF = bt.Stats[StatType.EffectTime][e];
			bool resizeFromDownTmp = resizeFromDown[e];

			Statuses.Add(e, new BuildingStatus(elGo[e], bt.Effects[e], SoundManager.Clips[e], effectDamageF, effectTimeF, strikeDamageF, fillSpeedF, 
				SpriteManager.ElementPerBuildingSprites[SpriteManager.DefaultEffectPaths[e]], resizeFromDownTmp, pan));
		}
	}

	public void Clicked(BaseEventData b) {
		if (Listeners != null && Health > 0) {
			foreach (Listener<ScoreType, float> l in Listeners) {
				l.Inform(ScoreType.Interventions, 1f);
			}
		}

		foreach (Element e in Game.Me.TouchPowers) {
			TreatWith(e, 1, true);
		}
	}
}
