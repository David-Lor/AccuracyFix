/* ACCURACY FIX GTA V SCRIPT - v1.1
 * by EnforcerZhukov - www.enforcerzhukov.xyz */
using GTA;
using GTA.Native;
using GTA.Math;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

public class AccuracyFix : Script
{
    //General variables
    private int ac1 = 0, ac2 = 50, accumode = 0, shootrate = 500;
    private bool accuOn = true, shootrateOn = true, damageOn = false, aimDebug = false;
    private float damage = 1.0f;
    private Dictionary<Ped, int> accuApplied = new Dictionary<Ped, int>();
    private List<Ped> accuApplied_Remove = new List<Ped>();
    private Random r = new Random((int)DateTime.Now.Ticks);

    public AccuracyFix()
    {
        Tick += OnTick;
        //KeyUp += OnKeyUp;

        //Get INI config file variables
        ScriptSettings config = ScriptSettings.Load("scripts\\AccuracyFix.ini");
        accuOn = config.GetValue<bool>("ACCURACY", "GlobalAccuracyModule", true);
        accumode = config.GetValue<int>("ACCURACY", "AccuracyMode", 0);
        ac1 = config.GetValue<int>("ACCURACY", "Accu1", 0);
        ac2 = config.GetValue<int>("ACCURACY", "Accu2", 35);
        damageOn = config.GetValue<bool>("DAMAGE", "GlobalDamageModule", false);
        damage = config.GetValue<float>("DAMAGE", "Damage", 1.0f);
        shootrateOn = config.GetValue<bool>("SHOOTRATE", "GlobalShootRate", false);
        shootrate = config.GetValue<int>("SHOOTRATE", "ShootRate", 500);
        aimDebug = config.GetValue<bool>("DEBUG", "AimDebug", false);
        int interv = config.GetValue<int>("DEBUG", "Interval", 50);
        Interval = interv;

        //Fix input ints.
            //A)accu1 & accu2 levels: accu1 must be lower than accu2. Otherwise: invert the values
            if ( (accumode == 1 || accumode == 2 || accumode == 3) && (ac1 > ac2) ) {
                int tmpAC1 = ac1;
                ac1 = ac2;
                ac2 = tmpAC1;
            }
            //B)Levels can't be negative or higher than 100.
            if (ac1 > 100) ac1 = 100;
            if (ac1 < 0) ac1 = 0;
            if (ac2 > 100) ac2 = 100;
            if (ac2 < 0) ac2 = 0;
            //C)AccuMode can't be negative or higher than 4.
            if (accumode > 3 || accumode < 0) accumode = 0;
    }

    private void OnTick(object sender, EventArgs e)
    {

        /*if ( ( (accu != prevGlobalAccu) && accuOn ) || ( (shootrate != prevShootRate) && shootrateOn) ) { //when GlobalAccu value has changed (with global mod. On)
            changed.Clear(); //this should force ALL peds to change their accu/health again!
            //report("accuracy", accu);
        }*/

        foreach (Ped p in World.GetAllPeds()) { //get all the peds not included on the list
            if (!p.IsPlayer && p.IsHuman && p.IsAlive && p.Exists()) { //only work on non-player, human, alive, existing peds.
                if (accuOn && !accuApplied.ContainsKey(p)) { //Accuracy work
                    int pa = 0;
                    switch (accumode) {
                        case 1: //random-classic (each ped will receive a random accu between ac1~ac2)
                            pa = r.Next(ac1, ac2 + 1);
                            break;
                        case 2: //max-min values: uses vanilla ped accu but can't be higher or lower than ac1 & ac1
                            int paoC2 = p.Accuracy;
                            if (paoC2 < ac1) pa = ac1;
                            else if (paoC2 > ac2) pa = ac2;
                            else pa = paoC2;
                            break;
                        case 3: //proportional max-min values: accu scaling, proportional with the vanilla value 0~100 to ac1~ac2
                            float f1 = ( (float)p.Accuracy) / 100f;
                            float f2 = 1 - f1;
                            float paF = (ac1 * f2) + (ac2 * f1);
                            pa = Convert.ToInt32(paF);
                            break;
                        default: //classic (same accu for all the peds)
                            pa = ac1;
                            break;
                    }
                    p.Accuracy = pa;
                    accuApplied.Add(p, pa);
                }
                if (shootrateOn) p.ShootRate = shootrate; //Shootrate work
            }
        }

        foreach (KeyValuePair<Ped, int> k in accuApplied) { //process the modified peds constantly
            Ped p = k.Key;
            int accu = k.Value;
            if (p.IsDead || !p.Exists()) accuApplied_Remove.Add(p); //remove from list if ped is dead or no longer exist
            else p.Accuracy = accu; //re-apply the accuracy constantly if is still alive/exist
        }

        foreach (Ped p in accuApplied_Remove) accuApplied.Remove(p); //Remove dead/non-existing peds from list
        accuApplied_Remove.Clear();

        //GLOBAL DAMAGE
        if (damageOn) Function.Call(Hash.SET_AI_WEAPON_DAMAGE_MODIFIER, damage);

        if (aimDebug && Game.Player.IsAiming) {
            Entity pEnt = Game.Player.GetTargetedEntity();
            if ( Function.Call<bool>(Hash.IS_ENTITY_A_PED, pEnt) ) {
                Ped p = new Ped(pEnt.Handle);
                bool onList = false;
                if (accuApplied.ContainsKey(p)) onList = true;
                UI.ShowSubtitle("AccuracyFix dbg - aimed ped accu = " + p.Accuracy.ToString() + " (mode=" + accumode.ToString() + ") - on accu list: " + onList.ToString(), 400);
            }
        }
    }
}