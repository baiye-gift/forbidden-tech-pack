// Executes the actual compiler/coolant adapters against a small deterministic host.
// The empty-list indexing and completion/start boundary reproduce U59 ComplexFabricator.
// This host intentionally does not claim to simulate Unity, pipe flow, or serialization.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Compiler;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Save;
using ForbiddenTechnologyPack.Game.Recipes;
using ForbiddenTechnologyPack.Game.Elements;
using ForbiddenTechnologyPack.Game.Buildings.MassEnergy;

namespace UnityEngine {
    public static class Mathf { public static float Clamp(float v,float min,float max) { return Math.Max(min,Math.Min(max,v)); } }
    public struct Vector3 { public static Vector3 zero; }
    public class GameObject {
        public readonly Dictionary<Type, object> Components = new Dictionary<Type, object>();
        public T GetComponent<T>() where T:class { return Components.Values.OfType<T>().FirstOrDefault(); }
        public void Add<T>(T v) { Components[typeof(T)] = v; }
    }
}
namespace Klei { public static class SimUtil { public struct DiseaseInfo {} } }
namespace KSerialization {
    public enum MemberSerialization { OptIn }
    public class SerializationConfigAttribute : Attribute { public SerializationConfigAttribute(MemberSerialization v) {} }
    public class SerializeAttribute : Attribute {}
}
namespace HarmonyLib {
    public class HarmonyPatch : Attribute { public HarmonyPatch() {} public HarmonyPatch(Type t, string m) {} }
    public static class AccessTools { public static MethodInfo Method(Type t,string n) { return t.GetMethod(n,BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic); } }
}
public class MyCmpGetAttribute : Attribute {}
public interface ISim1000ms { void Sim1000ms(float dt); }
public interface ISim200ms { void Sim200ms(float dt); }
public interface ISaveLoadable {}
public class KMonoBehaviour {
    public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
    public T GetComponent<T>() where T:class { return gameObject.GetComponent<T>(); }
    public T[] GetComponents<T>() where T:class { return gameObject.Components.Values.OfType<T>().ToArray(); }
    protected virtual void OnSpawn() {}
    protected virtual void OnCleanUp() {}
    public void Subscribe(int h, Action<object> a) {}
    public void Unsubscribe(int h, Action<object> a) {}
}
public class GameClock {
    public static GameClock Instance=new GameClock(); private double time;
    public double Time { get { return time; } set {
        if(Math.Floor(value/.2d)!=Math.Floor(time/.2d)) SimulationTime.BeginPowerInterval();
        time=value;
    } }
    public float GetTime() { return (float)Time; }
    public int GetCycle() { return (int)Math.Floor(Time/600); }
    public float GetTimeSinceStartOfCycle() { return (float)(Time-GetCycle()*600d); }
}
public interface IEnergyConsumer {}
public class EnergyConsumer : KMonoBehaviour, IEnergyConsumer {
    public float WattsNeededWhenActive=8000;
    public static Operational.Flag PoweredFlag=new Operational.Flag("powered",Operational.Flag.Type.Requirement);
    public int GetInstanceID() { return GetHashCode(); }
    public void SetConnectionStatus(CircuitManager.ConnectionStatus status) {
        GetComponent<Operational>().SetFlag(PoweredFlag,status==CircuitManager.ConnectionStatus.Powered);
    }
}
public class CircuitManager { public enum ConnectionStatus { Powered, Unpowered, Disconnected } }
public class Operational {
    public class Flag { public enum Type { Requirement } public Flag(string n, Type t) {} }
    readonly Dictionary<Flag,bool> flags = new Dictionary<Flag,bool>();
    public bool IsActive = true;
    private bool enabled=true;
    public bool IsOperational { get { return enabled && flags.Values.All(v=>v); } set { enabled=value; } }
    public bool GetFlag(Flag f) { bool v; return !flags.TryGetValue(f,out v) || v; }
    public void SetFlag(Flag f, bool v) { flags[f] = v; if(!IsOperational)SetActive(false); }
    public void SetActive(bool v) { IsActive = v; }
}
public struct Tag { public string Name; public Tag(string n) { Name = n; } }
public enum SimHashes { Dirt = 1, Sand = 2, Proto = 3 }
public static class Hash { public static int SDBMLower(string s) { return s == "Dirt" ? 1 : s == "Sand" ? 2 : 3; } }
public static class PICKUPABLETUNING { public const float MINIMUM_PICKABLE_AMOUNT = .001f; }
public enum GameHashes { DeconstructComplete, OperationalChanged }
public static class KAnim { public enum PlayMode { Loop } }
public class KBatchedAnimController { public string Last; public void Play(string name,KAnim.PlayMode mode) { Last=name; } }
public class RadiationEmitter { public bool Emitting; public void SetEmitting(bool v) { Emitting=v; } }
public class Generator { public float Capacity=96000,JoulesAvailable,Generated; public void GenerateJoules(float j) { JoulesAvailable=Math.Min(Capacity,JoulesAvailable+j);Generated+=j; } }
public struct CellOffset { public CellOffset(int x,int y) {} }
public class Building { public int NaturalBuildingCell() { return 0; } }
public static class Grid { public static int OffsetCell(int c,CellOffset o) { return c; } }
public class Element { public Tag tag = new Tag("Water"); public bool IsLiquid = true; public bool IsSolid; public float highTemp = 373.15f, specificHeatCapacity = 4f; }
public class PrimaryElement { public UnityEngine.GameObject gameObject; public float Mass = 10f, Temperature = 293.15f; public Element Element = new Element(); public SimHashes ElementID; }
public class Pickupable { public PrimaryElement PrimaryElement; }
public class Flow {
    public Pickupable Item;
    public struct Handle { public bool Valid; public bool IsValid() { return Valid; } }
    public struct Contents { public Handle pickupableHandle; }
    public Contents GetContents(int c) { return new Contents{pickupableHandle=new Handle{Valid=Item!=null}}; }
    public Pickupable GetPickupable(Handle h) { return Item; }
}
public class Game { public static Game Instance = new Game(); public Flow solidConduitFlow = new Flow(); }
public class SolidConduitConsumer : KMonoBehaviour { public bool IsConnected; }
public class SolidConduitDispenser : KMonoBehaviour { public SimHashes[] elementFilter; }
public class ConduitConsumer { public bool On = true; public void SetOnState(bool v) { On=v; } }
public class ConduitDispenser : KMonoBehaviour { public Storage storage; public bool On; public void SetOnState(bool v) { On=v; } }
public class StatusItem {
    public enum IconType { Custom }
    public StatusItem(string id,string n,string t,string i,IconType it,NotificationType nt,bool b,string o) {}
}
public enum NotificationType { BadMinor }
public static class OverlayModes { public static class LiquidConduits { public static string ID="liquid"; } }
public class KSelectable { public void ToggleStatusItem(StatusItem s,bool v,object data) {} }
public static class STRINGS {
    public static class UI {
        public static class MATTER { public static string COOLANT_DRAIN="draining",COOLANT_EMPTY="empty",COOLANT_HOT="hot",COOLANT_HELP="{0} {1}",HEATED_BUFFER="{0}",OUTPUT_FULL="full",NOT_OPERATIONAL="disabled",CHOOSE="choose"; }
        public static class MASSENERGY { public static string FUEL_EMPTY="fuel",POWER_FULL="power full",IGNITING="ignition {0}",REACTING="reacting",READY="ready",CONDENSING="condensing {0}",REACTOR_DETAIL="{0} {1} {2} {3} {4}",CONDENSER_DETAIL="{0} {1}"; }
    }
}
public static class GameUtil { public static string GetFormattedTemperature(float k) { return k.ToString(); } }
public class Storage {
    public List<UnityEngine.GameObject> items = new List<UnityEngine.GameObject>();
    public float Mass, Capacity=12000;
    public float MassStored() { return items.Count>0 ? items.Sum(i=>i.GetComponent<PrimaryElement>().Mass) : Mass; }
    public float RemainingCapacity() { return Capacity-MassStored(); }
    public float GetAmountAvailable(Tag t) { return Mass; }
    public void ConsumeAndGetDisease(Tag t,float request,out float consumed,out Klei.SimUtil.DiseaseInfo disease,out float temperature) { consumed=Math.Min(Mass,request);Mass-=consumed;disease=new Klei.SimUtil.DiseaseInfo();temperature=293.15f; }
    public void DropAll(bool a,bool b,UnityEngine.Vector3 v,bool c,object d) { Mass=0; items.Clear(); }
    public bool Transfer(UnityEngine.GameObject item,Storage to,bool a,bool b) { if(!items.Remove(item)) return false;to.items.Add(item);return true; }
    public void Transfer(Storage to,bool a,bool b) { foreach(var item in items.ToArray()) Transfer(item,to,a,b); }
    public float Transfer(Storage to,Tag tag,float amount,bool a,bool b) {
        if(items.Count==0)return 0;
        var item=items[0];var p=item.GetComponent<PrimaryElement>();amount=Math.Min(amount,p.Mass);
        if(amount>=p.Mass)Transfer(item,to,a,b);
        else {p.Mass-=amount;var split=new UnityEngine.GameObject();split.Add(new PrimaryElement{gameObject=split,Mass=amount,Temperature=p.Temperature,Element=p.Element});to.items.Add(split);}
        return amount;
    }
}
public class ComplexRecipe {
    public string id; public float time=30;
    public RecipeElement[] ingredients,results;
    public class RecipeElement { public Tag material; public float amount; }
}
public class ComplexFabricator : KMonoBehaviour {
    public ComplexRecipe CurrentWorkingOrder, NextOrder;
    public float OrderProgress;
    public Storage inStorage=new Storage(), buildStorage=new Storage(), outStorage=new Storage();
    protected Operational operational=new Operational();
    public ComplexRecipe[] Recipes = new ComplexRecipe[0];
    public Dictionary<string,int> Queues=new Dictionary<string,int>();
    public int BaseTicks, Completed;
    public ComplexRecipe[] GetRecipes() { return Recipes; }
    public int GetRecipeQueueCount(ComplexRecipe r) { int v; return Queues.TryGetValue(r.id,out v)?v:0; }
    public void SetRecipeQueueCount(ComplexRecipe r,int n) { Queues[r.id]=n; }
    public void SetQueueDirty() {}
    protected virtual bool HasIngredients(ComplexRecipe r,Storage s) { return s.Mass>=r.ingredients.Sum(i=>i.amount); }
    protected virtual float ComputeWorkProgress(float dt,ComplexRecipe r) { return dt/r.time; }
    public float Work(float dt) { GameClock.Instance.Time+=.2; var p=ComputeWorkProgress(dt,CurrentWorkingOrder); OrderProgress+=p; return p; }
    public void Sim200ms(float dt) { if(!operational.IsOperational)return; OrderProgress+=ComputeWorkProgress(dt,CurrentWorkingOrder); if(OrderProgress>=1)CompleteWorkingOrder(); }
    public virtual void Sim1000ms(float dt) {
        BaseTicks++;
        if (!operational.IsOperational) return;
        var next=Recipes[0]; // Actual native ValidateNextOrder indexes zero on an empty list.
        if (CurrentWorkingOrder==null && GetRecipeQueueCount(next)>0 && HasIngredients(next,inStorage)) {
            CurrentWorkingOrder=next; buildStorage.Mass=next.ingredients.Sum(i=>i.amount);
        }
    }
    public virtual void CompleteWorkingOrder() {
        if (CurrentWorkingOrder==null) return;
        Completed++;outStorage.Mass+=CurrentWorkingOrder.results.Sum(i=>i.amount); buildStorage.Mass=0; CurrentWorkingOrder=null; OrderProgress=0;
    }
}
namespace ForbiddenTechnologyPack.Game.Save {
    public class ForbiddenTechSaveData {
        public static ForbiddenTechSaveData Instance;
        public bool SafeRemovalStarted;
        private UnlockState state=UnlockState.FromSerialized(1,null);
        public event Action UnlocksChanged;
        public UnlockState GetUnlockState() { return state; }
        public void Unlock(string id) { state.Unlock(id); if(UnlocksChanged!=null) UnlocksChanged(); }
    }
}
namespace ForbiddenTechnologyPack.Game.Elements {
    public static class ProtoMatterRegistration { public static SimHashes Hash=SimHashes.Proto; public static Tag Tag=new Tag("Proto"); }
    public static class ElementCatalogAdapter { public static Dictionary<string,MaterialRule> Rules=new Dictionary<string,MaterialRule>(); }
}
namespace ForbiddenTechnologyPack.Game.Recipes { public static class RecipeRegistry { public static Dictionary<string,ComplexRecipe> CompilerRecipes=new Dictionary<string,ComplexRecipe>(); public static ComplexRecipe CondenserRecipe=new ComplexRecipe{id="Condense",time=500,ingredients=new ComplexRecipe.RecipeElement[0],results=new[]{new ComplexRecipe.RecipeElement{material=new Tag("Proto"),amount=1}}}; } }
namespace ForbiddenTechnologyPack.Game.Safety { public static class SafeRemovalController { public static Operational.Flag SafeRemovalAllowed=new Operational.Flag("remove",Operational.Flag.Type.Requirement); } }
namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    public static class FabricatorSupport {
        public static void SetRecipeList(ComplexFabricator f,IEnumerable<ComplexRecipe> r) {
            if(f.CurrentWorkingOrder!=null) throw new Exception("Active list replacement");
            f.Recipes=r.ToArray();
        }
        public static string GetLastWorkingRecipeId(ComplexFabricator f) { return null; }
        public static void SetStorageFilters(Storage s,IEnumerable<Tag> t) {}
    }
    // Payment is injected for adapter tests; PAYMENT_HOST compiles the actual budget.
