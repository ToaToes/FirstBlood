using UnityEngine;
using System.Collections;

public class PrEnemyAI : MonoBehaviour
{

    public enum enemyType
    {
        Soldier,
        Pod
    }

    public enum AIState
    {
        Patrol,
        ChasingPlayer,
        AimingPlayer,
        AttackingPlayer,
        CheckingSound,
        Dead
    }

    [Header("Health and Stats")]
    public enemyType type = enemyType.Soldier;
    public int Health = 100;
    private bool dead = false;

    [Header("Basic AI Settings")]
    public AIState actualState = AIState.Patrol;

    public float chasingSpeed = 1.0f;
    public float normalSpeed = 0.75f;
    public float aimingSpeed = 0.3f;

    public float rotationSpeed = 0.15f;
    public float randomWaypointAccuracy = 1.0f;

    public bool useRootmotion = true;

    public bool lockRotation = false;
    public Vector3 lockedRotDir = Vector3.zero;


    [Header("AI Sensor Settings")]

    public float awarnessDistance = 20;
    public float aimingDistance = 15;
    public float attackDistance = 8;
    private float playerActualDistance = 50;

    [Range(10f, 360f)]
    public float lookAngle = 90f;
    public float hearingDistance = 20;

    public Transform eyesAndEarTransform;
    private Transform actualSensorTrans;
    private Vector3 actualSensorPos;

    private bool aiming = false;
    private Animator enemyAnimator;
    private bool playerIsVisible = false;
    private float playerLastTimeSeen = 0.0f;

    public float forgetPlayerTimer = 5.0f;
    private float actualforgetPlayerTimer = 0.0f;
    private Vector3 lastNoisePos;
    private float alarmedTimer = 10.0f;
    private float actualAlarmedTimer = 0.0f;
    private float newtAlarm = 0.0f;

    [HideInInspector]
    public bool lookForPlayer = false;

    [Header("Waypoints Settings")]
    public PrWaypointsRoute waypointRoute;
    private int actualWaypoint = 0;
    [HideInInspector]
    public Transform[] waypoints;
    private bool waiting = false;
    private float timeToWait = 3.0f;
    private float actualTimeToWait = 0.0f;
    private float waitTimer = 0.0f;
    private Vector3 finalGoal = Vector3.zero;

    [Header("Weapon Settings")]
    public Transform WeaponGrip;
    public GameObject AssignedWeapon;
    private PrWeapon weapon;
    private float LastFireTimer = 0.0f;
    public float FireRate = 1.0f;
    public float attackAngle = 5f;
    public int meleeAttacksOptions = 1;
    private int actualMeleeAttack = 0;
    public bool chooseRandomMeleeAttack = true;

    [HideInInspector]
    public GameObject player;
    [HideInInspector]
    public Transform playerTransform;
    //private Vector3 enemyPos;

    [Header("VFX")]
    public GameObject spawnFX;
    public GameObject damageVFX;
    public GameObject explosionFX;
    public bool destroyOnDead = false;
    public Renderer[] MeshRenderers;
    private Vector3 LastHitPos = Vector3.zero;
    private bool Damaged = false;
    private float DamagedTimer = 0.0f;

    [Header("Sound FX")]

    public float FootStepsRate = 0.4f;
    public float generalFootStepsVolume = 1.0f;
    public AudioClip[] Footsteps;
    private float LastFootStepTime = 0.0f;
    private AudioSource Audio;

    private UnityEngine.AI.NavMeshAgent agent;

    [Header("Debug")]
    public bool doNotAttackPlayer = false;
    public bool DebugOn = false;
    public TextMesh DebugText;
    [HideInInspector]
    public Mesh AreaMesh;
    public Mesh TargetArrow;

