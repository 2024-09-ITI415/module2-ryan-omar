﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is an enum of the various possible weapon types.
/// It also includes a "shield" type to allow a shield power-up.
/// Items marked [NI] below are Not Implemented in the IGDPD book.
/// </summary>
public enum WeaponType
{
    none, // The default / no weapons
    blaster, // A simple blaster
    spread, // Two shots simultaneously
    phaser, // [NI] Shots that move in waves
    missile, // [NI] Homing missiles
    laser, // [NI] Damage over time
    shield, // Raise shieldLevel
    timeBomb //A bomb that has a timer
}

/// <summary>
/// The WeaponDefinition class allows you to set the properties
/// of a specific weapon in the Inspector. The Main class has
/// an array of WeaponDefinitions that makes this possible.
/// </summary>
[System.Serializable]
public class WeaponDefinition
{
    public WeaponType type = WeaponType.none;
    public string letter; // Letter to show on the power-up
    public Color color = Color.white; // Color of Collar & power-up
    public GameObject projectilePrefab; // Prefab for projectiles
    public Color projectileColor = Color.white;
    public float damageOnHit = 0; // Amount of damage caused
    public float continuousDamage = 0; // Damage per second (Laser)
    public float delayBetweenShots = 0;
    public float velocity = 0; // Speed of projectiles
    public float explosionRadius = 5f; //Explosion range for time bomb
    public float explosionDelay = 3f; //Explosion range for time bomb
}
public class Weapon : MonoBehaviour {
    static public Transform PROJECTILE_ANCHOR;

    [Header("Set Dynamically")]
    [SerializeField]
    private WeaponType _type = WeaponType.none;
    public WeaponDefinition def;
    public GameObject collar;
    public float lastShotTime; // Time last shot was fired
    private Renderer collarRend;

    [Header("Audio")]
    public AudioClip fireSound;
    private AudioSource audioSource;

    [Header("Audio Settings")]
    public float fireVolume = 0.4f; // 1.0 is full volume


    private void Start()
    {
        collar = transform.Find("Collar").gameObject;
        collarRend = collar.GetComponent<Renderer>();

        // Call SetType() for the default _type of WeaponType.none
        SetType(_type);

        // Dynamically create an anchor for all Projectiles
        if(PROJECTILE_ANCHOR == null)
        {
            GameObject go = new GameObject("_ProjectileAnchor");
            PROJECTILE_ANCHOR = go.transform;
        }

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Find the fireDelegate of the root GameObject
        GameObject rootGO = transform.root.gameObject;
        if(rootGO.GetComponent<Hero>() != null)
        {
            rootGO.GetComponent<Hero>().fireDelegate += Fire;
        }
    }

    public WeaponType type
    {
        get
        {
            return (_type);
        }
        set
        {
            SetType(value);
        }
    }

    public void SetType(WeaponType wt)
    {
        _type = wt;
        if (type == WeaponType.none)
        {
            this.gameObject.SetActive(false);
            return;
        }
        else
        {
            this.gameObject.SetActive(true);
        }
        def = Main.GetWeaponDefinition(_type);
        collarRend.material.color = def.color;
        lastShotTime = 0; // You can fire immediately after _type is set.
    }

    public void Fire()
    {
        Debug.Log("Weapon Fired:" + gameObject.name);
        // If this.gameObject is inactive, return
        if (!gameObject.activeInHierarchy) return;
        // If it hasn't been enough time between shots, return
        if (Time.time - lastShotTime < def.delayBetweenShots)
        {
            return;
        }

        if(fireSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(fireSound);
        }
        Projectile p;
        Vector3 vel = Vector3.up * def.velocity;
        if (transform.up.y < 0)
        {
            vel.y = -vel.y;
        }
        switch (type)
        {
            case WeaponType.blaster:
                p = MakeProjectile();
                p.rigid.velocity = vel;
                break;

            case WeaponType.spread:
                p = MakeProjectile(); // Make middle Projectile
                p.rigid.velocity = vel;
                p = MakeProjectile(); // Make right Projectile
                p.transform.rotation = Quaternion.AngleAxis(10, Vector3.back);
                p.rigid.velocity = p.transform.rotation * vel;
                p = MakeProjectile(); // Make left Projectile
                p.transform.rotation = Quaternion.AngleAxis(-10, Vector3.back);
                p.rigid.velocity = p.transform.rotation * vel;
                break;
            case WeaponType.timeBomb:
                StartCoroutine(DeployTimeBomb());
                break;
        }
    }

    private IEnumerator DeployTimeBomb()
    {
        Projectile bomb = MakeProjectile();
        bomb.rigid.velocity = Vector3.zero;

        //Wait for explosion delay
        yield return new WaitForSeconds(def.explosionDelay);

        //Trigger explosions and damage enemies within range
        Collider[] hitColliders  = Physics.OverlapSphere(bomb.transform.position, def.explosionRadius);
        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Enemy"))
            {
                Enemy hitEnemy = hit.GetComponent<Enemy>();
                if (hitEnemy != null)
                {
                    hitEnemy.TakeDamage(def.damageOnHit); //Dealing damage to enemies
                }
            }
        }
    }
    public Projectile MakeProjectile()
    {
        GameObject go = Instantiate<GameObject>(def.projectilePrefab);
        if(transform.parent.gameObject.tag == "Hero")
        {
            go.tag = "ProjectileHero";
            go.layer = LayerMask.NameToLayer("ProjectileHero");
        }
        else
        {
            go.tag = "ProjectileEnemy";
            go.layer = LayerMask.NameToLayer("ProjectileEnemy");
        }
        go.transform.position = collar.transform.position;
        go.transform.SetParent(PROJECTILE_ANCHOR, true);
        Projectile p = go.GetComponent<Projectile>();
        p.type = type;
        lastShotTime = Time.time;
        return p;
    }
}