#if !PAYMENT_HOST
    public class FabricatorPowerBudget { public bool IsSettling=true; public float CreditSeconds; public float TakeWorkSeconds(float dt) { var v=Math.Min(dt,CreditSeconds);CreditSeconds-=v;return v; } public void Clear() { CreditSeconds=0; } }
#endif
}
#if !PAYMENT_HOST
internal static class ProductionBehavior {
    static int passed;
    static void Check(bool b,string m) { if(!b) throw new Exception(m);passed++; }
    static void Invoke(object o,string m,params object[] args) { o.GetType().GetMethod(m,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,args); }
    static MatterCompiler Create() {
        var c=new MatterCompiler(); c.gameObject.Add(new Operational()); c.gameObject.Add(new SolidConduitDispenser());
        c.gameObject.Add(new FabricatorPowerBudget()); c.outStorage.Capacity=120;
        Invoke(c,"OnSpawn");return c;
    }
    static CoolantController Cooling(KMonoBehaviour c, float heat=160000) {
        var cooling=new CoolantController {storage=new Storage(),heatedStorage=new Storage{Capacity=10},heatJoulesPerSecond=heat};
        var item=new UnityEngine.GameObject();item.Add(new PrimaryElement{gameObject=item}); cooling.storage.items.Add(item);
        foreach(var name in new[]{"operational","consumer","dispenser"}) {
            var f=typeof(CoolantController).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
            f.SetValue(cooling,name=="operational"?(object)new Operational():name=="consumer"?(object)new ConduitConsumer():new ConduitDispenser());
        }
        c.gameObject.Add(cooling);Invoke(cooling,"OnSpawn");return cooling;
    }
    static void SetIgnition(ProtoAnnihilationReactor r,float j) {
        typeof(ProtoAnnihilationReactor).GetField("ignitionJoules",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(r,j);
    }
    static void TickReactor(ProtoAnnihilationReactor r,float dt) { GameClock.Instance.Time+=.2; r.Sim200ms(dt); }
    static ProtoAnnihilationReactor Reactor() {
        var r=new ProtoAnnihilationReactor {fuel=new Storage{Mass=30,Capacity=120}};r.gameObject.Add(r);
        r.gameObject.Add(new Operational());r.gameObject.Add(new Generator());r.gameObject.Add(new FabricatorPowerBudget());
        r.gameObject.Add(new KBatchedAnimController());r.gameObject.Add(new RadiationEmitter());
        Cooling(r);Invoke(r,"OnSpawn");return r;
    }
    static void MassEnergyChecks() {
        var r=Reactor();var p=r.gameObject.GetComponent<FabricatorPowerBudget>();
        var gen=r.gameObject.GetComponent<Generator>();var cool=r.gameObject.GetComponent<CoolantController>();
        var cold=cool.storage.items[0].GetComponent<PrimaryElement>();var initial=cold.Temperature;
        TickReactor(r,.2f);
        Check(r.IgnitionProgress==0 && r.fuel.Mass==30 && gen.Generated==0 && cold.Temperature==initial,"Unpaid ignition changes no material, energy or heat");
        p.CreditSeconds=.1f;TickReactor(r,.2f);
        Check(Math.Abs(r.IgnitionProgress*600000-800)<.01 && gen.Generated==0 && r.fuel.Mass==30,"Partial electrical payment charges only its actual ignition duration");
        Check(Math.Abs(cold.Temperature-initial-.02)<.00005,"Ignition electricity becomes actual coolant heat");
        SetIgnition(r,599200);p.CreditSeconds=.2f;TickReactor(r,.2f);
        Check(r.Ignited && gen.Generated==0 && r.fuel.Mass==30,"Ignition completion cannot also spend the same receipt on generation");
        p.CreditSeconds=.2f;TickReactor(r,.2f);
        Check(Math.Abs(r.TotalFuelKg-.004)<.000001 && Math.Abs(gen.Generated-9600)<.1,"Actual 0.004 kg fuel consumption generates exactly one paid interval");
        Check(r.gameObject.GetComponent<RadiationEmitter>().Emitting,"Radiation turns on only during actual annihilation");
        var total=r.TotalFuelKg;var heat=cold.Temperature;
        TickReactor(r,.2f);
        Check(r.TotalFuelKg==total && gen.Generated==9600 && cold.Temperature==heat,"A generation receipt cannot be reused");
        Check(!r.gameObject.GetComponent<RadiationEmitter>().Emitting && r.StateText==STRINGS.UI.MASSENERGY.READY,"Unpaid reactor interval clears running effects and reports waiting for power");
        gen.JoulesAvailable=gen.Capacity;p.CreditSeconds=.2f;TickReactor(r,.2f);
        Check(r.TotalFuelKg==total && cold.Temperature==heat && p.CreditSeconds==0,"A full electrical buffer stops consumption, heat and payment reuse");
        gen.JoulesAvailable=0;cool.heatedStorage.Capacity=cool.heatedStorage.MassStored();p.CreditSeconds=.2f;TickReactor(r,.2f);
        Check(r.TotalFuelKg==total && cold.Temperature==heat,"An obstructed coolant return stops annihilation");
        cool.heatedStorage.Capacity=10;r.fuel.Mass=0;p.CreditSeconds=.2f;TickReactor(r,.2f);
        Check(r.TotalFuelKg==total && gen.Generated==9600,"Empty fuel cannot generate electricity");
        r.fuel.Mass=30;r.gameObject.GetComponent<Operational>().IsOperational=false;p.CreditSeconds=.2f;TickReactor(r,.2f);
        Check(r.TotalFuelKg==total && p.CreditSeconds==0 && !r.gameObject.GetComponent<RadiationEmitter>().Emitting,"Disabled automation or damage stops actual work and effects");
        r.gameObject.GetComponent<Operational>().IsOperational=true;Invoke(r,"OnCleanUp");p.CreditSeconds=.2f;TickReactor(r,.2f);
        Check(r.TotalFuelKg==total,"A destroyed component cannot process a late callback");
        var restored=Reactor();SetIgnition(restored,300000);Invoke(restored,"OnSpawn");
        Check(restored.IgnitionProgress==.5f,"Spawn preserves saved ignition without granting electricity");
        SetIgnition(restored,float.NaN);Invoke(restored,"OnSpawn");
        Check(restored.IgnitionProgress==0,"Invalid saved ignition cannot contaminate energy accounting");

        var c=new EnergyMatterCondenser();c.gameObject.Add(new FabricatorPowerBudget());
        c.outStorage.Capacity=20;var cc=Cooling(c,12000);Invoke(c,"OnSpawn");
        c.SetRecipeQueueCount(RecipeRegistry.CondenserRecipe,1);c.Sim1000ms(1);
        Check(c.CurrentWorkingOrder!=null && c.buildStorage.Mass==0,"An ingredient-free paid recipe starts through the native queue contract");
        c.CompleteWorkingOrder();Check(c.Completed==0 && c.outStorage.Mass==0,"An unfinished condenser cannot emit a product");
        var cp=c.gameObject.GetComponent<FabricatorPowerBudget>();
        var ce=cc.storage.items[0].GetComponent<PrimaryElement>();var ct=ce.Temperature;
        Check(c.Work(.2f)==0 && ce.Temperature==ct,"Unpaid condenser work produces neither progress nor heat");
        cp.CreditSeconds=.1f;Check(Math.Abs(c.Work(.2f)-.1/500)<.0000001,"Partial condenser payment advances only the paid time");
        Check(Math.Abs(ce.Temperature-ct-.03)<.00005,"Partial condenser heat matches its electrical work");
        c.outStorage.Mass=20;cp.CreditSeconds=.2f;ct=ce.Temperature;
        Check(c.Work(.2f)==0 && ce.Temperature==ct && cp.CreditSeconds==0,"Full output storage rejects both progress and heat");
        c.outStorage.Mass=0;cc.heatedStorage.Capacity=cc.heatedStorage.MassStored();cp.CreditSeconds=.2f;
        Check(c.Work(.2f)==0,"Condenser cannot progress with a blocked cooling return");
        // Cancellation leaves the already emitted heat in its physical buffers.
        c.CurrentWorkingOrder=null;c.OrderProgress=0;
        Check(ce.Temperature==ct && cc.heatedStorage.MassStored()>0,"Cancellation preserves paid heat and emits no partial fragments");
        c.CurrentWorkingOrder=RecipeRegistry.CondenserRecipe;
        double paid=0,heatJ=0;
        for(int i=0;i<2600 && c.OrderProgress<1;i++) {
            var cycle=Cooling(c,12000);var liquid=cycle.storage.items[0].GetComponent<PrimaryElement>();
            var t=liquid.Temperature;cp.CreditSeconds=.2f;c.Work(.2f);paid+=(.2f-cp.CreditSeconds)*36000;
            heatJ+=cycle.storage.items.Concat(cycle.heatedStorage.items).Sum(item=>{
                var element=item.GetComponent<PrimaryElement>();return (double)element.Mass*(element.Temperature-t)*4000;
            });
        }
        Check(c.OrderProgress>=1 && c.outStorage.Mass==0,"An entire paid batch keeps output deferred until completion");
        Check(Math.Abs(paid-18000000)<1500 && Math.Abs(heatJ-6000000)<1500,"A full batch pays 18 MJ and sends 6 MJ to coolant within float simulation precision");
        c.CompleteWorkingOrder();c.CompleteWorkingOrder();
        Check(c.Completed==1 && c.outStorage.Mass==1,"Completion produces exactly one 1 kg aggregate and cannot run twice");
    }
    static void BoundaryChecks() {
        var r=Reactor();var cool=r.gameObject.GetComponent<CoolantController>();
        cool.heatJoulesPerSecond=200000;
        var cold=cool.storage.items[0].GetComponent<PrimaryElement>();cold.Temperature=371;
        GameClock.Instance.Time+=.2;
        Check(cool.TryProcessWork(.2f),"Near-limit coolant accepts its final safe paid slice");
        Check(cool.heatedStorage.MassStored()==2 && cold.Mass==8,"Readiness refresh never adds a second return packet");
        var flag=cool.IsReady;var detail=cool.DetailText;cool.Sim200ms(.2f);cool.Sim200ms(.2f);
        Check(cool.heatedStorage.MassStored()==2 && cold.Mass==8,"Status and repeated transport callbacks share the 2 kg interval cap");
        GameClock.Instance.Time+=.2;cool.Sim200ms(.2f);
        Check(cool.heatedStorage.MassStored()==4 && cold.Temperature==372,"Next interval returns unsafe coolant without inventing heat");
        cool=Cooling(r);cold=cool.storage.items[0].GetComponent<PrimaryElement>();
        cool.heatJoulesPerSecond=200000;cold.Temperature=371.8f;
        GameClock.Instance.Time+=.2;
        Check(cool.TryProcessWork(.1f),"A partial safe work slice is accepted");
        cool.Sim200ms(.2f);
        Check(cool.heatedStorage.MassStored()==2,"Partial work and unsafe flush use one shared interval allowance");
        GameClock.Instance.Time+=.2;cool.Sim200ms(float.NaN);
        Check(cool.heatedStorage.MassStored()==2,"Invalid elapsed time cannot move liquid");
        var tail=cool.heatedStorage.items[0].GetComponent<PrimaryElement>();
        Check(!cool.CanReleaseReturn(tail),"Small return liquid accumulates instead of emitting per work slice");
        GameClock.Instance.Time+=2;
        Check(cool.CanReleaseReturn(tail),"A stopped machine eventually releases its actual tail inventory");
        var previous=cool.heatedStorage.MassStored();cool.heatedStorage.items.Remove(tail.gameObject);cool.ObserveReturn(previous);
        Check(!cool.CanReleaseReturn(cool.heatedStorage.items[0].GetComponent<PrimaryElement>()),"Small mixed tails cannot flash out repeatedly in one interval");
        Check(OutputBatchPolicy.CanReleaseLiquid(5,5,0,double.PositiveInfinity),"Normal 5 kg batches are available without recent partial output");
        Check(OutputBatchPolicy.CanReleaseLiquid(1,0,0,3),"A full mixed-element buffer can drain without deadlock");
        Check(!OutputBatchPolicy.CanReleaseLiquid(float.NaN,0,3,3),"Invalid liquid inventory cannot be emitted");
        var batched=Cooling(r);var packet=batched.storage.items[0].GetComponent<PrimaryElement>();
        batched.storage.Transfer(batched.heatedStorage,packet.Element.tag,6,true,true);
        packet=batched.heatedStorage.items[0].GetComponent<PrimaryElement>();
        Check(batched.CanReleaseReturn(packet),"Large actual return stock is ready for native transport");
        packet.Mass-=.01f;batched.ObserveReturn(6);
        Check(!batched.CanReleaseReturn(packet),"A native partial output applies cooldown even when the candidate remains over 5 kg");
        GameClock.Instance.Time+=.2;Check(!batched.CanReleaseReturn(packet),"A nearly full downstream pipe cannot accept micro-packets every tick");
        GameClock.Instance.Time+=2;Check(batched.CanReleaseReturn(packet),"Partial-output cooldown eventually permits the retained inventory to drain");
        var rail=new SolidConduitConsumer{gameObject=r.gameObject,IsConnected=true};r.gameObject.Add(new Building());
        var prefix=typeof(MatterCompiler).Assembly.GetType("ForbiddenTechnologyPack.Game.Buildings.Compiler.MatterCompilerRailInputPatch").GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic);
        Game.Instance.solidConduitFlow.Item=new Pickupable{PrimaryElement=new PrimaryElement{ElementID=SimHashes.Dirt}};
        Check(!(bool)prefix.Invoke(null,new object[]{rail}),"The reactor rejects wrong rail material before native storage consumes it");
        Game.Instance.solidConduitFlow.Item.PrimaryElement.ElementID=SimHashes.Proto;
        Check((bool)prefix.Invoke(null,new object[]{rail}),"The reactor accepts its actual Proto-Matter fuel");
        Game.Instance.solidConduitFlow.Item=null;
        ForbiddenTechSaveData.Instance=new ForbiddenTechSaveData{SafeRemovalStarted=true};
        var removed=Reactor();var p=removed.gameObject.GetComponent<FabricatorPowerBudget>();p.CreditSeconds=.2f;
        TickReactor(removed,.2f);
        Check(removed.IgnitionProgress==0 && p.CreditSeconds==0,"A reactor spawned after safe removal cannot start processing");
        ForbiddenTechSaveData.Instance=null;removed=Reactor();
        ForbiddenTechSaveData.Instance=new ForbiddenTechSaveData{SafeRemovalStarted=true};p=removed.gameObject.GetComponent<FabricatorPowerBudget>();p.CreditSeconds=.2f;
        TickReactor(removed,.2f);
        Check(removed.IgnitionProgress==0 && p.CreditSeconds==0,"Late save initialization cannot bypass the removal guard");
        ForbiddenTechSaveData.Instance=new ForbiddenTechSaveData();
        Check((bool)prefix.Invoke(null,new object[]{rail}),"An empty rail retains native connection handling");
        var ordinary=new SolidConduitConsumer{IsConnected=true};
        Game.Instance.solidConduitFlow.Item=new Pickupable{PrimaryElement=new PrimaryElement{ElementID=SimHashes.Dirt}};
        Check((bool)prefix.Invoke(null,new object[]{ordinary}),"The filter cannot intercept unrelated buildings");
        Game.Instance.solidConduitFlow.Item=null;
    }
    static int Main() {
        try {
            foreach(string id in new[]{"Dirt","Sand"}) {
                ElementCatalogAdapter.Rules[id]=MaterialRule.ForTier(id,MaterialTier.Common);
                RecipeRegistry.CompilerRecipes[id]=new ComplexRecipe { id=id,ingredients=new[]{new ComplexRecipe.RecipeElement { material=new Tag("Proto"),amount=125 }},results=new[]{new ComplexRecipe.RecipeElement {material=new Tag(id),amount=100}}};
            }
            ForbiddenTechSaveData.Instance=new ForbiddenTechSaveData();
            var empty=Create(); empty.Sim1000ms(1);empty.Sim1000ms(1);
            Check(empty.CurrentWorkingOrder==null,"No unlocked material must idle without native empty-list indexing");
            ForbiddenTechSaveData.Instance.Unlock("Dirt");
            Check(empty.GetRecipes().Length==1,"External unlock refreshes the idle recipe list");
            empty.CurrentWorkingOrder=RecipeRegistry.CompilerRecipes["Dirt"];empty.buildStorage.Mass=125;
            ForbiddenTechSaveData.Instance.Unlock("Sand");
            Check(empty.GetRecipes().Length==1,"Unlock during a batch defers the list replacement");
            empty.CurrentWorkingOrder=null;empty.Sim1000ms(1);
            Check(empty.GetRecipes().Length==2,"Cancellation reaches an idle boundary and flushes pending recipes");
            var patch=typeof(MatterCompiler).Assembly.GetType("ForbiddenTechnologyPack.Game.Buildings.Compiler.MatterCompilerCancellationPatch");
            empty.CurrentWorkingOrder=RecipeRegistry.CompilerRecipes["Dirt"];empty.buildStorage.Mass=125;
            bool permitted=patch==null || (bool)patch.GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{empty});
            Check(permitted,"A user must be allowed to cancel a fully reserved batch");
            ForbiddenTechSaveData.Instance=new ForbiddenTechSaveData();ForbiddenTechSaveData.Instance.Unlock("Dirt");
            var output=Create();Check(output.CanDispenseProduct(SimHashes.Sand),"Existing ordinary output remains dispensable even when its recipe is locked");
            output.CurrentWorkingOrder=RecipeRegistry.CompilerRecipes["Dirt"];
            var outputOperational=(Operational)typeof(ComplexFabricator).GetField("operational",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(output);
            output.outStorage.Mass=30;output.RefreshOutputSpace();
            Check(!outputOperational.GetFlag(MatterCompiler.OutputSpace),"An obstructed output disables native operation before its next automatic tick");
            output.outStorage.Mass=0;output.RefreshOutputSpace();
            Check(outputOperational.GetFlag(MatterCompiler.OutputSpace),"Removing stored output restores the native operational requirement");
            var cool=Cooling(output);var primary=cool.storage.items[0].GetComponent<PrimaryElement>();
            output.CurrentWorkingOrder=RecipeRegistry.CompilerRecipes["Dirt"];output.buildStorage.Mass=125;
            float before=primary.Temperature;
            Check(output.Work(.2f)==0,"No paid electricity must yield zero work");
            Check(primary.Temperature==before,"No paid work must yield zero process heat");
            output.gameObject.GetComponent<FabricatorPowerBudget>().CreditSeconds=.2f;
            Check(Math.Abs(output.Work(.2f)-(.2f/30))<.00001,"Paid work advances the exact duration");
            Check(Math.Abs(primary.Temperature-before-.8f)<.0001,"Heat matches 160 kDTU/s times the paid 0.2 seconds");
            before=primary.Temperature;Check(output.Work(.2f)==0,"A spent electrical receipt cannot be reused");
            Check(primary.Temperature==before,"Unpaid time cannot gain heat");
            Check(Math.Abs(cool.heatedStorage.MassStored()-2f)<.0001,"A 0.2 second interval emits at most a 2 kg pipe packet");
            Check(Math.Abs(cool.storage.MassStored()-8f)<.0001,"The remaining actual coolant stays in the input buffer");
            double heat=(cool.storage.MassStored()*(primary.Temperature-293.15f)+
                cool.heatedStorage.items.Sum(i=>{var p=i.GetComponent<PrimaryElement>();return p.Mass*(p.Temperature-293.15f);}))*4000;
            Check(Math.Abs(heat-32000)<1,"Split buffer total heat equals the paid process heat");
            cool.heatedStorage.Capacity=2;
            output.gameObject.GetComponent<FabricatorPowerBudget>().CreditSeconds=.2f;
            Check(output.Work(.2f)==0 && primary.Temperature==before,"A full return buffer stops both progress and heat");
            cool.heatedStorage.DropAll(true,true,UnityEngine.Vector3.zero,true,null);
            output.gameObject.GetComponent<FabricatorPowerBudget>().CreditSeconds=.2f;
            Check(output.Work(.2f)>0,"Unblocking the actual return buffer resumes work");
            var restored=Cooling(output);
            var old=restored.storage.items[0].GetComponent<PrimaryElement>();old.Temperature=340;
            typeof(CoolantController).GetField("releasingHeatedPacket",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(restored,true);
            Invoke(restored,"OnSpawn");
            Check(restored.storage.MassStored()==0 && restored.heatedStorage.MassStored()==10,
                "A saved heated packet migrates to the appended return buffer exactly once");
            Check(restored.heatedStorage.items[0].GetComponent<PrimaryElement>().Temperature==340,
                "Migration preserves the saved temperature");
            Invoke(restored,"OnSpawn");
            Check(restored.heatedStorage.MassStored()==10,"Repeating initialization cannot duplicate the migrated packet");
            var hot=Cooling(output);var unsafeLiquid=hot.storage.items[0].GetComponent<PrimaryElement>();unsafeLiquid.Temperature=373;
            hot.Sim200ms(.2f);
            Check(unsafeLiquid.Temperature==373 && hot.heatedStorage.MassStored()==2,
                "Unsafe liquid is returned unchanged in a bounded packet");
            var unavailable=Create();
            ForbiddenTechSaveData.Instance=null;var late=Create();
            ForbiddenTechSaveData.Instance=new ForbiddenTechSaveData();late.Sim1000ms(1);
            ForbiddenTechSaveData.Instance.Unlock("Dirt");
            Check(late.GetRecipes().Length==1,"A late save singleton attaches its unlock event on the next tick");
            MassEnergyChecks();
            BoundaryChecks();
            Console.WriteLine("Production runtime host: "+passed+" behavior checks passed (no Unity/Sim).");return 0;
        } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
    }
}
#endif
