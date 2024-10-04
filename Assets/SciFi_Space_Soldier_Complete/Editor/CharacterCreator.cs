using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditorInternal;

public class CharacterCreator : ScriptableWizard
{
    public enum CHR
    {
        Player, Enemy
    }

    public CHR Type = CHR.Player;

    public GameObject CharacterMesh; //To Create
    public GameObject CharacterReference; //to use as reference

    [MenuItem("PolygonR/Create Character Wizard...")]
    static void CreateWizard()
    {
        ScriptableWizard.DisplayWizard<CharacterCreator>("PolygonR : Create Character", "Create Player");
    }

    void OnWizardUpdate()
    {
        helpString = "Set the Character Mesh (your new prefab) and the Character reference (the player or enemy that you want to copy parameters)";
        if (CharacterMesh == null || CharacterReference == null)
        {
            errorString = "you should assing the characters";
            isValid = false;
        }
        else
        {
            errorString = "";
            isValid = true;
        }
    }

    void OnWizardCreate()
    {

        if (Type == CHR.Player)
        {
            CreatePlayer();
        }
        else if (Type == CHR.Enemy)
        {
            CreateEnemy();
        }

    }

    void CreatePlayer()

    {

        //Create new character and parent it
        GameObject characterGO = Instantiate(CharacterMesh, Vector3.zero, Quaternion.identity) as GameObject;
        characterGO.name = "Player";

        characterGO.tag = "Player";
        characterGO.layer = LayerMask.NameToLayer("Character");

        //Look for Weapon Grip Node
        GameObject WeaponGrip = GameObject.Find("Weapon_R") as GameObject;
        if (WeaponGrip != null)
            WeaponGrip.SetActive(false);

        GameObject WeaponGripL = GameObject.Find("Weapon_L") as GameObject;
        if (WeaponGripL != null)
            WeaponGripL.SetActive(false);


        //New Player GO and replace reference
        GameObject newReference = Instantiate(CharacterReference, Vector3.zero, Quaternion.identity) as GameObject;
        newReference.name = CharacterMesh.name + "_Root";
        GameObject newReferencePlayer = newReference.GetComponentInChildren<PrTopDownCharController>().gameObject;
        newReferencePlayer.name = "OLD_Player";

        GameObject RefWeaponGrip = GameObject.Find("Weapon_R") as GameObject;
        if (WeaponGrip != null)
            WeaponGrip.SetActive(true);

        GameObject RefWeaponGripL = GameObject.Find("Weapon_L") as GameObject;
        if (WeaponGripL != null)
            WeaponGripL.SetActive(true);

        characterGO.transform.SetParent(newReference.transform);

        //Copy Components
        CopyComponents(newReferencePlayer.GetComponent<CapsuleCollider>(), characterGO);
        CopyComponents(newReferencePlayer.GetComponent<Rigidbody>(), characterGO);

        //AddComponents
        PrTopDownCharController newCharController = characterGO.AddComponent<PrTopDownCharController>();
        PrTopDownCharInventory newCharInv = characterGO.AddComponent<PrTopDownCharInventory>();
        AudioSource newAudio = characterGO.AddComponent<AudioSource>();

        CopyComponents(newReferencePlayer.GetComponent<PrTopDownCharController>(), newCharController);
        CopyComponents(newReferencePlayer.GetComponent<PrTopDownCharInventory>(), newCharInv);
        
        CopyComponents(newReferencePlayer.GetComponent<AudioSource>(), newAudio);
        

        //Move Objects
        ReparentObjects(newReferencePlayer.transform.Find("PlayerLight"), characterGO.transform);
        ReparentObjects(newReferencePlayer.transform.Find("HUD"), characterGO.transform);
        ReparentObjects(newReferencePlayer.transform.Find("CameraTarget"), characterGO.transform);

        //Set New Skinned Meshes to inventory
        characterGO.GetComponent<PrTopDownCharInventory>().MeshRenderers = characterGO.GetComponentsInChildren<SkinnedMeshRenderer>();

        //create AimIK Node
        GameObject aimIKNode = new GameObject("Weapon_AimIK") as GameObject;

        //Set Weapon Grip
        if (WeaponGrip != null && RefWeaponGrip != null)
        {
            Debug.Log("Weapon Found - Automatically assigned");
            CopyComponents(RefWeaponGrip.GetComponent<AudioSource>(), WeaponGrip);
            newCharInv.WeaponR = WeaponGrip.transform;

            //Set Aim IK Node hierarchy
            aimIKNode.transform.position = newCharInv.WeaponR.position;
            aimIKNode.transform.rotation = newCharInv.WeaponR.rotation;

            Quaternion aimLocalRot = Quaternion.Euler(90,0,0);
            aimIKNode.transform.rotation = aimIKNode.transform.rotation * aimLocalRot;


            aimIKNode.transform.SetParent(newCharInv.WeaponR.transform.parent);

            newCharInv.WeaponR.SetParent(aimIKNode.transform);

        }
        else
        {
            Debug.LogWarning("Weapon Grip node Not Found - You need to assing a node to the Weapon R and L in the Player Inventory");
            if (RefWeaponGrip)
                ReparentObjects(RefWeaponGrip.transform, characterGO.transform);
        }

        
       

        //Set Weapon Grip
        if (WeaponGripL != null && RefWeaponGripL != null)
        {
            Debug.Log("Weapon Found - Automatically assigned");
            CopyComponents(RefWeaponGripL.GetComponent<AudioSource>(), WeaponGripL);
            newCharInv.WeaponL = WeaponGripL.transform;
        }
        else
        {
            Debug.LogWarning("Weapon Grip node Not Found - You need to assing a node to the Weapon R and L in the Player Inventory");
            if (RefWeaponGripL)
                ReparentObjects(RefWeaponGripL.transform, characterGO.transform);
        }


        DestroyImmediate(newReferencePlayer);
    }

