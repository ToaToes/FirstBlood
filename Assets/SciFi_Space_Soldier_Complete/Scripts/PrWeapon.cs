using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class PrWeapon : MonoBehaviour {


    public string WeaponName = "Rifle";

    public enum WT
    {
        Pistol = 0, Rifle = 1, Minigun = 2, RocketLauncher = 3, Melee = 4
    }

    public WT Type = WT.Rifle;

    [Header("Melee Radius")]
    public float MeleeRadius = 1.0f;
    public int meleeDamage = 1;
    private List<GameObject> meleeFinalTarget;

    [Header("Stats")]
    
    public int BulletsPerShoot = 1;
    public int BulletDamage = 20;
    public float BulletSize = 1.0f;

    public float BulletSpeed = 1.0f;
    public float BulletAccel = 0.0f;

    public int Bullets = 10;
    [HideInInspector] 
    public int ActualBullets = 0;

    public int Clips = 3;
	private int ActualClips = 0;

	public float ReloadTime = 1.0f;
    public bool playReloadAnim = true;
	private float ActualReloadTime = 0.0f;

    [HideInInspector]
    public bool Reloading = false;

	public float FireRate = 0.1f;
	public float AccDiv = 0.0f;

    public float radialAngleDirection = 0.0f;

    public float shootingNoise = 25f;

    [Header("References & VFX")]
    public float shootShakeFactor = 2.0f;
    public Transform ShootFXPos;
	public GameObject BulletPrefab;
	public GameObject ShootFXFLash;
	public Light ShootFXLight;
    public Renderer LaserSight;
    private PrTopDownCamera playerCamera;

    [HideInInspector]
    public Transform ShootTarget;
    [HideInInspector]
    public GameObject Player;

    [Header("Sound FX")]
    public AudioClip[] ShootSFX;
    public AudioClip ReloadSFX;
    public AudioClip ShootEmptySFX;
    [HideInInspector]
    public AudioSource Audio;
    
    [Header("Autoaim")]
    public float AutoAimAngle = 7.5f;
    public float AutoAimDistance = 10.0f;

    private Vector3 EnemyTargetAuto = Vector3.zero;
    private Vector3 FinalTarget = Vector3.zero;

    //HUD
    [Header("HUD")]
    public Sprite WeaponPicture;
    [HideInInspector]
    public GameObject HUDWeaponPicture;
    [HideInInspector]
    public GameObject HUDWeaponBullets;
    [HideInInspector]
    public GameObject HUDWeaponBulletsBar;
    [HideInInspector]
    public GameObject HUDWeaponClips;

    //Object Pooling Manager
    private GameObject[] GameBullets;
    private int ActualGameBullet = 0;
    private GameObject Muzzle;

    [HideInInspector]
    public bool AIWeapon = false;
    [HideInInspector]
    public Transform AIEnemyTarget;
    

    // Use this for initialization
    void Start()
    {
        Audio = transform.parent.GetComponent<AudioSource>();

        ActualBullets = Bullets;
        ActualClips = Clips;

        if (!AIWeapon)
        {
            HUDWeaponBullets.GetComponent<Text>().text = ActualBullets.ToString();
            HUDWeaponClips.GetComponent<Text>().text = ActualClips.ToString();
            HUDWeaponBulletsBar.GetComponent<RectTransform>().localScale = Vector3.one;
        }
        
        //Basic Object Pooling Initialization ONLY FOR RANGED WEAPONS
        if (Type != WT.Melee)
        {
            GameBullets = new GameObject[Bullets * BulletsPerShoot];
            GameObject BulletsParent = new GameObject(WeaponName + "_Bullets");

            for (int i = 0; i < (Bullets * BulletsPerShoot); i++)
            {
                GameBullets[i] = Instantiate(BulletPrefab, ShootFXPos.position, ShootFXPos.rotation) as GameObject;
                GameBullets[i].SetActive(false);
                GameBullets[i].name = WeaponName + "_Bullet_" + i.ToString();
                GameBullets[i].transform.parent = BulletsParent.transform;
                GameBullets[i].GetComponent<PrBullet>().InitializePooling();
            }
        }
        else
        {
            /*
            HUDWeaponBullets.GetComponent<Text>().text = "";
            HUDWeaponClips.GetComponent<Text>().text = "";
            HUDWeaponBulletsBar.GetComponent<RectTransform>().localScale = Vector3.zero;*/
        }

        if (ShootFXFLash)
        {
            Muzzle = Instantiate(ShootFXFLash, ShootFXPos.position, ShootFXPos.rotation ) as GameObject;
            Muzzle.transform.parent = ShootFXPos.transform;
            Muzzle.SetActive(false);
        }
        
        if (GameObject.Find("PlayerCamera") != null)
        {
            playerCamera = GameObject.Find("PlayerCamera").GetComponent<PrTopDownCamera>();
            
        }

    }
	
	// Update is called once per frame
	void Update () {

        if (Reloading)
		{
			ActualReloadTime += Time.deltaTime;
			if (ActualReloadTime >= ReloadTime)
			{
				Reloading = false;
				ActualReloadTime = 0.0f;
				SendMessageUpwards("EndReload", SendMessageOptions.DontRequireReceiver);
                WeaponEndReload();
			}
		}
        
		
	}

    public void TurnOffLaser()
    {
        LaserSight.enabled = false;
    }

    void LateUpdate()
    {
        if (!AIWeapon)
        {
            LaserSight.transform.position = ShootFXPos.position;
            LaserSight.transform.LookAt(ShootTarget.position, Vector3.up);
        }
    }

    void WeaponEndReload()
    {
        ActualBullets = Bullets;
       
        UpdateWeaponGUI();
        
    }

    void UpdateWeaponGUI()
    {
        if (!AIWeapon && Type != WT.Melee )
        {
            HUDWeaponBullets.GetComponent<Text>().text = ActualBullets.ToString();
            HUDWeaponClips.GetComponent<Text>().text = ActualClips.ToString();
            HUDWeaponBulletsBar.GetComponent<RectTransform>().localScale = new Vector3((1.0f / Bullets) * ActualBullets, 1.0f, 1.0f);
        }
    }

    public void UpdateWeaponGUI(GameObject weapPic)
    {
        if (!AIWeapon && Type != WT.Melee)
        {
            HUDWeaponBullets.GetComponent<Text>().text = ActualBullets.ToString();
            HUDWeaponClips.GetComponent<Text>().text = ActualClips.ToString();
            HUDWeaponBulletsBar.GetComponent<RectTransform>().localScale = new Vector3((1.0f / Bullets) * ActualBullets, 1.0f, 1.0f);
            HUDWeaponPicture = weapPic;
        }
    }

    public void CancelReload()
    {
        Reloading = false;
        if (playReloadAnim)
            Player.GetComponent<Animator>().SetBool("Reloading", false);
        SendMessageUpwards("EndReload", SendMessageOptions.DontRequireReceiver);
        ActualReloadTime = 0.0f;
    }

	public void Reload()
	{
		if (ActualClips > 0 || Clips == -1)
		{
            if (!AIWeapon)
                ActualClips -= 1;
            if (playReloadAnim)
                Player.GetComponent<Animator>().SetBool("Reloading", true);
            Reloading = true;
            Audio.PlayOneShot(ReloadSFX);
            ActualReloadTime = 0.0f;
        }
	}

    void AIReload()
    {
        SendMessageUpwards("StartReload", SendMessageOptions.DontRequireReceiver);
        Reloading = true;
        Audio.PlayOneShot(ReloadSFX);
        ActualReloadTime = 0.0f;
    }

    void AutoAim()
    {
        //Autoaim////////////////////////

        GameObject[] Enemys = GameObject.FindGameObjectsWithTag("Enemy");
        if (Enemys != null)
        {
            float BestDistance = 100.0f;

            foreach (GameObject Enemy in Enemys)
            {
                Vector3 EnemyPos = Enemy.transform.position;
                Vector3 EnemyDirection = EnemyPos - Player.transform.position;
                float EnemyDistance = EnemyDirection.magnitude;

                if (Vector3.Angle(Player.transform.forward, EnemyDirection) <= AutoAimAngle && EnemyDistance < AutoAimDistance)
                {
                    //
                    if (Enemy.GetComponent<PrEnemyAI>().actualState != PrEnemyAI.AIState.Dead)
                    {
                        if (EnemyDistance < BestDistance)
                        {
                            BestDistance = EnemyDistance;
                            EnemyTargetAuto = EnemyPos + new Vector3(0, 1, 0);
                        }
                    }
                   

                }
            }
        }

        if (EnemyTargetAuto != Vector3.zero)
        {
            FinalTarget = EnemyTargetAuto;
            ShootFXPos.transform.LookAt(FinalTarget);
        }
        else
        {
            ShootFXPos.transform.LookAt(ShootTarget.position);
            FinalTarget = ShootTarget.position;
        }

        //End of AutoAim
        /////////////////////////////////

    }

    void AIAutoAim()
    {
        //Autoaim////////////////////////

        Vector3 PlayerPos = AIEnemyTarget.position + new Vector3(0, 1.2f, 0);
        FinalTarget = PlayerPos;
        
      
    }

    public void PlayShootAudio()
    {
        if (ShootSFX.Length > 0)
        {
            int FootStepAudio = 0;

            if (ShootSFX.Length > 1)
            {
                FootStepAudio = Random.Range(0, ShootSFX.Length);
            }

            float RandomVolume = Random.Range(0.6f, 1.0f);

            Audio.PlayOneShot(ShootSFX[FootStepAudio], RandomVolume);

            if (!AIWeapon)
                Player.SendMessage("MakeNoise", shootingNoise);
           
        }
    }

    public void Shoot()
	{
        if (!AIWeapon)
        {
            AutoAim();
        }
        else
        {
            AIAutoAim();
        }

        if (ActualBullets > 0)
            PlayShootAudio();
        //else
        //    Audio.PlayOneShot(ShootEmptySFX);
        float angleStep = radialAngleDirection / BulletsPerShoot;
        float finalAngle = 0.0f; 

        for (int i = 0; i < BulletsPerShoot; i++)
		{
            
            float FinalAccuracyModX = Random.Range(AccDiv, -AccDiv) * Vector3.Distance(Player.transform.position, FinalTarget);
            FinalAccuracyModX /= 100;

            float FinalAccuracyModY = Random.Range(AccDiv, -AccDiv) * Vector3.Distance(Player.transform.position, FinalTarget);
            FinalAccuracyModY /= 100;

            float FinalAccuracyModZ = Random.Range(AccDiv, -AccDiv) * Vector3.Distance(Player.transform.position, FinalTarget);
            FinalAccuracyModZ /= 100;
          
            Vector3 FinalOrientation = FinalTarget + new Vector3(FinalAccuracyModX, FinalAccuracyModY, FinalAccuracyModZ);

			ShootFXPos.transform.LookAt(FinalOrientation);

            if (BulletsPerShoot > 1 && radialAngleDirection > 0.0f)
            {
                Quaternion aimLocalRot = Quaternion.Euler(0, finalAngle - (radialAngleDirection / 2) + (angleStep * 0.5f), 0);
                ShootFXPos.transform.rotation = ShootFXPos.transform.rotation * aimLocalRot;

                finalAngle += angleStep;
            } 
            

            if (BulletPrefab && ShootFXPos && !Reloading)
			{
				if (ActualBullets > 0)
				{
                    //Object Pooling Method 
                    GameObject Bullet = GameBullets[ActualGameBullet];
                    Bullet.transform.position = ShootFXPos.position;
                    Bullet.transform.rotation = ShootFXPos.rotation;
                    Bullet.GetComponent<Rigidbody>().isKinematic = false;
                    Bullet.GetComponent<Collider>().enabled = true;
                    Bullet.GetComponent<PrBullet>().timeToLive = 3.0f;
                    Bullet.GetComponent<PrBullet>().ResetPooling();

                    Bullet.SetActive(true);
                    ActualGameBullet += 1;
                    if (ActualGameBullet >= GameBullets.Length)
                        ActualGameBullet = 0;

                    //Object Pooling VFX
                    Muzzle.transform.rotation = transform.rotation;
                    EmitParticles(Muzzle);
 
                    //Generic 
                    Bullet.GetComponent<PrBullet>().Damage = BulletDamage;
                    Bullet.GetComponent<PrBullet>().BulletSpeed = BulletSpeed;
                    Bullet.GetComponent<PrBullet>().BulletAccel = BulletAccel;
                    Bullet.transform.localScale = Bullet.GetComponent<PrBullet>().OriginalScale  * BulletSize;

                    ShootFXLight.GetComponent<PrLightAnimator>().AnimateLight(true);
					ActualBullets -= 1;

                    if (playerCamera)
                    {
                        if (!AIWeapon)
                            playerCamera.Shake(shootShakeFactor, 0.2f);
                        else
                            playerCamera.Shake(shootShakeFactor * 0.5f, 0.2f);
                    }

                    if (ActualBullets == 0)
						Reload();

				}

                
            }

            UpdateWeaponGUI();

            EnemyTargetAuto = Vector3.zero;

            
        }
	}

    void EmitParticles(GameObject VFXEmiiter)
    {
        VFXEmiiter.SetActive(true);
        VFXEmiiter.GetComponent<ParticleSystem>().Play();
    }


    public void AIAttackMelee(Vector3 playerPos, GameObject targetGO)
    {
        PlayShootAudio();

        //Object Pooling VFX
        if (Muzzle)
        {
            EmitParticles(Muzzle);
        }
        if (ShootFXLight)
            ShootFXLight.GetComponent<PrLightAnimator>().AnimateLight(true);

        if (Vector3.Distance(playerPos, ShootFXPos.position) <= MeleeRadius)
        {
            //Debug.Log("Hit Player Sucessfully");
            targetGO.SendMessage("ApplyDamage", meleeDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void AttackMelee()
    {
        PlayShootAudio();

        //Object Pooling VFX
        if (Muzzle)
        {
            EmitParticles(Muzzle);
        }
        //Use Light
        if (ShootFXLight)
            ShootFXLight.GetComponent<PrLightAnimator>().AnimateLight(true);

        //Start Finding Enemy Target
        meleeFinalTarget = new List<GameObject>();

        GameObject[] Enemys = GameObject.FindGameObjectsWithTag("Enemy");
        
        if (Enemys != null)
        {
            float BestDistance = 100.0f;

            foreach (GameObject Enemy in Enemys)
            {
                Vector3 EnemyPos = Enemy.transform.position;
                Vector3 EnemyDirection = EnemyPos - Player.transform.position;
                float EnemyDistance = EnemyDirection.magnitude;

                if (Vector3.Angle(Player.transform.forward, EnemyDirection) <= 90 && EnemyDistance < MeleeRadius)
                {
                    //
                    if (Enemy.GetComponent<PrEnemyAI>().actualState != PrEnemyAI.AIState.Dead)
                    {
                        if (EnemyDistance < BestDistance)
                        {
                            BestDistance = EnemyDistance;
                            meleeFinalTarget.Add(Enemy);// = Enemy;
                        }
                       
                    }
                }
            }
        }

        GameObject[] destroyables = GameObject.FindGameObjectsWithTag("Destroyable");

        if (destroyables != null)
        {
            float BestDistance = 100.0f;

            foreach (GameObject destroyable in destroyables)
            {
                Vector3 destroyablePos = destroyable.transform.position;
                Vector3 destrDirection = destroyablePos - Player.transform.position;
                float EnemyDistance = destrDirection.magnitude;

                if (Vector3.Angle(Player.transform.forward, destrDirection) <= 90 && EnemyDistance < MeleeRadius)
                {
                    if (EnemyDistance < BestDistance)
                    {
                        BestDistance = EnemyDistance;
                        meleeFinalTarget.Add(destroyable);// = Enemy;
                    }
                 }
            }
        }

        //if (meleeFinalTarget.Count != 0)
        //{
        foreach (GameObject meleeTarget in meleeFinalTarget)
        {
            //Debug.Log("Hit Enemy Sucessfully");
            meleeTarget.SendMessage("ApplyDamage", meleeDamage, SendMessageOptions.DontRequireReceiver);
        }
            
       // }
        //else
        //{
            //Debug.Log("Attack Missed");
        //}
    }

    public void LoadAmmo(int LoadType)
    {
        ActualBullets = Bullets;
        ActualClips = Clips / LoadType;
        WeaponEndReload();
    }

    void OnDrawGizmos()
    {
        /*
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(FinalTarget, 0.25f);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(ShootFXPos.position, 0.2f);*/
      
    }
}
