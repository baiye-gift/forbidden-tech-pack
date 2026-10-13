[CmdletBinding()]
param([Parameter(Mandatory=$true)][string]$ProjectRoot)
$ErrorActionPreference='Stop'
. (Join-Path $ProjectRoot 'scripts\Get-CSharpCompiler.ps1')
$compiler=Get-ForbiddenTechnologyCSharpCompiler -ProjectRoot $ProjectRoot
$directory=Join-Path $ProjectRoot 'test-artifacts\research-topology'
New-Item -ItemType Directory -Force -Path $directory | Out-Null
$source=Join-Path $directory 'ResearchTopologyHost.cs'
$assembly=Join-Path $directory 'ResearchTopologyHost.exe'
$body=@'
// Actual registration adapter in a deterministic research graph host.
// Native Tech construction still requires the separate Unity runtime check.
using System;
using System.Collections.Generic;
using System.Reflection;
using ForbiddenTechnologyPack.Core;
namespace UnityEngine {
    public class TextAsset {}
    public static class Debug { public static void LogWarning(string s) {} public static void LogError(string s) {} }
}
namespace HarmonyLib {
    public class HarmonyPatch:Attribute { public HarmonyPatch(Type t,string s) {} }
    public class Traverse {
        object value;public static Traverse Create(object o) { return new Traverse{value=o}; }
        public Field<T> Field<T>(string name) { return new Field<T>{Value=(T)value.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(value)}; }
    }
    public class Field<T> { public T Value; }
}
namespace Database {
    public class Techs {
        public Dictionary<string,Tech> entries=new Dictionary<string,Tech>();
        public Tech TryGet(string id) { Tech t;return entries.TryGetValue(id,out t)?t:null; }
    }
}
public class ResourceTreeLoader<T>:List<T> { public ResourceTreeLoader(UnityEngine.TextAsset text) {} }
public class ResourceTreeNode {
    public string Id,Name; public float nodeX,nodeY,width,height;
    public List<ResourceTreeNode> references=new List<ResourceTreeNode>(); public List<Edge> edges=new List<Edge>();
    public class Edge {
        public enum EdgeType { BezierEdge }
        public ResourceTreeNode target; public Edge(ResourceTreeNode from,ResourceTreeNode to,EdgeType type) { target=to; }
    }
}
public class Tech {
    public string Id,category;private ResourceTreeNode node;
    public List<string> unlockedItemIDs;public List<Tech> requiredTech=new List<Tech>(),unlockedTech=new List<Tech>();
    public Dictionary<string,float> costsByResearchTypeID=new Dictionary<string,float>();
    public Tech(string id,List<string> ids,Database.Techs owner,Dictionary<string,float> costs) {
        Id=id;unlockedItemIDs=ids;owner.entries[id]=this;
        costsByResearchTypeID["default"]=1; // Models native constructor default cost rewriting.
    }
    public void SetNode(ResourceTreeNode n,string c) { node=n;category=c; }
    public void AddSearchTerms(string s) {}
    public ResourceTreeNode Node {get{return node;}}
}
namespace ForbiddenTechnologyPack.Game.Options {
    public static class ForbiddenTechOptions { public static ResolvedOptions Current=PackOptions.Resolve(new RawOptions()); }
}
public static class STRINGS { public static class RESEARCH { public static class TECHS {
    public static class BAIYEFORBIDDENMATTERENGINEERING { public static string SEARCH_TERMS="matter"; }
    public static class BAIYEFORBIDDENMASSENERGYENGINEERING { public static string SEARCH_TERMS="energy"; }
} } }
internal static class ResearchTopologyHost {
    static int checks;
    static void Check(bool b,string s) { if(!b)throw new Exception(s);checks++; }
    static void Call(string method,params object[] args) {
        typeof(ResearchTopologyHost).Assembly.GetType("ForbiddenTechnologyPack.Game.Registration.ForbiddenResearchRegistration")
            .GetMethod(method,BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,args);
    }
    static void Scenario(string prerequisite,RawOptions options,bool energy) {
        ForbiddenTechnologyPack.Game.Options.ForbiddenTechOptions.Current=PackOptions.Resolve(options);
        var techs=new Database.Techs();var tree=new ResourceTreeLoader<ResourceTreeNode>(null);
        var baseNode=new ResourceTreeNode{Id=prerequisite,nodeX=100,nodeY=60,width=40,height=20};tree.Add(baseNode);
        var vanilla=new Tech(prerequisite,new List<string>(),techs,new Dictionary<string,float>());vanilla.SetNode(baseNode,"industrial");
        Call("Register",techs,tree);Call("RegisterEnergy",techs,tree);Call("Postfix",techs);
        var matter=techs.TryGet(ModIdentity.ResearchId);var mass=techs.TryGet(ModIdentity.MassEnergyResearchId);
        Check(matter!=null && matter.requiredTech.Contains(vanilla) && baseNode.references.Contains(matter.Node),"Matter prerequisite and visible connection must agree");
        Check((mass!=null)==energy,"Mass-energy module flag must control its research node");
        if(energy) {
            Check(mass.requiredTech.Count==1 && mass.requiredTech[0]==matter && matter.Node.references.Contains(mass.Node) &&
                !baseNode.references.Contains(mass.Node),"Mass-energy must connect only through forbidden matter engineering");
            Check(mass.category==matter.category && mass.Node.nodeX>matter.Node.nodeX && mass.Node.nodeY==matter.Node.nodeY,"Mass-energy layout and category must follow its actual prerequisite");
            Check(mass.costsByResearchTypeID.Count==2 && mass.costsByResearchTypeID["basic"]==180 && mass.costsByResearchTypeID["advanced"]==160,"Explicit research costs must survive native default rewriting");
            Check(mass.unlockedItemIDs.Count==2 && !matter.unlockedItemIDs.Contains(ModIdentity.AnnihilationReactorId),"Each research must own its intended buildings");
            Call("Register",techs,tree);Call("RegisterEnergy",techs,tree);Call("Postfix",techs);
            Check(techs.entries.Count==3 && matter.Node.edges.Count==1 && baseNode.edges.Count==1,"Repeated registration must not duplicate nodes or edges");
        }
    }
    static int Main() {
        try {
            Scenario("MatterDeconstruction",new RawOptions(),true);
            Scenario("HighTempForging",new RawOptions(),true);
            Scenario("MatterDeconstruction",new RawOptions{MassEnergyEnabled=false},false);
            Console.WriteLine("Research topology host: "+checks+" behavior checks passed (no native Tech construction/Unity).");return 0;
        } catch(Exception e) {Console.Error.WriteLine(e);return 1;}
    }
}
'@
[IO.File]::WriteAllText($source,$body,[Text.UTF8Encoding]::new($false))
$sources=@(Get-ChildItem (Join-Path $ProjectRoot 'src\Core') -Filter '*.cs' | ForEach-Object FullName)
$sources+=@($source,(Join-Path $ProjectRoot 'src\Game\Registration\ForbiddenResearchRegistration.cs'))
& $compiler /nologo /target:exe /langversion:7.3 "/out:$assembly" @sources
if($LASTEXITCODE -ne 0) {exit $LASTEXITCODE}
& $assembly
exit $LASTEXITCODE