    void CreateEnemy()
    {
        //Create new character and parent it
        GameObject characterGO = Instantiate(CharacterMesh, Vector3.zero, Quaternion.identity) as GameObject;
        characterGO.name = "Enemy_" + CharacterMesh.name;
        characterGO.tag = "Enemy";
        characterGO.layer = LayerMask.NameToLayer("Character");
        
        //Look for Weapon Grip Node
        GameObject WeaponGrip = GameObject.Find("Weapon_R").gameObject as GameObject;
        if (WeaponGrip != null)
        {
            WeaponGrip.SetActive(false);
            Debug.Log("Weapon 1 has been Found" + WeaponGrip.name);

        }
            

        //New Player GO and replace reference
        GameObject newReferencePlayer = Instantiate(CharacterReference, Vector3.zero, Quaternion.identity) as GameObject;
        newReferencePlayer.name = CharacterReference.name + "_Reference";

        GameObject RefWeaponGrip = GameObject.Find("Weapon_R") as GameObject;
        AudioSource RefWeaponComp = GameObject.Find("Weapon_R").GetComponent<AudioSource>();
        if (WeaponGrip != null)
            WeaponGrip.SetActive(true);

        //AddComponents
        PrEnemyAI newEnemyAI = characterGO.AddComponent<PrEnemyAI>();
        AudioSource newAudio = characterGO.AddComponent<AudioSource>();

        //Copy Components
        CopyComponents(newReferencePlayer.GetComponent<PrEnemyAI>(), newEnemyAI);
        CopyComponents(newReferencePlayer.GetComponent<Rigidbody>(), characterGO);
        CopyComponents(newReferencePlayer.GetComponent<AudioSource>(), newAudio);
        CopyComponents(newReferencePlayer.GetComponent<CharacterController>(), characterGO);
        CopyComponents(newReferencePlayer.GetComponent<UnityEngine.AI.NavMeshAgent>(), characterGO);

        //Move Objects
        ReparentObjects(newReferencePlayer.transform.Find("DebugText"), characterGO.transform);

        //Set Weapon Grip
        if (WeaponGrip != null && RefWeaponGrip != null)
        {
            Debug.Log("Weapon Found");
            RefWeaponGrip.SetActive(false);

            CopyComponents(RefWeaponComp, GameObject.Find("Weapon_R").gameObject);
            newEnemyAI.WeaponGrip = GameObject.Find("Weapon_R").gameObject.transform;
           // WeaponGrip.name = "Weapon_R";
        }
        else
        {
            Debug.Log("Weapon Not Found");
        }

        //Set Sensor Position
        GameObject EnemySensors = new GameObject("EnemySensor") as GameObject;
        EnemySensors.transform.parent = characterGO.transform;
        EnemySensors.transform.position = new Vector3(0, 1.7f, 0);
        newEnemyAI.eyesAndEarTransform = EnemySensors.transform;

        //Set New Skinned Meshes to inventory
        characterGO.GetComponent<PrEnemyAI>().MeshRenderers = characterGO.GetComponentsInChildren<SkinnedMeshRenderer>();

        //DestroyImmediate(newReferencePlayer);
    }

    void CopyComponents(Component Source, Component Target )
    {
        ComponentUtility.CopyComponent(Source);
        ComponentUtility.PasteComponentValues(Target);
    }

    void CopyComponents(Component Source, GameObject Target)
    {
        ComponentUtility.CopyComponent(Source);
        ComponentUtility.PasteComponentAsNew(Target);
    }

    void ReparentObjects(Transform Target, Transform newParent)
    {
        Target.parent = newParent;
    }
    
}