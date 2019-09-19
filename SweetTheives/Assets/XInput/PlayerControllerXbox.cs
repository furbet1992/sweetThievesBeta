﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XboxCtrlrInput;


public class PlayerControllerXbox : MonoBehaviour
{

	//max move speeds
	public float maxMoveSpeed = 20;
	public XboxController controller;
	public float dashSpeed = 20;
	private Vector3 newPosition;
	public XboxButton dashButton = XboxButton.A;
	public XboxButton tongueButton = XboxButton.B;

	//public Material matRed;
	//public Material matGreen;
	//public Material matBlue;
	//public Material matYellow;

	private static bool didQueryNumOfCtrlrs = false;

	// The layer that the walls and other environment are in
	public LayerMask environmentLayer;
	// The layer that the collectabels are in
	public LayerMask collectableLayer;
	// The amount that the grapple accelerates object every second
	public float grappleAcceleration = 20;
	// How close to the collision position for the tongue it has to be for it to release
	public float acceptanceRange = 0.5f;
	// The radus for the sphere cast
	public float sphereCastRadus = 5f;
	// How many collectables id the player holding
	public int heldCollectables = 0;

	// The amount that the player is moving by the tongue every frame
	private Vector3 tongueGrappleMovement;
	// The amout of time that it has to retract in frames
	private float timeLeftToRetract;

	// If the tongue is currently retracting
	private bool tongueHitEnvionment = false;

	// Variables needed by the collectableController
	[HideInInspector]
	public bool tongueHitCollectible = false;
	[HideInInspector]
	public GameObject objectHit;
	// The postion for the hit on the tongue on a wall
	[HideInInspector]
	public Vector3 hitPoint;

	private Rigidbody rb;
	private LineRenderer line;

	//The current speed of the collectable
	private float collectableSpeed;


	// Start is called before the first frame update
	void Start()
	{

		// Only draws tongue if there is a line renderer on the game object
		line = GetComponent<LineRenderer>();
		rb = this.gameObject.GetComponent<Rigidbody>();

		float xAxis = XCI.GetAxis(XboxAxis.LeftStickX, XboxController.First);
		float yAxis = XCI.GetAxis(XboxAxis.LeftStickY, XboxController.First);

		//switch (controller)
		//{
		//    case XboxController.First: GetComponent<Renderer>().material = matRed; break;
		//    case XboxController.Second: GetComponent<Renderer>().material = matGreen; break;
		//    case XboxController.Third: GetComponent<Renderer>().material = matBlue; break;
		//    case XboxController.Fourth: GetComponent<Renderer>().material = matYellow; break;
		//}

		if (!didQueryNumOfCtrlrs)
		{
			didQueryNumOfCtrlrs = true;

			int queriedNumberOfCtrlrs = XCI.GetNumPluggedCtrlrs();
			if (queriedNumberOfCtrlrs == 1)
			{
				Debug.Log("Only " + queriedNumberOfCtrlrs + " Xbox controller plugged in.");
			}
			else if (queriedNumberOfCtrlrs == 0)
			{
				Debug.Log("No Xbox controllers plugged in!");
			}
			else
			{
				Debug.Log(queriedNumberOfCtrlrs + " Xbox controllers plugged in.");
			}

			XCI.DEBUG_LogControllerNames();
		}

	}

	// Update is called once per frame
	void Update()
	{
		// Get input (put it in x and z because we are moving accross those axes)
		Vector3 xboxInput = new Vector3(XCI.GetAxis(XboxAxis.LeftStickX, controller), 0.0f, XCI.GetAxis(XboxAxis.LeftStickY, controller));

		// dash
		if (XCI.GetButton(dashButton, controller))
		{
			transform.position += xboxInput * dashSpeed * Time.deltaTime;
		}
		// movement from left joystick
		transform.position += xboxInput * maxMoveSpeed * Time.deltaTime;

		// Look the direction the controler is going
		if (xboxInput.x != 0 || xboxInput.z != 0)
		{
			transform.rotation = Quaternion.LookRotation(xboxInput);
		}

		// Commented out because causeing errors because there is no animator on gameObject
		//GetComponent<Animator>().SetFloat("Speed", 1);

		// Tougue lash
		if (XCI.GetButtonDown(tongueButton, controller))
		{
			Debug.Log("B button pressed");
			// The object that the circleCast hits if it hits something
			RaycastHit hit;

			// Casts sphereCast
			if (Physics.SphereCast(transform.position, sphereCastRadus, transform.forward, out hit, Mathf.Infinity))
			{
				Debug.Log("SphereCase hit " + hit.collider.gameObject.name);
				// The possition that the tongue hit
				hitPoint = hit.point;
				// Set what it hit.
				objectHit = hit.collider.gameObject;
				// If the tongue has hit a wall or other environment
				if (environmentLayer.value == (1 << objectHit.layer))
				{
					tongueHitEnvionment = true;
					tongueHitCollectible = false;
				}
				// If the wall has hit a collectable
				else if (collectableLayer.value == (1 << objectHit.layer))
				{
					tongueHitEnvionment = false;
					tongueHitCollectible = true;
				}
			}
			else
			{
				objectHit = null;
				tongueHitCollectible = false;
				tongueHitEnvionment = false;
			}
		}
	}

	void FixedUpdate()
	{
		// If the tongue has hit a wall or other environment
		if (tongueHitEnvionment)
		{
			// Draw tongue to position hit
			if (line != null)
			{
				line.enabled = true;
				line.SetPositions(new Vector3[]
					{
					transform.position,
					hitPoint
				});
			}
			// The seperation between the player and the grapple point.
			Vector3 differnce = transform.position - hitPoint;

			if (!XCI.GetButton(tongueButton, controller))
			{
				// Move player towards tongue
				rb.AddForce(-differnce.normalized * grappleAcceleration, ForceMode.Acceleration);
			}
			// Check if the tongue has fully retracted
			if (differnce.x < acceptanceRange && differnce.x > -acceptanceRange &&
				differnce.y < acceptanceRange && differnce.y > -acceptanceRange &&
				differnce.z < acceptanceRange && differnce.z > -acceptanceRange)
			{
				tongueHitEnvionment = false;
			}

		}
		// If the tongue has hit a collectable
		else if (tongueHitCollectible)
		{
			// Draw tongue to collectable
			if (line != null)
			{
				line.enabled = true;
				line.SetPositions(new Vector3[]
					{
					transform.position,
					objectHit.transform.position
				});
			}
			if (!XCI.GetButton(tongueButton, controller))
			{
				// Move collectable towards player
				Vector3 towardsPlayer = (transform.position - objectHit.transform.position).normalized;

				collectableSpeed += grappleAcceleration * Time.fixedDeltaTime;
				objectHit.transform.position = objectHit.transform.position + (towardsPlayer * collectableSpeed) * Time.fixedDeltaTime;
			}
		}
		else
		{
			rb.velocity = Vector3.zero;
			if (line != null)
			{
				line.enabled = false;
			}
		}
	}

}
