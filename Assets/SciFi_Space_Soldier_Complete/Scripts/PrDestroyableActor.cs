using UnityEngine;
using System.Collections;

public class PrDestroyableActor : MonoBehaviour {

    [Header("Stats")]
    public int Health = 100;
    public bool Destroyable = true;
	private int ActualHealth = 0;
    private bool Destroyed = false;

    [Header("VFX")]
    public float shakeFactor = 3.0f;
    public float shakeDuration = 1.0f;
    public GameObject DestroyFX;
	public Vector3 DestroyFXOffset = new Vector3(0,0,0);
    private PrTopDownCamera playerCamera;

    [Header("Damage")]
    public bool RadialDamage = true;
    public float DamageRadius = 2;
    public int Damage = 50;
    
    private bool Damaged = false;
    private float DamagedTimer = 0.0f;

    [Header("Debug")]
    public Mesh AreaRing;

    // Use this for initialization
    void Start () {
		ActualHealth = Health;
        if (GameObject.Find("PlayerCamera"))
            playerCamera = GameObject.Find("PlayerCamera").GetComponent<PrTopDownCamera>();

    }
	
	// Update is called once per frame
	void Update () {
        if ( Damaged )
        {
            DamagedTimer = Mathf.Lerp(DamagedTimer, 0.0f, Time.deltaTime * 10);
           
            if (Mathf.Approximately(DamagedTimer, 0.0f))
            {
                DamagedTimer = 0.0f;
                Damaged = false;

            }
            GetComponent<MeshRenderer>().material.SetFloat("_DamageFX", DamagedTimer);
        }
       
	}

	void ApplyDamage(int Damage)
	{
        if (Destroyable)
        {
            ActualHealth -= Damage;

            Damaged = true;
            DamagedTimer = 1.0f;

            if (ActualHealth <= 0 && !Destroyed)
            {
                DestroyActor();
            }
        }
		
	}

	void DestroyActor()
	{
        Destroyed = true;

        if (DestroyFX)
			Instantiate (DestroyFX, transform.position + DestroyFXOffset, Quaternion.identity);

        if (playerCamera)
        {
            playerCamera.ExplosionShake(shakeFactor, shakeDuration);
        }
	        			
		Destroy (this.gameObject);

        if (RadialDamage)
        {
            DestroyImmediate(GetComponent<Rigidbody>());
            DestroyImmediate(GetComponent<Collider>());
            Vector3 explosivePos = transform.position + DestroyFXOffset;
            Collider[] colls = Physics.OverlapSphere(explosivePos, DamageRadius);
            foreach (Collider col in colls)
            {

                if (col.CompareTag("Player") || col.CompareTag("Enemy") || col.CompareTag("Destroyable"))
                {
                    col.SendMessage("ApplyDamage", Damage, SendMessageOptions.DontRequireReceiver);
                }
            }

        }


    }

    void OnDrawGizmos()
    {
        if (RadialDamage)
          Gizmos.DrawMesh(AreaRing, transform.position + DestroyFXOffset, Quaternion.identity,Vector3.one * DamageRadius);

    }
}
