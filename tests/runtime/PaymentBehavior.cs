using System;
using System.Reflection;
using ForbiddenTechnologyPack.Game.Buildings.Common;
using ForbiddenTechnologyPack.Game.Buildings.Compiler;
using ForbiddenTechnologyPack.Game.Buildings.MassEnergy;
using ForbiddenTechnologyPack.Game.Recipes;

internal static class PaymentBehavior {
    static int passed;
    static void Check(bool value,string message) { if(!value)throw new Exception(message);passed++; }
    static void Spawn(object component) { component.GetType().GetMethod("OnSpawn",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(component,null); }
    static FabricatorPowerBudget Budget(KMonoBehaviour owner,Operational operational,float watts) {
        owner.gameObject.Add(owner);owner.gameObject.Add(operational);
        var consumer=new EnergyConsumer{gameObject=owner.gameObject,WattsNeededWhenActive=watts};owner.gameObject.Add(consumer);
        var budget=new FabricatorPowerBudget{gameObject=owner.gameObject};owner.gameObject.Add(budget);Spawn(budget);return budget;
    }
    static CoolantController Cooling(KMonoBehaviour owner,float watts) {
        var item=new UnityEngine.GameObject();item.Add(new PrimaryElement{gameObject=item});
        var c=new CoolantController{storage=new Storage(),heatedStorage=new Storage{Capacity=10},heatJoulesPerSecond=watts};c.storage.items.Add(item);
        typeof(CoolantController).GetField("operational",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,owner.GetComponent<Operational>());
        owner.gameObject.Add(c);Spawn(c);return c;
    }
    static void Brownout(KMonoBehaviour owner) {
        var consumer=owner.GetComponent<EnergyConsumer>();
        typeof(FabricatorPowerBudget).Assembly.GetType("ForbiddenTechnologyPack.Game.Buildings.Common.FabricatorBrownoutPatch")
            .GetMethod("Prefix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{consumer,CircuitManager.ConnectionStatus.Unpowered});
        consumer.SetConnectionStatus(CircuitManager.ConnectionStatus.Unpowered);
    }
    static void Powered(KMonoBehaviour owner) {
        var consumer=owner.GetComponent<EnergyConsumer>();consumer.SetConnectionStatus(CircuitManager.ConnectionStatus.Powered);
        typeof(FabricatorPowerBudget).Assembly.GetType("ForbiddenTechnologyPack.Game.Buildings.Common.FabricatorBrownoutPatch")
            .GetMethod("Postfix",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{consumer,CircuitManager.ConnectionStatus.Powered});
    }
    static int Main() {
        try {
            var r=new ProtoAnnihilationReactor{fuel=new Storage{Mass=30}};var op=new Operational();var budget=Budget(r,op,8000);
            r.gameObject.Add(new Generator());r.gameObject.Add(new KBatchedAnimController());r.gameObject.Add(new RadiationEmitter());
            var cooling=Cooling(r,8000);Spawn(r);budget.RecordPayment(800);Brownout(r);
            Check(Math.Abs(r.IgnitionProgress*600000-800)<.01,"A real budget commits partial ignition before native PoweredFlag turns false");
            Check(!op.IsOperational && !op.IsActive,"Native brownout flags remain effective after paid work");
            Check(Math.Abs(cooling.heatedStorage.MassStored()-1)<.00001,"Partial payment emits exactly its corresponding coolant mass");
            budget.Sim200ms(.2f);r.Sim200ms(.2f);
            Check(Math.Abs(r.IgnitionProgress*600000-800)<.01 && budget.TakeWorkSeconds(.2f)==0,"A partial settlement cannot run again after the power flag changes");
            op.SetFlag(EnergyConsumer.PoweredFlag,true);op.SetActive(true);GameClock.Instance.Time+=.2;
            var automation=new Operational.Flag("automation",Operational.Flag.Type.Requirement);op.SetFlag(automation,false);
            budget.RecordPayment(1600);Brownout(r);
            Check(Math.Abs(r.IgnitionProgress*600000-800)<.01,"Partial-power handling cannot override automation, damage or coolant requirements");

            var condenser=new EnergyMatterCondenser();var cop=new Operational();var cb=Budget(condenser,cop,36000);
            typeof(ComplexFabricator).GetField("operational",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(condenser,cop);
            condenser.outStorage.Capacity=20;var cc=Cooling(condenser,12000);Spawn(condenser);
            condenser.CurrentWorkingOrder=RecipeRegistry.CondenserRecipe;GameClock.Instance.Time+=.2;
            cb.RecordPayment(3600);Brownout(condenser);
            Check(Math.Abs(condenser.OrderProgress-.1/500)<.0000001,"Condenser partial payment survives native power loss with exact progress");
            Check(cc.heatedStorage.MassStored()==1 && condenser.outStorage.Mass==0,"Condenser partial settlement emits heat and no premature product");
            Check(!cop.IsOperational && cb.TakeWorkSeconds(.2f)==0,"Condenser cannot spend a partial receipt twice");
            cop.SetFlag(EnergyConsumer.PoweredFlag,true);cop.SetActive(true);condenser.outStorage.Mass=20;GameClock.Instance.Time+=.2;
            cb.RecordPayment(7200);Brownout(condenser);
            Check(Math.Abs(condenser.OrderProgress-.1/500)<.0000001 && cc.heatedStorage.MassStored()==1,"A full output blocks brownout progress and heat");
            foreach(double phase in new[]{0d,.0333d,.1666d}) {
                var continuous=new ProtoAnnihilationReactor{fuel=new Storage{Mass=30}};var continuousOp=new Operational();var b=Budget(continuous,continuousOp,8000);
                continuous.gameObject.Add(new Generator());continuous.gameObject.Add(new KBatchedAnimController());continuous.gameObject.Add(new RadiationEmitter());
                var cool=Cooling(continuous,8000);Spawn(continuous);GameClock.Instance.Time=6000000+phase;
                b.RecordPayment(1600);Powered(continuous);
                Check(Math.Abs(continuous.IgnitionProgress*600000-1600)<.01,"Normal payment is committed immediately at the settlement event in an old colony");
                continuous.Sim200ms(.2f);b.Sim200ms(.2f);
                Check(Math.Abs(continuous.IgnitionProgress*600000-1600)<.01,"Load-balanced ordinary callbacks cannot spend the previous receipt again");
                GameClock.Instance.Time+=.2;continuous.Sim200ms(.2f);b.RecordPayment(800);Brownout(continuous);
                Check(Math.Abs(continuous.IgnitionProgress*600000-2400)<.01 && cool.heatedStorage.MassStored()==3,
                    "Normal work followed by partial brownout keeps both payments and their exact coolant across callback phases");
            }
            Console.WriteLine("Actual payment adapter host: "+passed+" checks passed (native flag model; no Unity/Sim).");return 0;
        } catch(Exception ex) {Console.Error.WriteLine(ex);return 1;}
    }
}