    // Use this for initialization
    void Start()
    {
        //Debug
        if (DebugText && !DebugOn)
            DebugText.GetComponent<Renderer>().enabled = false;

        //Create Waypoints Array
        SetWaypoints();

        //Ass
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        //Spawn FX
        if (spawnFX)
            Instantiate(spawnFX, transform.position, Quaternion.identity);

        //Rigidbody
        GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

        if (eyesAndEarTransform)
            actualSensorTrans = eyesAndEarTransform;
        else
            actualSensorTrans = this.transform;

        actualSensorPos = actualSensorTrans.position;
        actualforgetPlayerTimer = forgetPlayerTimer;

        SetTimeToWait();
        Audio = GetComponent<AudioSource>();
        player = GameObject.Find("Player");
        if (player)
            playerTransform = player.transform;
        if (GetComponent<Animator>())
            enemyAnimator = GetComponent<Animator>();

        //Initialize Waypoints
        if (waypoints.Length > 0)
        {
            finalGoal = waypoints[0].transform.position;
        }

        //Initialize Weapon
        InstantiateWeapon();

        GetCLoserWaypoint();

        if (lookForPlayer)
            CheckPlayerVisibility(360f);

        if (useRootmotion)
            enemyAnimator.applyRootMotion = false;
    }


    public void SetWaypoints()
    {
        if (waypointRoute)
        {
            waypoints = new Transform[waypointRoute.waypoints.Length];
            timeToWait = waypointRoute.timeToWait;

            for (int i = 0; i < (waypoints.Length); i++)
            {
                waypoints[i] = waypointRoute.waypoints[i];

            }
        }
    }

    void OnAnimatorMove()
    {
        if (agent != null && useRootmotion)
            agent.velocity = enemyAnimator.deltaPosition / Time.deltaTime;
    }

    void BulletPos(Vector3 BulletPosition)
    {
        LastHitPos = BulletPosition;
        LastHitPos.y = 0;
    }

    void ApplyDamage(int damage)
    {
        if ( actualState != AIState.Dead)
        {
            //Get Damage Direction
            Vector3 hitDir = new Vector3(LastHitPos.x,0, LastHitPos.z) - transform.position;
            Vector3 front = transform.forward;

            if (type == enemyType.Pod)
            {
                if (Vector3.Dot(front, hitDir) > 0)
                {
                    GetComponent<Animator>().SetInteger("Side", 1);
                }
                else
                {
                    GetComponent<Animator>().SetInteger("Side", 0);
                }
            }

            GetComponent<Animator>().SetTrigger("Hit");
            GetComponent<Animator>().SetInteger("Type", Random.Range(0, 1));
            //agent.speed -= (damage * 0.1f);


            Damaged = true;
            DamagedTimer = 1.0f;

            if (playerTransform != null)
            {
                agent.ResetPath();
                CheckPlayerNoise(playerTransform.position);
                actualState = AIState.ChasingPlayer;
            }
                
            Health -= damage;
            enemyAnimator.SetTrigger("Hit");
            if (Health <= 0)
            {
                Die();
            }
        }
        

    }

    void PodDestruction(Vector3 hitDir)
    {
        Destroy(GetComponent<Rigidbody>());
        Destroy(GetComponent<CharacterController>());
        Destroy(GetComponent<UnityEngine.AI.NavMeshAgent>());
        //this.enabled = false;
        GetComponent<Animator>().enabled = false;

        if (transform.Find("Root").GetComponent<SphereCollider>())
            transform.Find("Root").GetComponent<SphereCollider>().enabled = true;
        if (transform.Find("Root").GetComponent<Rigidbody>())
        {
            transform.Find("Root").GetComponent<Rigidbody>().isKinematic = false;
            transform.Find("Root").GetComponent<Rigidbody>().AddForce(hitDir * -10, ForceMode.Impulse);

        }

        if (destroyOnDead)
        {
            PrDestroyTimer DestroyScript = GetComponent<PrDestroyTimer>();
            DestroyScript.enabled = true;
        }

        gameObject.name = gameObject.name + "_DEAD";
        SendMessageUpwards("EnemyDead", SendMessageOptions.DontRequireReceiver);
    }

    void SoldierDestruction()
    {
        actualState = AIState.Dead;
        enemyAnimator.SetBool("Dead", true);
        GetComponent<CharacterController>().enabled = false;
        GetComponent<Rigidbody>().isKinematic = true;
        Destroy(GetComponent<UnityEngine.AI.NavMeshAgent>());
    }

    void Die()
    {
        //Send Message to Spawners
        SendMessageUpwards("EnemyDead", SendMessageOptions.DontRequireReceiver);

        gameObject.tag = "Untagged";
        Vector3 hitDir = LastHitPos - transform.position;

        if (explosionFX)
        {
            GameObject DieFXInstance = Instantiate(explosionFX, transform.Find("Root").transform.position, Quaternion.identity) as GameObject;
            DieFXInstance.transform.parent = transform.Find("Root").transform;
        }

        if (type == enemyType.Pod)
        {
            PodDestruction(hitDir);
        }
        else if (type == enemyType.Soldier)
        {
            SoldierDestruction();
        }

    }



