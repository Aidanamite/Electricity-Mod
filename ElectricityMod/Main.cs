using HarmonyLib;
using HMLLibrary;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
using I2.Loc;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;


namespace ElectricityMod
{
    public class Main : Mod
    {
        public Item[] items = new[]
        {
            new Item()
            {
                baseIndex = 265,
                UniqueIndex = 7725,
                UniqueName = "Placeable_MotorWheel_Electric",
                localization = "Electric Engine@Allows you to go in different directions and increases your speed.",
                batteryPos = new Vector3(0.65f,0.35f,0),
                additionEdits = (x,y) =>
                {
                    DestroyImmediate(x.GetComponentInChildren<PipeSocket>(true));
                    Traverse.Create(y).Field("timer").SetValue(float.NegativeInfinity);
                    y.GetComponent<Collider>().enabled = false;
                    var b = x.gameObject.AddComponent<Block_MotorWheel_Electric>();
                    b.CopyFieldsOf(x);
                    b.ReplaceValues(x, b);
                    DestroyImmediate(x);
                    var m = b.GetComponent<MotorWheel>();
                    var e = b.gameObject.AddComponent<MotorWheel_Electric>();
                    e.CopyFieldsOf(m);
                    b.ReplaceValues(m, e);
                    DestroyImmediate(m);
                    b.motor = e;
                    var a = y.gameObject.AddComponent<BatteryAccess>();
                    a.batteryIndex = 0;
                    a.networkBehaviourID = e;
                    DestroyImmediate(a);
                    e.battery = y;
                    var t = b.GetComponentInChildren<MotorwheelFuelTank>();
                    var f = t.gameObject.AddComponent<MotorTank_Electric>();
                    f.CopyFieldsOf(t);
                    b.ReplaceValues(t, f);
                    DestroyImmediate(t);
                    Traverse.Create(e).Field("wheelRotationSpeed").SetValue(Traverse.Create(e).Field("wheelRotationSpeed").GetValue<float>() * 1.5f);
                    e.FuelTank.maxCapacity = 1;
                    e.FuelTank.harvestSetting = Tank.HarvestSetting.NONE;
                    e.FuelTank.defaultFuelToAdd = null;
                    Traverse.Create(e.FuelTank).Field("tankAcceptance").SetValue(Tank.TankAcceptance.None);
                    Traverse.Create(e.FuelTank).Field("acceptableTypes").SetValue(new List<Item_Base>());
                    Traverse.Create(e.FuelTank).Field("acceptableOutputTypes").SetValue(new List<Item_Base>());
                    return b;
                },
                cost = new[] {
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(70) }, 7),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(22) }, 5),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(178) }, 3),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(21) }, 25),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(177) }, 2)
                }
            },
            new Item()
            {
                baseIndex = 194,
                UniqueIndex = 1194,
                UniqueName = "Placeable_CookingTable_Electric",
                localization = "Electric Cookingpot@Combines base food into amazing meals.",
                batteryPos = new Vector3(-0.6f,0.2f,0),
                additionEdits = (x,y) =>
                {
                    y.GetComponent<Collider>().enabled = false;
                    var b = x.gameObject.AddComponent<Block_CookingTable>();
                    b.CopyFieldsOf(x);
                    b.ReplaceValues(x, b);
                    DestroyImmediate(x);
                    var p = b.GetComponent<CookingTable_Pot>();
                    var e = b.gameObject.AddComponent<CookingTable_Pot_Electric>();
                    e.CopyFieldsOf(p);
                    b.ReplaceValues(p, e);
                    DestroyImmediate(p);
                    b.table = e;
                    var a = y.gameObject.AddComponent<BatteryAccess>();
                    a.batteryIndex = 0;
                    a.networkBehaviourID = e;
                    DestroyImmediate(a);
                    e.battery = y;
                    var f = b.GetComponentInChildren<Fuel>();
                    f.SetFuelCount(0);
                    f.ForceSetTimer(0);
                    foreach (var s in b.GetComponentsInChildren<ParticleSystem>(true))
                        if (s && new[] { "Smoke", "SmallBlobs", "Sparks", "Fire" }.Contains(s.name))
                            foreach (var c in s.GetComponents<Component>())
                                if (!(c is Transform))
                                DestroyImmediate(c);
                    DestroyImmediate(b.GetComponentInChildren<FuelNetwork>());
                    DestroyImmediate(f);
                    return b;
                },
                cost = new[] {
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(21) }, 8),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(53) }, 8),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(70) }, 3),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(154) }, 2),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(125) }, 5),
                    new CostMultiple(new[] { ItemManager.GetItemByIndex(178) }, 2)
                }
            }
        };
        public Texture2D overlay;
        Harmony harmony;
        public static LanguageSourceData language;
        public List<Object> createdObjects = new List<Object>();
        Transform prefabHolder;
        public Battery prefab;
        public static Main instance;
        public override bool CanUnload(ref string message)
        {
            if (loaded && SceneManager.GetActiveScene().name == Raft_Network.GameSceneName && ComponentManager<Raft_Network>.Value.remoteUsers.Count > 1)
            {
                message = "Cannot unload while in a multiplayer";
                return false;
            }
            return base.CanUnload(ref message);
        }
        bool loaded = false;
        public void Awake()
        {
            if (SceneManager.GetActiveScene().name == Raft_Network.GameSceneName && ComponentManager<Raft_Network>.Value.remoteUsers.Count > 1)
            {
                Debug.LogError($"[{name}]: This cannot be loaded while in a multiplayer");
                modlistEntry.modinfo.unloadBtn.GetComponent<Button>().onClick.Invoke();
                return;
            }
            loaded = true;
            instance = this;
            prefabHolder = new GameObject("prefabHolder").transform;
            prefabHolder.gameObject.SetActive(false);
            createdObjects.Add(prefabHolder.gameObject);
            DontDestroyOnLoad(prefabHolder.gameObject);
            language = new LanguageSourceData()
            {
                mDictionary = new Dictionary<string, TermData>(),
                mLanguages = new List<LanguageData> { new LanguageData() { Code = "en", Name = "English" } }
            };
            LocalizationManager.Sources.Add(language);
            overlay = LoadImage("overlay.png");
            var bat = ItemManager.GetItemByIndex(293).settings_buildable.GetBlockPrefab(0).GetComponentInChildren<Battery>();
            prefab = Instantiate(bat, prefabHolder, false);
            prefab.transform.localScale = bat.transform.lossyScale;
            prefab.name = "BatterySlot";

            foreach (var item in items)
                if (item.baseItem = ItemManager.GetItemByIndex(item.baseIndex))
                    CreateItem(item);

            (harmony = new Harmony("com.aidanamite.ElectricityMod")).PatchAll();
            Traverse.Create(typeof(LocalizationManager)).Field("OnLocalizeEvent").GetValue<LocalizationManager.OnLocalizeCallback>().Invoke();
            Log("Mod has been loaded!");
        }



        public void OnModUnload()
        {
            if (!loaded)
                return;
            loaded = false;
            harmony?.UnpatchAll(harmony.Id);
            LocalizationManager.Sources.Remove(language);
            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().RemoveAll(x => items.Any(y => y.item?.UniqueIndex == x.UniqueIndex));
            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                Traverse.Create(q).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().RemoveAll(x => items.Any(y => y.item?.UniqueIndex == x.UniqueIndex));
            ItemManager.GetAllItems().RemoveAll(x => items.Any(y => y.item?.UniqueIndex == x.UniqueIndex));
            foreach (var b in BlockCreator.GetPlacedBlocks())
                if (b.buildableItem != null && items.Any(y => y.item?.UniqueIndex == b.buildableItem.UniqueIndex))
                    BlockCreator.RemoveBlock(b, null, false);
            foreach (var o in createdObjects)
                if (o is AssetBundle)
                    (o as AssetBundle).Unload(true);
                else
                    Destroy(o);
            createdObjects.Clear();
            Log("Mod has been unloaded!");
        }
        public Texture2D LoadImage(string filename, bool leaveReadable = true)
        {
            var t = new Texture2D(0, 0);
            t.LoadImage(GetEmbeddedFileBytes(filename), !leaveReadable);
            if (leaveReadable)
                t.Apply();
            createdObjects.Add(t);
            return t;
        }
        Dictionary<Material, Material> editedMaterials = new Dictionary<Material, Material>();
        void CreateItem(Item item)
        {
            item.item = item.baseItem.Clone(item.UniqueIndex, item.UniqueName);
            createdObjects.Add(item.item);
            Traverse.Create(item.item.settings_buildable).Field("mirroredVersion").SetValue(null);
            var t = item.baseItem.settings_Inventory.Sprite.texture.GetReadable(item.baseItem.settings_Inventory.Sprite.rect);
            t.Edit();
            var t2 = new Texture2D(t.width, t.height, t.format, false);
            t2.SetPixels(t.GetPixels(0));
            t2.Apply(true, true);
            item.item.settings_Inventory.Sprite = t2.ToSprite();
            Destroy(t);
            item.item.settings_Inventory.LocalizationTerm = "Item/" + item.item.UniqueName;
            if (item.cost != null)
                item.item.settings_recipe.NewCost = item.cost;
            language.mDictionary[item.item.settings_Inventory.LocalizationTerm] = new TermData() { Languages = new[] { item.localization } };
            var p = item.item.settings_buildable.GetBlockPrefabs().ToArray();
            for (int i = 0; i < p.Length; i++)
            {
                //RAPI.SendNetworkMessage(new Message_Battery(Messages.Battery_Insert, RAPI.GetLocalPlayer().Network.NetworkIDManager, new CSteamID(0), BlockCreator.GetPlacedBlocks().First(x => x.buildableItem?.UniqueIndex == 7725).GetComponent<MonoBehaviour_ID_Network>().ObjectIndex, 5, 0, 176), target: Target.All);
                p[i] = Instantiate(p[i], prefabHolder);
                p[i].name = item.UniqueName + (p[i].dpsType == DPS.Default || p.Length == 1 ? "" : $"_{p[i].dpsType}");
                var r = p[i].GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < r.Length; j++)
                    if (editedMaterials.TryGetValue(r[j].sharedMaterial, out var mat))
                        r[j].sharedMaterial = mat;
                    else
                    {
                        mat = Instantiate(r[j].sharedMaterial);
                        createdObjects.Add(mat);
                        if (mat.HasProperty("_Diffuse"))
                        {
                            var texture = (mat.GetTexture("_Diffuse") as Texture2D).GetReadable();
                            var c = texture.GetPixels(0);
                            for (int k = 0; k < c.Length; k++)
                                c[k] = new Color(Mathf.Sqrt(c[k].r), Mathf.Sqrt(c[k].g), Mathf.Sqrt(c[k].b), c[k].a);
                            texture.SetPixels(c,0);
                            texture.Apply(true);
                            mat.SetTexture("_Diffuse", texture);
                        }
                        r[j].sharedMaterial = mat;
                    }
                p[i].ReplaceValues(item.baseItem, item.item);
                var b = Instantiate(prefab, p[i].transform, false);
                b.name = prefab.name;
                b.transform.localScale = p[i].transform.InverseTransformDirection(prefab.transform.localScale);
                b.transform.localPosition = item.batteryPos;
                b.transform.localRotation = Quaternion.Euler(item.batteryRot);
                var nb = item.additionEdits?.Invoke(p[i],b);
                if (nb)
                    p[i] = nb;
            }
            Traverse.Create(item.item.settings_buildable).Field("blockPrefabs").SetValue(p);

            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockQuadType>())
                if (q.AcceptsBlock(item.baseItem))
                    Traverse.Create(q).Field("acceptableBlockTypes").GetValue<List<Item_Base>>().Add(item.item);
            foreach (var q in Resources.FindObjectsOfTypeAll<SO_BlockCollisionMask>())
                if (q.IgnoresBlock(item.baseItem))
                    Traverse.Create(q).Field("blockTypesToIgnore").GetValue<List<Item_Base>>().Add(item.item);
            RAPI.RegisterItem(item.item);
            foreach (var i in ItemManager.GetAllItems().FindAll(x => x.settings_recipe.BlueprintItem?.UniqueIndex == item.baseIndex || (x.settings_recipe.ExtraBlueprintItems?.Any(y => y?.UniqueIndex == item.baseIndex) ?? false)))
                Traverse.Create(i.settings_recipe).Field("extraBlueprintItems").SetValue(i.settings_recipe.ExtraBlueprintItems.AddToArray(item.item));
        }
        public static T CreateObject<T>() => (T)FormatterServices.GetUninitializedObject(typeof(T));
    }

    public class Item
    {
        public int baseIndex;
        public Item_Base baseItem;
        public int UniqueIndex;
        public string UniqueName;
        public Item_Base item;
        public string localization;
        public Vector3 batteryPos;
        public Vector3 batteryRot;
        public Func<Block,Battery,Block> additionEdits;
        public CostMultiple[] cost;
    }
    class BatteryAccess : MonoBehaviour
    {
        static FieldInfo _networkBehaviourID = typeof(Battery).GetField("networkBehaviourID", ~BindingFlags.Default);
        static FieldInfo _batteryIndex = typeof(Battery).GetField("batteryIndex", ~BindingFlags.Default);
        public MonoBehaviour_ID_Network networkBehaviourID
        {
            get => (MonoBehaviour_ID_Network)_networkBehaviourID.GetValue(GetComponent<Battery>());
            set => _networkBehaviourID.SetValue(GetComponent<Battery>(), value);
        }
        public int batteryIndex
        {
            get => (int)_batteryIndex.GetValue(GetComponent<Battery>());
            set => _batteryIndex.SetValue(GetComponent<Battery>(), value);
        }
    }

    public class Block_MotorWheel_Electric : Block
    {
        public MotorWheel_Electric motor;
        public override RGD_Block GetBlockCreationData() => (RGD_Block)motor.Serialize_Save();
    }
    public class MotorWheel_Electric : MotorWheel
    {
        public bool hasBeenPlaced = false;
        public Battery battery;
        static FieldInfo _timePerFuel = typeof(MotorWheel).GetField("timePerFuel", ~BindingFlags.Default);
        public float timePerFuel
        {
            get => (float)_timePerFuel.GetValue(this);
            set => _timePerFuel.SetValue(this,value);
        }
        new void Awake()
        {
            battery.gameObject.AddComponent<BatteryStateUpdate>().OnUpdate += x=> FuelTank.SetTankAmount(x.NormalizedBatteryLeft);
            base.Awake();
        }
        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            if (msg is Message_Battery)
                return battery.OnBatteryMessage(msg);
            if (msg is Message_Fuel)
                return false;
            return base.Deserialize(msg, remoteID);
        }
        float timePassed = 0;
        new void Update()
        {
            base.Update();
            if (hasBeenPlaced && MotorState && RaftVelocityManager.MotorWheelWeightStrength != WeightStrength.NotStrongEnough)
            {
                timePassed += Time.deltaTime;
                if (timePassed >= timePerFuel)
                {
                    battery.RemoveBatteryUsesNetworked((int)(timePassed / timePerFuel));
                    timePassed %= timePerFuel;
                }
            }
        }
        public void OnBlockPlaced()
        {
            hasBeenPlaced = true;
            battery.GetComponent<Collider>().enabled = true;
            NetworkIDManager.AddNetworkID(this,typeof(MotorWheel));
            BaseOnBlockPlaced();
        }
        static MethodInfo _OnBlockPlaced = typeof(MotorWheel).GetMethod("OnBlockPlaced", ~BindingFlags.Default);
        void BaseOnBlockPlaced() => _OnBlockPlaced.Invoke(this, new object[0]);
        public override RGD Serialize_Save()
        {
            var r = Main.CreateObject<RGD_Block_ElectricPurifier>();
            r.CopyFieldsOf(new RGD_Block(RGDType.Block, GetComponent<Block>()));
            r.rgdBattery = new RGD_Battery(battery);
            r.objectIndex = (engineSwitchOn ? 1u : 0) + (rotatesForward ? 2u : 0);
            return r;
        }
        protected override void OnDestroy()
        {
            NetworkIDManager.RemoveNetworkID(this, typeof(MotorWheel));
            base.OnDestroy();
        }
    }
    public class MotorTank_Electric : MotorwheelFuelTank
    {
        public override bool ModifyTank(Network_Player player, float amount, Item_Base itemType = null)
        {
            return false;
        }
    }

    public class Block_CookingTable : Block
    {
        public CookingTable table;
        public override RGD_Block GetBlockCreationData() => (RGD_Block)table.Serialize_Save();
    }

    public class CookingTable_Pot_Electric : CookingTable
    {
        public Battery battery;

        protected override void Update()
        {
            if (!hasBeenPlaced)
            {
                return;
            }
            base.Update();
            HandleAnimations();
        }

        protected override void OnBlockPlaced()
        {
            base.OnBlockPlaced();
            battery.GetComponent<Collider>().enabled = true;
            battery.On = false;
        }

        protected override bool IsCooking()
        {
            return base.IsCooking() && battery.CanGiveElectricity && battery.On;
        }

        protected override bool StartCooking(SO_CookingTable_Recipe recipe)
        {
            bool flag = base.StartCooking(recipe);
            if (flag)
                battery.On = true;
            return flag;
        }

        protected override void FinishCooking()
        {
            base.FinishCooking();
            battery.On = false;
        }

        public override bool CanStartCooking()
        {
            return base.CanStartCooking() && battery.CanGiveElectricity;
        }

        private void HandleAnimations()
        {
            if (!battery.On && battery.CanGiveElectricity && CurrentRecipe && Portions == 0U)
            {
                battery.On = true;
                anim.SetBool("Cooking", true);
                return;
            }
            if (!battery.On && CurrentRecipe && Portions == 0U)
            {
                anim.SetBool("Stalling", true);
                return;
            }
            if (battery.On)
            {
                anim.SetBool("Stalling", false);
            }
        }

        public override RGD Serialize_Save()
        {
            if (hasBeenPlaced)
            {
                var r = Main.CreateObject<RGD_CookingTable_Juicer>();
                r.CopyFieldsOf(new RGD_CookingPot(this));
                r.rgdBattery = new RGD_Battery(battery);
                return new RGD_Block_CookingPot(GetComponent<Block>(), r);
            }
            return base.Serialize_Save();
        }

        public override void RestoreCookingPot(RGD_CookingPot rgdCookingPot)
        {
            if (rgdCookingPot == null)
                return;
            base.RestoreCookingPot(rgdCookingPot);
            var rgd = rgdCookingPot as RGD_CookingTable_Juicer;
            if (rgd?.rgdBattery != null)
                rgd.rgdBattery.RestoreBattery(battery);
        }

        public override bool Deserialize(Message_NetworkBehaviour msg, CSteamID remoteID)
        {
            if (msg is Message_Battery)
                return battery.OnBatteryMessage(msg);
            return base.Deserialize(msg, remoteID);
        }
    }

    public class BatteryStateUpdate : MonoBehaviour
    {
        public Action<Battery> OnUpdate;
    }

    [HarmonyPatch(typeof(Battery),"UpdateBatteryHolderLights")]
    static class Patch_BatteryUpdateDisplay
    {
        static void Postfix(Battery __instance)
        {
            __instance.GetComponent<BatteryStateUpdate>()?.OnUpdate?.Invoke(__instance);
        }
    }
    [HarmonyPatch(typeof(RemovePlaceables), "ReturnItemsFromBlock")]
    static class Patch_ReturnItemsFromBlock
    {
        static void Prefix(Block block, Network_Player player, bool giveItems)
        {
            if (giveItems && (player?.IsLocalPlayer ?? false))
            {
                if (block.GetComponent<MotorWheel_Electric>())
                {
                    var b = block.GetComponent<MotorWheel_Electric>().battery;
                    if (!b.BatterySlotIsEmpty)
                        player.Inventory.AddItem(Traverse.Create(b).Field("batteryInstance").GetValue<ItemInstance>());
                }
                if (block.GetComponent<CookingTable_Pot_Electric>())
                {
                    var b = block.GetComponent<CookingTable_Pot_Electric>().battery;
                    if (!b.BatterySlotIsEmpty)
                        player.Inventory.AddItem(Traverse.Create(b).Field("batteryInstance").GetValue<ItemInstance>());
                }
            }
        }
    }
    [HarmonyPatch(typeof(RGD_Block_ElectricPurifier), "RestoreBlock")]
    static class Patch_Restore_ElectricPurifier
    {
        static void Postfix(RGD_Block_ElectricPurifier __instance, Block block)
        {
            var motor = block.GetComponent<MotorWheel_Electric>();
            if (motor)
            {
                __instance.rgdBattery?.RestoreBattery(motor.battery);
                var r = Main.CreateObject<RGD_MotorWheel>();
                r.isEngineTurnedOn = (__instance.objectIndex & 1) != 0;
                r.isRotatingForward = (__instance.objectIndex & 2) != 0;
                motor.RestoreMotor(r);
            }
        }
    }
    [HarmonyPatch]
    static class Patch_CreateMotorRestore
    {
        static MethodBase TargetMethod() => typeof(Message_MotorWheelCreate).GetConstructors()[0];
        static void Prefix(ref MotorWheel[] motors)
        {
            var l = motors.ToList();
            if (l.RemoveAll(x => x is MotorWheel_Electric) > 0)
                motors = l.ToArray();
        }
    }

    [HarmonyPatch(typeof(LanguageSourceData), "GetLanguageIndex")]
    static class Patch_GetLanguageIndex
    {
        static void Postfix(LanguageSourceData __instance, ref int __result)
        {
            if (__result == -1 && __instance == Main.language)
                __result = 0;
        }
    }
    [HarmonyPatch(typeof(ModManagerPage), "ShowModInfo")]
    class Patch_ShowModInfo
    {
        static void Postfix(ModData md)
        {
            if (md.modinfo.mainClass && md.modinfo.mainClass.GetType() == typeof(Main))
                ModManagerPage.modInfoObj.transform.Find("MakePermanent").gameObject.SetActive(true);
        }
    }

    static class ExtentionMethods
    {
        public static Item_Base Clone(this Item_Base source, int uniqueIndex, string uniqueName)
        {
            Item_Base item = ScriptableObject.CreateInstance<Item_Base>();
            item.Initialize(uniqueIndex, uniqueName, source.MaxUses);
            item.settings_buildable = source.settings_buildable.Clone();
            item.settings_consumeable = source.settings_consumeable.Clone();
            item.settings_cookable = source.settings_cookable.Clone();
            item.settings_equipment = source.settings_equipment.Clone();
            item.settings_Inventory = source.settings_Inventory.Clone();
            item.settings_recipe = source.settings_recipe.Clone();
            item.settings_usable = source.settings_usable.Clone();
            return item;
        }
        public static void SetRecipe(this Item_Base item, CostMultiple[] cost, CraftingCategory category = CraftingCategory.Resources, int amountToCraft = 1, bool learnedFromBeginning = false, string subCategory = null, int subCatergoryOrder = 0)
        {
            Traverse recipe = Traverse.Create(item.settings_recipe);
            recipe.Field("craftingCategory").SetValue(category);
            recipe.Field("amountToCraft").SetValue(amountToCraft);
            recipe.Field("learnedFromBeginning").SetValue(learnedFromBeginning);
            recipe.Field("subCategory").SetValue(subCategory);
            recipe.Field("subCatergoryOrder").SetValue(subCatergoryOrder);
            item.settings_recipe.NewCost = cost;
        }

        public static void CopyFieldsOf(this object value, object source)
        {
            var t1 = value.GetType();
            var t2 = source.GetType();
            while (!t1.IsAssignableFrom(t2))
                t1 = t1.BaseType;
            while (t1 != typeof(Object) && t1 != typeof(object))
            {
                foreach (var f in t1.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                        f.SetValue(value, f.GetValue(source));
                t1 = t1.BaseType;
            }
        }

        public static void ReplaceValues(this Component value, object original, object replacement, int serializableLayers = 0)
        {
            foreach (var c in value.GetComponentsInChildren<Component>())
                (c as object).ReplaceValues(original, replacement, serializableLayers);
        }
        public static void ReplaceValues(this GameObject value, object original, object replacement, int serializableLayers = 0)
        {
            foreach (var c in value.GetComponentsInChildren<Component>())
                (c as object).ReplaceValues(original, replacement, serializableLayers);
        }

        public static void ReplaceValues(this object value, object original, object replacement, int serializableLayers = 0)
        {
            if (value == null)
                return;
            var t = value.GetType();
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic)
                    {
                        if (f.GetValue(value) == original || (f.GetValue(value)?.Equals(original) ?? false))
                            try
                            {
                                f.SetValue(value, replacement);
                            } catch { }
                        else if (f.GetValue(value) is IList)
                        {
                            var l = f.GetValue(value) as IList;
                            for (int i = 0; i < l.Count; i++)
                                if (l[i] == original || (l[i]?.Equals(original) ?? false))
                                    try
                                    {
                                        l[i] = replacement;
                                    } catch { }

                        }
                        else if (serializableLayers > 0 && (f.GetValue(value)?.GetType()?.IsSerializable ?? false))
                            f.GetValue(value).ReplaceValues(original, replacement, serializableLayers - 1);
                    }
                t = t.BaseType;
            }
        }

        public static bool HasFieldWithValue(this object obj, object value)
        {
            var t = obj.GetType();
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic && f.GetValue(obj).Equals(value))
                        return true;
                t = t.BaseType;
            }
            return false;
        }
        public static bool HasFieldValueMatch<T>(this object obj, Predicate<T> predicate)
        {
            var t = obj.GetType();
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic && f.GetValue(obj) is T && (predicate == null || predicate((T)f.GetValue(obj))))
                        return true;
                t = t.BaseType;
            }
            return false;
        }
        public static FieldInfo[] FindFieldsMatch<T>(this object obj, Predicate<T> predicate)
        {
            var fs = new List<FieldInfo>();
            var t = obj.GetType();
            while (t != typeof(Object) && t != typeof(object))
            {
                foreach (var f in t.GetFields(~BindingFlags.Default))
                    if (!f.IsStatic && typeof(T).IsAssignableFrom(f.FieldType) && (predicate == null || predicate((T)f.GetValue(obj))))
                        fs.Add(f);
                t = t.BaseType;
            }
            return fs.ToArray();
        }

        public static Sprite ToSprite(this Texture2D texture, Rect? rect = null, Vector2? pivot = null)
        {
            var s = Sprite.Create(texture, rect ?? new Rect(0, 0, texture.width, texture.height), pivot ?? new Vector2(0.5f, 0.5f));
            Main.instance.createdObjects.Add(s);
            return s;
        }


        public static Texture2D GetReadable(this Texture2D source, Rect? copyArea = null, RenderTextureFormat format = RenderTextureFormat.Default, RenderTextureReadWrite readWrite = RenderTextureReadWrite.Default, TextureFormat? targetFormat = null, bool mipChain = true)
        {
            var temp = RenderTexture.GetTemporary(source.width, source.height, 0, format, readWrite);
            Graphics.Blit(source, temp);
            temp.filterMode = FilterMode.Point;
            var prev = RenderTexture.active;
            RenderTexture.active = temp;
            var area = copyArea ?? new Rect(0, 0, temp.width, temp.height);
            area.y = temp.height - area.y - area.height;
            var texture = new Texture2D((int)area.width, (int)area.height, targetFormat ?? TextureFormat.RGBA32, mipChain);
            texture.ReadPixels(area, 0, 0);
            texture.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(temp);
            Main.instance.createdObjects.Add(texture);
            return texture;
        }
        public static void Edit(this Texture2D baseImg)
        {
            var w = baseImg.width - 1;
            var h = baseImg.height - 1;
            for (var x = 0; x <= w; x++)
                for (var y = 0; y <= h; y++)
                    baseImg.SetPixel(x, y, baseImg.GetPixel(x, y).Overlay(Main.instance.overlay.GetPixelBilinear((float)x / w, (float)y / h)));
            baseImg.Apply();
        }
        public static Color Overlay(this Color a, Color b)
        {
            if (a.a <= 0)
                return b;
            if (b.a <= 0)
                return a;
            var r = b.a / (b.a + a.a * (1 - b.a));
            float Ratio(float aV, float bV) => bV * r + aV * (1 - r);
            return new Color(Ratio(a.r, b.r), Ratio(a.g, b.g), Ratio(a.b, b.b), b.a + a.a * (1 - b.a));
        }
        public static Vector2 Rotate(this Vector2 value, float angle)
        {
            if (angle % 360 == 0)
                return value;
            var l = value.magnitude;
            var a = Mathf.Atan2(value.x, value.y) + angle / 180 * Mathf.PI;
            if (value.y < 0)
                a += Mathf.PI;
            return new Vector2(Mathf.Sin(a) * l, Mathf.Cos(a) * l);
        }

        public static Vector3 Multiply(this Vector3 value, Vector3 scale) => new Vector3(value.x * scale.x, value.y * scale.y, value.z * scale.z);
    }
}