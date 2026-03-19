using GoveKits.Runtime.Unit;
using UnityEngine;

public class Char : UnitBehaviour
{
    public override void InitAbilities()
    {
        
    }

    public override void InitAttributes()
    {
        var MaxHP = Attributes.AddState("MaxHealth", 100);
        Attributes.AddRuntime("Health", MaxHP);
    }

    public override void InitMarks()
    {
        
    }

    public override void InitReactions()
    {
        
    }
}