    void InstantiateWeapon()
    {
        if (AssignedWeapon && WeaponGrip)
        {
            GameObject InstWeapon = Instantiate(AssignedWeapon, WeaponGrip.position, WeaponGrip.rotation) as GameObject;
            InstWeapon.transform.parent = WeaponGrip;
            InstWeapon.transform.localRotation = Quaternion.Euler(90, 0, 0);

            weapon = InstWeapon.GetComponent<PrWeapon>();
            weapon.Player = this.gameObject;
            weapon.AIWeapon = true;
            weapon.LaserSight.enabled = false;
            if (weapon.Type == PrWeapon.WT.Melee)
            {
                aimingDistance = weapon.MeleeRadius * 0.5f;
                attackDistance = weapon.MeleeRadius * 0.5f;
            }

            FireRate = weapon.FireRate;

            if (player)
                weapon.AIEnemyTarget = player.transform;
        }
    }



    void SetRandomPosVar(Vector3 goal)
    {
        finalGoal = goal + new Vector3(Random.Range(-randomWaypointAccuracy, randomWaypointAccuracy), 0, Random.Range(-randomWaypointAccuracy, randomWaypointAccuracy));
    }

    void SetTimeToWait()
    {
        actualTimeToWait = Random.Range(timeToWait * 0.75f, -timeToWait * 0.75f) + timeToWait;
    }

    public void SwitchDebug()
    {
        if (DebugOn)
        {
            DebugOn = false;
        }
        else
        {
            DebugOn = true;
        }

        if (DebugText)
            DebugText.GetComponent<Renderer>().enabled = DebugOn;

    }

    // Update is called once per frame
    void Update()
    {

        if (Damaged && MeshRenderers.Length > 0)
        {
            DamagedTimer = Mathf.Lerp(DamagedTimer, 0.0f, Time.deltaTime * 15);

            if (Mathf.Approximately(DamagedTimer, 0.0f))
            {
                DamagedTimer = 0.0f;
                Damaged = false;
            }

            foreach (Renderer Mesh in MeshRenderers)
            {
                Mesh.material.SetFloat("_DamageFX", DamagedTimer);
            }

            foreach (SkinnedMeshRenderer SkinnedMesh in MeshRenderers)
            {
                SkinnedMesh.material.SetFloat("_DamageFX", DamagedTimer);
            }

        }

        //Turn On Debug
        if (Input.GetKeyUp(KeyCode.F1))
        {
            SwitchDebug();
        }


        //Noise detection state update
        if (Health <= 0 || dead)
        {
            actualState = AIState.Dead;
        }
        else
        {
            //Set variables
            if (eyesAndEarTransform)
                actualSensorPos = actualSensorTrans.position;
            else
                actualSensorPos = transform.position + new Vector3(0.0f, 1.6f, 0.0f);

            if (actualAlarmedTimer > 0.0)
            {
                actualAlarmedTimer -= Time.deltaTime;
            }


            if (player && Health > 0 && !doNotAttackPlayer)
            {
                playerActualDistance = Vector3.Distance(actualSensorPos, playerTransform.position + new Vector3(0.0f, 1.6f, 0.0f));

                if (actualAlarmedTimer > 0.0)
                {
                    actualState = AIState.CheckingSound;
                }
                else if (actualAlarmedTimer <= 0.0 && playerActualDistance <= awarnessDistance)
                {
                    CheckPlayerVisibility(lookAngle);
                }
                else if (actualAlarmedTimer <= 0.0f || playerActualDistance > awarnessDistance)
                {
                    actualState = AIState.Patrol;
                }

            }

            else if (Health > 0)
            {
                actualState = AIState.Patrol;
            }

        }

        switch (actualState)
        {
            case AIState.Patrol:
                if (waypoints.Length > 0)
                {
                    if (agent.remainingDistance >= 1.0f && !waiting)
                    {
                        if (agent.remainingDistance >= 2.0f)
                            MoveForward(normalSpeed, finalGoal);
                        else
                            MoveForward(aimingSpeed, finalGoal);
                    }
                    else if (!waiting && waitTimer < actualTimeToWait)
                    {
                        waiting = true;

                    }
                    if (waiting)
                    {
                        StopMovement();
                        if (waitTimer < actualTimeToWait)
                            waitTimer += Time.deltaTime;
                        else
                            ChangeWaytpoint();
                    }

                }
                //Debug.Log("patrolling");
                break;
            case AIState.ChasingPlayer:

                MoveForward(chasingSpeed, playerTransform.position);

                //Debug.Log("chasing");
                break;
            case AIState.AimingPlayer:
                LookToTarget(playerTransform.position);
                MoveForward(aimingSpeed, playerTransform.position);
                AttackPlayer();
                // Debug.Log("Aiming Player");
                break;
            case AIState.AttackingPlayer:
                LookToTarget(playerTransform.position);
                StopMovement();
                AttackPlayer();
                // Debug.Log("Attacking");
                break;
            case AIState.Dead:
                StopMovement();
                //Debug.Log("Dead");
                break;
            case AIState.CheckingSound:
                if (Vector3.Distance(transform.position, lastNoisePos) >= 2.0f)
                {
                    MoveForward(normalSpeed, lastNoisePos);
                }
                else
                {
                    StopMovement();
                }
                CheckPlayerVisibility(lookAngle);
                //  Debug.Log("Checking noise position!!");
                break;
            default:
                // Debug.Log("NOTHING");
                break;
        }

        transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y, 0);
        if (agent != null)
            agent.updateRotation = true;

