using UnityEngine;
using System.Collections;

public class DoorManager : MonoBehaviour {

    public bool IsActive = true;

    [Header("Generic Vars")]
    public GameObject[] TargetGameObjects;
	public float DoorX = 1.0f;
	public float DoorSpeed = 2.0f;

	public bool AffectCharacters = true;
	public bool AffectBullets = false;
	public bool AffectEnemys = true;

	private bool Action = false;
	

    [Header("Sound FX")]
    public AudioClip DoorSlideSFX;
    private AudioSource Audio;

    //[Header("Visuals")]
    //public Renderer[] DoorPanels;

    // Use this for initialization
    void Start () {
        
		SetNavigationCarve();
        
        Audio = GetComponent<AudioSource>() as AudioSource;
    }


	// Update is called once per frame
	void Update () {
		if (IsActive && Action)
		{
			foreach (GameObject GOTarget in TargetGameObjects)
			{
				GOTarget.transform.localPosition = Vector3.Lerp(GOTarget.transform.localPosition, new Vector3( DoorX , GOTarget.transform.localPosition.y, GOTarget.transform.localPosition.z), Time.deltaTime  * DoorSpeed);
			}
		}
		else 
		{
			foreach (GameObject GOTarget in TargetGameObjects)
			{
				if (Mathf.Approximately( GOTarget.transform.localPosition.x , 0.0f) == false)
				{
					GOTarget.transform.localPosition = Vector3.Lerp(GOTarget.transform.localPosition, new Vector3(0,GOTarget.transform.localPosition.y,GOTarget.transform.localPosition.z), Time.deltaTime  * DoorSpeed);
				}
			}
		}
	
	}

    void SetNavigationCarve()
    {
        foreach (GameObject Door in TargetGameObjects)
        {
            if (Door.GetComponent<UnityEngine.AI.NavMeshObstacle>() != null)
            {
                Door.GetComponent<UnityEngine.AI.NavMeshObstacle>().carving = !IsActive;
            }
        }
    }

    public void SetActive()
    {
        /*
        if (IsActive)
        {
            IsActive = false;
        }

        else
        {
            IsActive = true;
        }*/

		// or use ^ operation
        IsActive ^= true;

        SetNavigationCarve();

        //SetPanelColors();
    }

	// object in the way of door
	void OnTriggerStay(Collider other) {
        if (IsActive)
        {
            if (other.gameObject.tag == "Player" && AffectCharacters && TargetGameObjects != null)
            {
                if (!Action)
                    Audio.PlayOneShot(DoorSlideSFX);
                Action = true;
            }
            else if (other.gameObject.tag == "Enemy" && AffectEnemys && TargetGameObjects != null)
            {
                if (!Action)
                    Audio.PlayOneShot(DoorSlideSFX);
                Action = true;
            }
        }
		
    }


	// object leave the door
	void OnTriggerExit(Collider other)
	{
		if (other.gameObject.tag == "Player" && AffectCharacters && TargetGameObjects != null)
		{
            if (Action)
                Audio.PlayOneShot(DoorSlideSFX);
            Action = false;
		}
        else if (other.gameObject.tag == "Enemy" && AffectEnemys && TargetGameObjects != null)
        {
            if (Action)
                Audio.PlayOneShot(DoorSlideSFX);
            Action = false;
        }
    }
	
}
