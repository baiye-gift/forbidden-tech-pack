[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$gamePath = 'D:\steam\steamapps\common\OxygenNotIncluded'
$managedDirectory = Join-Path $gamePath 'OxygenNotIncluded_Data\Managed'
$rawAssembly = Join-Path $ProjectRoot 'obj\ForbiddenTechnologyPack.raw.dll'
$probeDirectory = Join-Path $ProjectRoot 'test-artifacts\analyzer-recipe-runtime'
$probeSource = Join-Path $probeDirectory 'AnalyzerRecipeRuntimeProbe.cs'
$probeAssembly = Join-Path $probeDirectory 'AnalyzerRecipeRuntimeProbe.exe'
$runtimeConfig = Join-Path $probeDirectory 'AnalyzerRecipeRuntimeProbe.runtimeconfig.json'

& (Join-Path $ProjectRoot 'build.ps1') -GamePath $gamePath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

New-Item -ItemType Directory -Force -Path $probeDirectory | Out-Null
$source = @'
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;

internal static class AnalyzerRecipeRuntimeProbe {
    private static IEnumerable<MethodBase> Calls(MethodInfo method) {
        var codes = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode))
            .Select(f => (OpCode)f.GetValue(null)).ToDictionary(c => c.Value);
        var il = method.GetMethodBody().GetILAsByteArray();
        for (int i = 0; i < il.Length;) {
            short value = il[i++];
            if (value == 0xfe) value = (short)(0xfe00 | il[i++]);
            var code = codes[value];
            if (code.OperandType == OperandType.InlineMethod) yield return method.Module.ResolveMethod(BitConverter.ToInt32(il, i));
            switch (code.OperandType) {
                case OperandType.InlineNone: break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: i++; break;
                case OperandType.InlineVar: i += 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: i += 8; break;
                case OperandType.InlineSwitch: i += 4 + 4 * BitConverter.ToInt32(il, i); break;
                default: i += 4; break;
            }
        }
    }
    private static string gameManaged;
    private static string modDirectory;
    private static string libraryDirectory;

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args) {
        var fileName = new AssemblyName(args.Name).Name + ".dll";
        foreach (var directory in new[] { gameManaged, modDirectory, libraryDirectory }) {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate)) {
                return Assembly.LoadFrom(candidate);
            }
        }
        return null;
    }

    private static int Main(string[] args) {
        if (args.Length != 3) {
            Console.Error.WriteLine("Expected game-managed, mod-assembly, and library-directory arguments.");
            return 2;
        }

        gameManaged = args[0];
        modDirectory = Path.GetDirectoryName(args[1]);
        libraryDirectory = args[2];
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        try {
            Assembly.LoadFrom(Path.Combine(gameManaged, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(args[1]);
            var planType = mod.GetType("ForbiddenTechnologyPack.Core.RecipePlan", true);
            var planConstructor = planType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(constructor => constructor.GetParameters().Length == 14);
            var plan = planConstructor.Invoke(new object[] {
                "Dirt", "BaiyeMatterAnalyze_Dirt", "BaiyeMatterCrush_Dirt", "BaiyeMatterCompile_Dirt",
                10f, false, 30f, 100f, 90f, 40f, 125f, 100f, 37.5f, true
            });

            var registry = mod.GetType("ForbiddenTechnologyPack.Game.Recipes.RecipeRegistry", true);
            var create = registry.GetMethod("CreateAnalyzerRecipe", BindingFlags.Static | BindingFlags.NonPublic);
            var recipe = create.Invoke(null, new[] { plan, (object)0 });
            var results = (Array)recipe.GetType().GetField("results").GetValue(recipe);
            if (results.Length < 1) {
                throw new InvalidOperationException("Analyzer recipe must satisfy ComplexRecipe's non-empty result contract.");
            }

            var result = results.GetValue(0);
            var amount = (float)result.GetType().GetProperty("amount").GetValue(result, null);
            if (amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) {
                throw new InvalidOperationException("Analyzer metadata result must have a finite positive amount.");
            }

            var nameDisplay = recipe.GetType().GetField("nameDisplay").GetValue(recipe).ToString();
            if (!string.Equals(nameDisplay, "Ingredient", StringComparison.Ordinal)) {
                throw new InvalidOperationException("Analyzer recipe must display its analyzed ingredient, not its metadata result.");
            }

            var analyzer = mod.GetType("ForbiddenTechnologyPack.Game.Buildings.Analyzer.MatterAnalyzer", true);
            var completion = analyzer.GetMethod("CompleteWorkingOrder", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (completion == null || completion.GetBaseDefinition().DeclaringType != analyzer.BaseType) {
                throw new InvalidOperationException("Analyzer must override completion before the base fabricator starts another batch.");
            }
            var calls = Calls(completion).ToArray();
            var setQueue = Array.FindIndex(calls, m => m.Name == "SetRecipeQueueCount");
            var baseComplete = Array.FindIndex(calls, m => m.Name == "CompleteWorkingOrder" && m.DeclaringType == analyzer.BaseType);
            var unlock = Array.FindIndex(calls, m => m.Name == "Unlock");
            if (setQueue < 0 || baseComplete <= setQueue || unlock <= baseComplete || calls.Any(m => m.Name == "SetFlag")) {
                throw new InvalidOperationException("Completion must clamp the completed queue before base completion, unlock afterward, and never disable Operational.");
            }
            if (!Calls(analyzer.BaseType.GetMethod("CompleteWorkingOrder")).Any(m => m.Name == "RefreshAndStartNextOrder")) {
                throw new InvalidOperationException("Game completion no longer starts the next order; review the completion boundary fixture.");
            }
            var spawn = analyzer.GetMethod("SpawnOrderProduct",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (spawn == null || spawn.ReturnType != typeof(System.Collections.Generic.List<>).MakeGenericType(
                    Assembly.LoadFrom(Path.Combine(gameManaged, "UnityEngine.CoreModule.dll")).GetType("UnityEngine.GameObject", true))) {
                throw new InvalidOperationException("MatterAnalyzer must override SpawnOrderProduct to consume samples without spawning the metadata result.");
            }

            Console.WriteLine("Analyzer runtime recipe contract passed.");
            return 0;
        } catch (Exception exception) {
            var root = exception;
            while (root.InnerException != null) {
                root = root.InnerException;
            }
            Console.Error.WriteLine(root.GetType().FullName + ": " + root.Message);
            return 1;
        }
    }
}
'@
[System.IO.File]::WriteAllText($probeSource, $source, [System.Text.UTF8Encoding]::new($false))

. (Join-Path $ProjectRoot 'scripts\Get-CSharpCompiler.ps1')
$compiler = Get-ForbiddenTechnologyCSharpCompiler -ProjectRoot $ProjectRoot
& $compiler /nologo /target:exe /langversion:7.3 "/out:$probeAssembly" $probeSource
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$runtimeConfigBody = @'
{
  "runtimeOptions": {
    "tfm": "net10.0",
    "framework": {
      "name": "Microsoft.NETCore.App",
      "version": "10.0.0"
    }
  }
}
'@
[System.IO.File]::WriteAllText($runtimeConfig, $runtimeConfigBody, [System.Text.UTF8Encoding]::new($false))

& dotnet $probeAssembly $managedDirectory $rawAssembly (Join-Path $ProjectRoot 'lib')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Compile the actual analyzer source with a managed engine boundary. Unity native
# components cannot be constructed in this CLI process. The boundary reproduces
# the characterized game ordering: decrement, clear current, start next.
$behaviorSource = Join-Path $probeDirectory 'AnalyzerCompletionBehavior.cs'
$behaviorAssembly = Join-Path $probeDirectory 'AnalyzerCompletionBehavior.exe'
$behavior = @'
using System;
using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;
using ForbiddenTechnologyPack.Game.Buildings.Analyzer;
using ForbiddenTechnologyPack.Game.Save;
using ForbiddenTechnologyPack.Game.Recipes;
using ForbiddenTechnologyPack.Game.Elements;
namespace UnityEngine { public class GameObject {} }
namespace HarmonyLib {
    [AttributeUsage(AttributeTargets.Class)] public class HarmonyPatch : Attribute {
        public HarmonyPatch(Type type, string method) {}
    }
}
public struct Tag {
    public string Name;
    public Tag(string name) { Name = name; }
}
public class Storage {
    public void TransferMass(Storage to, Tag tag, float amount, bool a, bool b, bool c) {}
    public void ConsumeIgnoringDisease(Tag tag, float amount) {}
}
public class ComplexRecipe {
    public string id;
    public Ingredient[] ingredients;
    public class Ingredient { public Tag material; public float amount; public bool doNotConsume; }
}
public class ComplexFabricator {
    public ComplexRecipe CurrentWorkingOrder { get; set; }
    public ComplexRecipe[] Recipes = new ComplexRecipe[0];
    public Dictionary<string, int> Queues = new Dictionary<string, int>();
    public bool CanStart = true;
    public bool FailCompletion;
    public Storage buildStorage = new Storage(), outStorage = new Storage();
    protected virtual void OnSpawn() {}
    protected virtual void OnCleanUp() {}
    protected virtual List<UnityEngine.GameObject> SpawnOrderProduct(ComplexRecipe r) { return null; }
    public void SetRecipeQueueCount(ComplexRecipe recipe, int count) {
        Queues[recipe.id] = count;
        if (CurrentWorkingOrder == recipe && count == 0) CurrentWorkingOrder = null;
    }
    public int GetRecipeQueueCount(ComplexRecipe recipe) { return Queues[recipe.id]; }
    public virtual void CompleteWorkingOrder() {
        if (CurrentWorkingOrder == null) return;
        if (FailCompletion) throw new InvalidOperationException("Engine completion failed");
        var recipe = CurrentWorkingOrder;
        SpawnOrderProduct(recipe);
        if (Queues[recipe.id] != -1) Queues[recipe.id]--;
        CurrentWorkingOrder = null;
        StartNext();
    }
    public virtual void Sim1000ms(float dt) { StartNext(); }
    private void StartNext() {
        if (CanStart && CurrentWorkingOrder == null)
            CurrentWorkingOrder = Recipes.FirstOrDefault(r => Queues[r.id] != 0);
    }
}
namespace ForbiddenTechnologyPack.Game.Save {
    public class ForbiddenTechSaveData {
        public static ForbiddenTechSaveData Instance;
        private UnlockState state = UnlockState.FromSerialized(1, null);
        public event Action UnlocksChanged;
        public UnlockState GetUnlockState() { return state; }
        public bool Unlock(Tag tag) {
            bool changed = state.Unlock(tag.Name);
            if (changed && UnlocksChanged != null) UnlocksChanged();
            return changed;
        }
    }
}
namespace ForbiddenTechnologyPack.Game.Elements {
    public static class ElementCatalogAdapter {
        public static Dictionary<string, MaterialRule> Rules = new Dictionary<string, MaterialRule>();
    }
}
namespace ForbiddenTechnologyPack.Game.Recipes {
    public static class RecipeRegistry {
        public static Dictionary<string, ComplexRecipe> AnalyzerRecipes = new Dictionary<string, ComplexRecipe>();
    }
}
namespace ForbiddenTechnologyPack.Game.Buildings.Common {
    public static class FabricatorSupport {
        public static int Replacements;
        public static void SetRecipeList(ComplexFabricator f, IEnumerable<ComplexRecipe> recipes) {
            if (f.CurrentWorkingOrder != null) throw new InvalidOperationException("Active recipe-list replacement");
            Replacements++;
            f.Recipes = recipes.ToArray();
        }
    }
}
internal static class AnalyzerBehavior {
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static MatterAnalyzer Create(int count, int otherCount) {
        ForbiddenTechSaveData.Instance = new ForbiddenTechSaveData();
        var analyzer = new MatterAnalyzer();
        analyzer.Recipes = RecipeRegistry.AnalyzerRecipes.Values.ToArray();
        analyzer.Queues["Dirt"] = count; analyzer.Queues["Sand"] = otherCount;
        analyzer.CurrentWorkingOrder = RecipeRegistry.AnalyzerRecipes["Dirt"];
        typeof(MatterAnalyzer).GetMethod("SubscribeToUnlocks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(analyzer, null);
        return analyzer;
    }
    private static int Main() {
        try {
            foreach (string id in new[] { "Dirt", "Sand" }) {
                ElementCatalogAdapter.Rules[id] = MaterialRule.ForTier(id, MaterialTier.Common);
                RecipeRegistry.AnalyzerRecipes[id] = new ComplexRecipe { id = id, ingredients = new[] {
                    new ComplexRecipe.Ingredient { material = new Tag(id), amount = 10f } } };
            }
            foreach (int count in new[] { 1, 3, -1 }) {
                var a = Create(count, 2);
                a.CompleteWorkingOrder();
                Check(a.Queues["Dirt"] == 0, "Completed material must have no duplicate batches");
                Check(a.CurrentWorkingOrder == RecipeRegistry.AnalyzerRecipes["Sand"], "Other material must remain runnable");
                Check(a.Queues["Sand"] == 2, "Other material queue must be preserved");
                Check(ForbiddenTechSaveData.Instance.GetUnlockState().IsUnlocked("Dirt"), "Completed ingredient must unlock even after next batch starts");
                a.CompleteWorkingOrder();
                Check(a.CurrentWorkingOrder == null && a.Recipes.Length == 0, "Idle completion must apply deferred recipe refresh");
            }
            var deferred = Create(1, 2);
            ForbiddenTechSaveData.Instance.Unlock(new Tag("Sand"));
            Check(deferred.Recipes.Length == 2, "External unlock must defer list replacement during work");
            deferred.CurrentWorkingOrder = null; // Engine cancellation/power loss reaches an idle boundary.
            deferred.CanStart = false;
            deferred.Sim1000ms(1f);
            Check(deferred.Recipes.Length == 1 && deferred.Recipes[0].id == "Dirt", "Idle simulation must flush an externally requested refresh");
            var failed = Create(3, 2);
            failed.FailCompletion = true;
            try { failed.CompleteWorkingOrder(); } catch (InvalidOperationException) {}
            Check(!ForbiddenTechSaveData.Instance.GetUnlockState().IsUnlocked("Dirt"), "Failed completion must not unlock");
            Check(failed.Queues["Sand"] == 2, "Completion failure must not erase other queues");
            Console.WriteLine("Analyzer completion behavior passed: finite/infinite duplicate queues, other materials, deferred external refresh, failed completion.");
            return 0;
        } catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
'@
[System.IO.File]::WriteAllText($behaviorSource, $behavior, [System.Text.UTF8Encoding]::new($false))
$coreSources = @(Get-ChildItem -LiteralPath (Join-Path $ProjectRoot 'src\Core') -Filter '*.cs' -Recurse -File | ForEach-Object { $_.FullName })
& $compiler /nologo /target:exe /langversion:7.3 "/out:$behaviorAssembly" $behaviorSource @coreSources (Join-Path $ProjectRoot 'src\Game\Buildings\Analyzer\MatterAnalyzer.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $behaviorAssembly
exit $LASTEXITCODE
