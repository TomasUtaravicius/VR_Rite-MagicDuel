﻿using Photon.Pun;
using UnityEngine;

public enum SpellType { None, Projectile, Shield }

public class Spell : MonoBehaviourPun, IPunObservable
{
    protected string spellName = "";

    [SerializeField] private bool requiresHeldCast = false;

    /// <summary> Amount of health the spell has </summary>
    private float health = 10;

    /// <summary> Amount of damage the spell deals when touching a overlapped gameObject </summary>
    public float damage = 10;

    /// <summary> Lifetime of the spell after being cast. Lifetime of 0 is infinite </summary>
    private float lifeTime = 0;

    public float manaCost = 0;

    public float spellSpeed = 10;

    /// <summary> A held cast spell requires the caster to hold the spell to keep it active and release to deactivate it </summary>
    public bool RequiresHeldCast { get => requiresHeldCast;}

   

    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        throw new System.NotImplementedException();
    }



    public virtual void FireSpell()
    { }


    public void SetSpellAttributes(string spellName, float health = 10, float damage = 10, float lifeTime = 0, float spellSpeed = 0)
    {
        gameObject.name = spellName;
        this.spellName = spellName;
        this.health = health;
        this.damage = damage;
        this.lifeTime = lifeTime;
        this.spellSpeed = spellSpeed;
    }

    
    public virtual void SetSpellVisuals(bool shouldTintColor, Color tintColor)
    {
        if (shouldTintColor) TintSpellColors(tintColor);
    }

    protected virtual void TintSpellColors(Color tintColor)
    {
        //throw new NotImplementedException();
    }
}