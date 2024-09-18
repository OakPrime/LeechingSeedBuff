using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using R2API;
using System.Collections.Generic;

namespace LeechingSeedBuff
{

    //This is an example plugin that can be put in BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    //It's a small plugin that adds a relatively simple item to the game, and gives you that item whenever you press F2.

    //This attribute is required, and lists metadata for your plugin.
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    //This is the main declaration of our plugin class. BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    //BaseUnityPlugin itself inherits from MonoBehaviour, so you can use this as a reference for what you can declare and use in your plugin class: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class LeechingSeedBuff : BaseUnityPlugin
    {
        //The Plugin GUID should be a unique ID for this plugin, which is human readable (as it is used in places like the config).
        //If we see this PluginGUID as it is on thunderstore, we will deprecate this mod. Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "OakPrime";
        public const string PluginName = "LeechingSeedBuff";
        public const string PluginVersion = "1.1.2";

        private readonly Dictionary<string, string> DefaultLanguage = new Dictionary<string, string>();

        //The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            try
            {
                // Sets leeching seed to heal 0.2 per instance of damage
                IL.RoR2.HealthComponent.TakeDamageProcess += (il) =>
                {
                    ILCursor c = new ILCursor(il);
                    c.TryGotoNext(
                        x => x.MatchLdarg(out _),
                        x => x.MatchCallOrCallvirt<RoR2.HealthComponent>(nameof(RoR2.HealthComponent.combinedHealth)),
                        x => x.MatchStloc(out _)
                    );
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate<Action<RoR2.DamageInfo>>((damageInfo) =>
                    {
                        // Beetle queen bug probably here
                        if (damageInfo == null) { return; }
                        if (damageInfo.attacker == null) { return; }
                        CharacterBody component1 = damageInfo.attacker.GetComponent<CharacterBody>();
                        CharacterMaster master = component1?.master;
                        Inventory inventory = master?.inventory;
                        if (inventory != null && !damageInfo.procChainMask.HasProc(ProcType.HealOnHit))
                        {
                            int itemCount = inventory.GetItemCount(RoR2Content.Items.Seed);
                            // Bug not below
                            if (itemCount > 0)
                            {
                                HealthComponent component3 = component1?.GetComponent<HealthComponent>();
                                if (component3 != null && (bool)(UnityEngine.Object)component3)
                                {
                                    ProcChainMask procChainMask = damageInfo.procChainMask;
                                    procChainMask.AddProc(ProcType.HealOnHit);
                                    double num = (double)component3.Heal((float)itemCount * 0.2f, procChainMask);
                                }
                            }
                        }
                    });

                };
                this.ReplaceSeedText();
            }
            catch (Exception e)
            {
                Logger.LogError(e.Message + " - " + e.StackTrace);
            }
        }
        private void ReplaceSeedText()
        {
            this.ReplaceString("ITEM_SEED_DESC", "Proc damage <style=cIsHealing>heals</style> you for  <style=cIsHealing>1</style>" +
                " <style=cStack>(+1 per stack)</style>, <style=cIsHealing>health</style>. Also heals for <style=cIsHealing>0.2</style>" +
                " <style=cStack>(+0.2 per stack)</style>on all damage instances.");
        }

        private void ReplaceString(string token, string newText)
        {
            this.DefaultLanguage[token] = Language.GetString(token);
            LanguageAPI.Add(token, newText);
        }
    }
}