        if (DebugText && DebugOn)
        {
            DebugText.text = actualState.ToString() + "\n" + "Alarmed= " + Mathf.Round(actualAlarmedTimer) + "\n" + "ForgetPlayer= " + Mathf.Round(actualforgetPlayerTimer);
            if (actualState == AIState.Patrol)
                DebugText.color = Color.white;
            else if (actualState == AIState.ChasingPlayer)
                DebugText.color = Color.green * 3;
            else if (actualState == AIState.AimingPlayer)
                DebugText.color = Color.yellow * 2;
            else if (actualState == AIState.AttackingPlayer)
                DebugText.color = Color.red * 2;
            else if (actualState == AIState.CheckingSound)
                DebugText.color = Color.cyan;
            else if (actualState == AIState.Dead)
                DebugText.color = Color.gray;
        }

    }

    void AttackPlayer()
    {
        Vector3 targetDir = (player.transform.position + new Vector3(0f, 1.6f, 0f)) - (actualSensorPos);

        float angle = Vector3.Angle(targetDir, transform.forward);
        if (angle < attackAngle)
        {
            enemyAnimator.ResetTrigger("Alert");
            enemyAnimator.SetTrigger("CancelAlert");

            if (weapon && !doNotAttackPlayer)
            {

                if (Time.time >= (LastFireTimer + FireRate))
                {
                    LastFireTimer = Time.time;
                    //Attack Melee
                    if (weapon.Type == PrWeapon.WT.Melee)
                    {
                        UseMeleeWeapon();
                    }
                    //Attack Ranged 
                    else
                    {
                        ShootWeapon();
                    }
                }
            }
        }

    }

    void ShootWeapon()
    {
        weapon.Shoot();
        if (weapon.Reloading == false)
        {
            if (weapon.ActualBullets > 0)
                enemyAnimator.SetTrigger("Shoot");
            else
                weapon.Reload();
        }
    }

    void UseMeleeWeapon()
    {
        enemyAnimator.SetTrigger("MeleeAttack");

        if (chooseRandomMeleeAttack)
            enemyAnimator.SetInteger("MeleeType", Random.Range(0, meleeAttacksOptions));
        else
        {
            enemyAnimator.SetInteger("MeleeType", actualMeleeAttack);
            if (actualMeleeAttack < meleeAttacksOptions - 1)
                actualMeleeAttack += 1;
            else
                actualMeleeAttack = 0;
        }

    }

    void MeleeEvent()
    {
        weapon.AIAttackMelee(player.transform.position, player);
    }


    void CheckPlayerNoise(Vector3 noisePos)
    {
        if (!doNotAttackPlayer && !dead)
        {
            Vector3 currentGoal = agent.destination;
            SetWaypoint(noisePos);
            UnityEngine.AI.NavMeshPath NoisePath = agent.path;
            

            if (agent.remainingDistance != 0 && agent.CalculatePath(noisePos, NoisePath))
            {
                if (newtAlarm == 0.0f || Time.time >= newtAlarm + 15f)
                {
                    if (actualState == AIState.Patrol)
                    {
                        if (enemyAnimator)
                            enemyAnimator.SetTrigger("Alert");
                        lastNoisePos = noisePos;
                        newtAlarm = Time.time;

                        actualAlarmedTimer = alarmedTimer;

                        agent.SetDestination(noisePos);

                        Debug.Log(gameObject.name + " New Noise Position assigned. Position: " + lastNoisePos);
                        
                    }
                }
                else
                {
                    lastNoisePos = noisePos;
                    newtAlarm = Time.time;

                    actualAlarmedTimer = alarmedTimer;

                    agent.SetDestination(noisePos);

                    Debug.Log(gameObject.name + " New Noise Position assigned");
                }
               
            }
           
            else
            {
                agent.SetDestination(currentGoal);
                Debug.Log(gameObject.name + " Can´t Reach Noise");
                //Debug.Log("Can´t Reach Noise ");
            }
        }

    }

    void PlayerVisibilityRay(Vector3 targetDir)
    {
        RaycastHit hit;

        if (Physics.Raycast(actualSensorPos, targetDir, out hit))
        {
            if (!hit.collider.CompareTag("Player"))
            {
                //Debug.Log("Can´t see Player");
                playerIsVisible = false;
                if (actualforgetPlayerTimer <= 0.1f)
                {
                    actualState = AIState.Patrol;
                }
            }
            else if (hit.collider.CompareTag("Player"))
            {
                //Debug.Log("Seeing Player " + player.transform.position);
                //Debug.DrawRay(actualSensorPos, targetDir);

                playerIsVisible = true;
                actualAlarmedTimer = 0.0f;
                newtAlarm = 0.0f;
            }
        }
    }

    public void CheckPlayerVisibility(float actualLookAngle)
    {
        Vector3 targetDir = (player.transform.position + new Vector3(0f, 1.6f, 0f)) - (actualSensorPos);

        float angle = Vector3.Angle(targetDir, transform.forward);
        if (angle < actualLookAngle && !doNotAttackPlayer && !dead)
        {
            if (Time.time >= playerLastTimeSeen)
            {
                PlayerVisibilityRay(targetDir);
                playerLastTimeSeen = Time.time + 0.1f;
            }

            if (playerIsVisible)
            {
                playerActualDistance = Vector3.Distance(actualSensorPos, playerTransform.position + new Vector3(0.0f, 1.6f, 0.0f));

                if (playerActualDistance > aimingDistance /*&& agent.remainingDistance != 0*/)
                {
                    actualState = AIState.ChasingPlayer;
                    enemyAnimator.SetBool("Aiming", false);
                }
                else if (playerActualDistance <= aimingDistance /*&& agent.remainingDistance != 0*/)
                {
                    if (playerActualDistance <= attackDistance)
                    {
                        if (player.GetComponent<PrTopDownCharInventory>().isDead == false)
                            actualState = AIState.AttackingPlayer;
                        else
                            actualState = AIState.Patrol;
                    }
                    else
                    {
                        if (player.GetComponent<PrTopDownCharInventory>().isDead == false)
                            actualState = AIState.AimingPlayer;
                        else
                            actualState = AIState.Patrol;
                    }
                    enemyAnimator.SetBool("Aiming", true);
                }
                //}

            }
        }
        else if (actualAlarmedTimer > 0.0f)
        {
            actualState = AIState.CheckingSound;
            enemyAnimator.SetBool("Aiming", false);
        }
        else
        {
            actualState = AIState.Patrol;
            enemyAnimator.SetBool("Aiming", false);
        }

    }
    void FootStep()
    {
        if (Footsteps.Length > 0 && Time.time >= (LastFootStepTime + FootStepsRate))
        {
            int FootStepAudio = 0;

            if (Footsteps.Length > 1)
            {
                FootStepAudio = Random.Range(0, Footsteps.Length);
            }

            float FootStepVolume = enemyAnimator.GetFloat("Speed") * generalFootStepsVolume;
            if (aiming)
                FootStepVolume *= 0.5f;

            Audio.PlayOneShot(Footsteps[FootStepAudio], FootStepVolume);

            LastFootStepTime = Time.time;
        }
    }

    void StopMovement()
    {
        if (agent != null)
        {
            agent.velocity = Vector3.zero;
            enemyAnimator.SetFloat("Speed", agent.velocity.z, 0.25f, Time.deltaTime);
            
        }
           
    }

    void ChangeWaytpoint()
    {
        waiting = false;
        if (actualWaypoint < waypoints.Length - 1)
            actualWaypoint = actualWaypoint + 1;
        else
            actualWaypoint = 0;

        waitTimer = 0.0f;
        SetTimeToWait();
        SetWaypoint(waypoints[actualWaypoint].transform.position);

    }

    void SetWaypoint(Vector3 Pos)
    {
        SetRandomPosVar(Pos);
        agent.SetDestination(finalGoal);
    }

    void MoveForward(float speed, Vector3 goal)
    {
        if (useRootmotion)
        {
            agent.destination = goal;
            enemyAnimator.SetFloat("Speed", speed, 0.025f, Time.deltaTime);
        }
        else
        {
            //Debug.Log("Moving Forward");
            agent.destination = goal;
            
            agent.speed = speed;
            enemyAnimator.SetFloat("Speed", speed, 0.025f, Time.deltaTime);

        }

    }

    void LookToTarget(Vector3 target)
    {

        Quaternion targetRot = Quaternion.LookRotation(target - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed);

    }

    void OnTriggerEnter(Collider other)
    {

        if (other.tag == "Noise")
        {
            CheckPlayerNoise(other.transform.position);
        }


    }

    public void GetCLoserWaypoint()
    {
        int selected = 0;
        float selDist = 999f;
        float dist = 0.0f;
        bool changeWayp = false;
        if (waypoints.Length > 0)
        {
            for (int i = 0; i < waypoints.Length; i++)
            {
                dist = agent.remainingDistance;

                if (dist <= selDist)
                {
                    selDist = dist;

                    actualWaypoint = selected;

                    changeWayp = true;
                }
                selected += 1;
            }
            if (changeWayp)
            {
                ChangeWaytpoint();
            }
            else
            {
                SetWaypoint(waypoints[0].position);

            }
        }
    }

    void LateUpdate()
    {
        if (lockRotation)
        {
            transform.rotation = Quaternion.Euler(lockedRotDir);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        if (player && playerIsVisible)
        {
            if (eyesAndEarTransform)
                Gizmos.DrawLine(playerTransform.position + new Vector3(0, eyesAndEarTransform.position.y, 0), eyesAndEarTransform.position);
            else
                Gizmos.DrawLine(playerTransform.position + new Vector3(0f, 1.6f, 0f), transform.position + new Vector3(0f, 1.6f, 0f));
        }

        Quaternion lRayRot = Quaternion.AngleAxis(-lookAngle * 0.5f, Vector3.up);
        Quaternion rRayRot = Quaternion.AngleAxis(lookAngle * 0.5f, Vector3.up);
        Vector3 lRayDir = lRayRot * transform.forward;
        Vector3 rRayDir = rRayRot * transform.forward;
        if (eyesAndEarTransform)
        {
            Gizmos.DrawRay(eyesAndEarTransform.position, lRayDir * awarnessDistance);
            Gizmos.DrawRay(eyesAndEarTransform.position, rRayDir * awarnessDistance);
        }
        else
        {
            Gizmos.DrawRay(transform.position + new Vector3(0f, 1.6f, 0f), lRayDir * awarnessDistance);
            Gizmos.DrawRay(transform.position + new Vector3(0f, 1.6f, 0f), rRayDir * awarnessDistance);
        }


        Gizmos.DrawMesh(AreaMesh, transform.position, Quaternion.identity, Vector3.one * awarnessDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawMesh(AreaMesh, transform.position, Quaternion.identity, Vector3.one * aimingDistance);
        Gizmos.DrawSphere(lastNoisePos, 1.0f);

        Gizmos.color = Color.red;
        Gizmos.DrawMesh(AreaMesh, transform.position, Quaternion.identity, Vector3.one * attackDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawMesh(AreaMesh, transform.position, Quaternion.identity, Vector3.one * hearingDistance);

        Gizmos.color = Color.magenta;
        Gizmos.DrawMesh(TargetArrow, finalGoal, Quaternion.identity, Vector3.one);

        
    }

}
