[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)]
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$managedDirectory = Join-Path $gamePath 'OxygenNotIncluded_Data\Managed'
$gameAssemblyPath = Join-Path $managedDirectory 'Assembly-CSharp.dll'
if (-not (Test-Path -LiteralPath $gameAssemblyPath -PathType Leaf)) {
    throw "GamePath does not contain the required game assembly: '$gameAssemblyPath'."
}
$rawAssembly = Join-Path $ProjectRoot 'obj\ForbiddenTechnologyPack.raw.dll'
$probeDirectory = Join-Path $ProjectRoot 'test-artifacts\safe-removal-runtime'
$probeSource = Join-Path $probeDirectory 'SafeRemovalRuntimeProbe.cs'
$probeAssembly = Join-Path $probeDirectory 'SafeRemovalRuntimeProbe.exe'
$runtimeConfig = Join-Path $probeDirectory 'SafeRemovalRuntimeProbe.runtimeconfig.json'

& (Join-Path $ProjectRoot 'build.ps1') -GamePath $gamePath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

New-Item -ItemType Directory -Force -Path $probeDirectory | Out-Null
$probe = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

internal static class SafeRemovalRuntimeProbe {
    private sealed class Instruction {
        internal OpCode Code;
        internal object Operand;
    }

    private static readonly Dictionary<short, OpCode> Codes =
        typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null))
            .ToDictionary(code => code.Value);
    private static string[] directories;

    private static void Check(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static List<Instruction> Read(MethodInfo method) {
        var result = new List<Instruction>();
        var body = method.GetMethodBody();
        if (body == null) return result;
        var il = body.GetILAsByteArray();
        var typeArguments = method.DeclaringType.IsGenericType
            ? method.DeclaringType.GetGenericArguments() : Type.EmptyTypes;
        var methodArguments = method.IsGenericMethod
            ? method.GetGenericArguments() : Type.EmptyTypes;
        for (var offset = 0; offset < il.Length;) {
            short value = il[offset++];
            if (value == 0xfe) value = (short)(0xfe00 | il[offset++]);
            var code = Codes[value];
            object operand = null;
            switch (code.OperandType) {
                case OperandType.InlineNone:
                    break;
                case OperandType.ShortInlineBrTarget:
                    offset++;
                    break;
                case OperandType.ShortInlineVar:
                    operand = (int)il[offset++];
                    break;
                case OperandType.ShortInlineI:
                    operand = (int)(sbyte)il[offset++];
                    break;
                case OperandType.InlineVar:
                    operand = (int)BitConverter.ToUInt16(il, offset);
                    offset += 2;
                    break;
                case OperandType.InlineI:
                    operand = BitConverter.ToInt32(il, offset);
                    offset += 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    offset += 8;
                    break;
                case OperandType.InlineSwitch:
                    var count = BitConverter.ToInt32(il, offset);
                    offset += 4 + 4 * count;
                    break;
                case OperandType.InlineMethod:
                    operand = method.Module.ResolveMethod(BitConverter.ToInt32(il, offset),
                        typeArguments, methodArguments);
                    offset += 4;
                    break;
                case OperandType.InlineField:
                    operand = method.Module.ResolveField(BitConverter.ToInt32(il, offset),
                        typeArguments, methodArguments);
                    offset += 4;
                    break;
                case OperandType.InlineType:
                    operand = method.Module.ResolveType(BitConverter.ToInt32(il, offset),
                        typeArguments, methodArguments);
                    offset += 4;
                    break;
                case OperandType.InlineTok:
                    operand = method.Module.ResolveMember(BitConverter.ToInt32(il, offset),
                        typeArguments, methodArguments);
                    offset += 4;
                    break;
                case OperandType.InlineString:
                    operand = method.Module.ResolveString(BitConverter.ToInt32(il, offset));
                    offset += 4;
                    break;
                default:
                    offset += 4;
                    break;
            }
            result.Add(new Instruction { Code = code, Operand = operand });
        }
        return result;
    }

    private static IEnumerable<MethodBase> Calls(MethodInfo method) {
        return Read(method).Where(instruction => instruction.Operand is MethodBase)
            .Select(instruction => (MethodBase)instruction.Operand);
    }

    private static int? Constant(Instruction instruction) {
        if (instruction.Code == OpCodes.Ldc_I4_M1) return -1;
        if (instruction.Code == OpCodes.Ldc_I4_0) return 0;
        if (instruction.Code == OpCodes.Ldc_I4_1) return 1;
        if (instruction.Code == OpCodes.Ldc_I4_2) return 2;
        if (instruction.Code == OpCodes.Ldc_I4_3) return 3;
        if (instruction.Code == OpCodes.Ldc_I4_4) return 4;
        if (instruction.Code == OpCodes.Ldc_I4_5) return 5;
        if (instruction.Code == OpCodes.Ldc_I4_6) return 6;
        if (instruction.Code == OpCodes.Ldc_I4_7) return 7;
        if (instruction.Code == OpCodes.Ldc_I4_8) return 8;
        if (instruction.Code == OpCodes.Ldc_I4 || instruction.Code == OpCodes.Ldc_I4_S)
            return (int)instruction.Operand;
        return null;
    }

    private static int? LocalIndex(Instruction instruction, bool store) {
        if (store) {
            if (instruction.Code == OpCodes.Stloc_0) return 0;
            if (instruction.Code == OpCodes.Stloc_1) return 1;
            if (instruction.Code == OpCodes.Stloc_2) return 2;
            if (instruction.Code == OpCodes.Stloc_3) return 3;
            if (instruction.Code == OpCodes.Stloc || instruction.Code == OpCodes.Stloc_S)
                return (int)instruction.Operand;
        } else {
            if (instruction.Code == OpCodes.Ldloc_0) return 0;
            if (instruction.Code == OpCodes.Ldloc_1) return 1;
            if (instruction.Code == OpCodes.Ldloc_2) return 2;
            if (instruction.Code == OpCodes.Ldloc_3) return 3;
            if (instruction.Code == OpCodes.Ldloc || instruction.Code == OpCodes.Ldloc_S)
                return (int)instruction.Operand;
        }
        return null;
    }

    private static int CapturedLocal(List<Instruction> instructions, string methodName) {
        var getter = instructions.FindIndex(instruction => {
            var method = instruction.Operand as MethodInfo;
            return method != null && method.Name == methodName;
        });
        Check(getter >= 0 && getter + 1 < instructions.Count,
            "Proto-Matter replacement must capture " + methodName + ".");
        var local = LocalIndex(instructions[getter + 1], true);
        Check(local.HasValue,
            "Proto-Matter replacement must retain " + methodName + " for the native spawn call.");
        return local.Value;
    }

    private static bool CallsClosedGeneric(MethodInfo caller, MethodInfo definition, Type argument) {
        return Calls(caller).OfType<MethodInfo>().Any(call => call.IsGenericMethod &&
            call.GetGenericMethodDefinition() == definition &&
            call.GetGenericArguments().Length == 1 && call.GetGenericArguments()[0] == argument);
    }

    private static int Main(string[] args) {
        directories = new[] { args[0], Path.GetDirectoryName(args[1]), args[2] };
        AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => {
            var name = new AssemblyName(eventArgs.Name).Name + ".dll";
            foreach (var directory in directories) {
                var path = Path.Combine(directory, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };

        try {
            var unity = Assembly.LoadFrom(Path.Combine(args[0], "UnityEngine.CoreModule.dll"));
            var game = Assembly.LoadFrom(Path.Combine(args[0], "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(args[1]);
            var controller = mod.GetType(
                "ForbiddenTechnologyPack.Game.Safety.SafeRemovalController", true);
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var methods = controller.GetMethods(flags);
            var finder = methods.SingleOrDefault(method => method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 0 && method.ReturnType.IsArray &&
                method.ReturnType.GetElementType() == method.GetGenericArguments()[0]);
            Check(finder != null,
                "Safe removal must centralize inclusive discovery in a zero-argument generic array helper.");

            var finderInstructions = Read(finder);
            var findCallIndex = finderInstructions.FindIndex(instruction => {
                var call = instruction.Operand as MethodInfo;
                return call != null && call.Name == "FindObjectsByType" &&
                    call.DeclaringType.FullName == "UnityEngine.Object" &&
                    call.GetParameters().Length == 2 &&
                    call.GetParameters()[0].ParameterType.FullName == "UnityEngine.FindObjectsInactive" &&
                    call.GetParameters()[1].ParameterType.FullName == "UnityEngine.FindObjectsSortMode";
            });
            Check(findCallIndex >= 2,
                "Safe-removal discovery must call Object.FindObjectsByType<T>(FindObjectsInactive, FindObjectsSortMode).");
            var inactiveType = unity.GetType("UnityEngine.FindObjectsInactive", true);
            var sortType = unity.GetType("UnityEngine.FindObjectsSortMode", true);
            var include = Convert.ToInt32(Enum.Parse(inactiveType, "Include"));
            var none = Convert.ToInt32(Enum.Parse(sortType, "None"));
            Check(Constant(finderInstructions[findCallIndex - 2]) == include &&
                Constant(finderInstructions[findCallIndex - 1]) == none,
                "Safe-removal discovery must include inactive objects without sorting.");

            var execute = controller.GetMethod("Execute", flags);
            var convert = controller.GetMethod("ConvertProtoMatter", flags);
            var count = controller.GetMethod("CountRemainingCustomObjects", flags);
            foreach (var typeName in new[] {
                    "ForbiddenTechnologyPack.Game.Buildings.Analyzer.MatterAnalyzer",
                    "ForbiddenTechnologyPack.Game.Buildings.Crusher.MassCrusher",
                    "ForbiddenTechnologyPack.Game.Buildings.Compiler.MatterCompiler",
                    "ForbiddenTechnologyPack.Game.Buildings.Reconstructor.MatterReconstructor" }) {
                var type = mod.GetType(typeName, true);
                Check(CallsClosedGeneric(execute, finder, type),
                    "Execute must include inactive " + type.Name + " objects.");
                Check(CallsClosedGeneric(count, finder, type),
                    "Completion counting must include inactive " + type.Name + " objects.");
            }
            var primaryElement = game.GetType("PrimaryElement", true);
            Check(CallsClosedGeneric(convert, finder, primaryElement),
                "Proto-Matter conversion must include inactive loose, stored, and rail objects.");
            Check(!CallsClosedGeneric(count, finder, primaryElement),
                "Same-invocation completion must use conversion outcomes instead of rescanning deferred-delete Proto-Matter.");

            var conversionInstructions = Read(convert);
            var conversionCalls = conversionInstructions
                .Where(instruction => instruction.Operand is MethodBase)
                .Select(instruction => (MethodBase)instruction.Operand).ToArray();
            var spawnIndex = Array.FindIndex(conversionCalls, call => call.Name == "SpawnResource" &&
                call.DeclaringType.FullName == "Substance");
            var destroyIndex = Array.FindIndex(conversionCalls, call => call.Name == "KDestroyGameObject" &&
                call.DeclaringType.FullName == "Util");
            Check(spawnIndex >= 0,
                "Proto-Matter conversion must spawn the native Igneous Rock resource prefab.");
            Check(destroyIndex > spawnIndex,
                "Proto-Matter conversion must destroy the original custom GameObject after spawning its replacement.");
            Check(!conversionCalls.Any(call => call.Name == "SetElement" &&
                    call.DeclaringType.FullName == "PrimaryElement"),
                "Proto-Matter conversion must not mutate the custom prefab with PrimaryElement.SetElement.");
            foreach (var getter in new[] { "get_Mass", "get_Temperature", "get_DiseaseIdx", "get_DiseaseCount" }) {
                Check(conversionCalls.Any(call => call.Name == getter &&
                        call.DeclaringType.FullName == "PrimaryElement"),
                    "Proto-Matter replacement must preserve " + getter.Substring(4) + ".");
            }
            Check(conversionCalls.Any(call => call.Name == "FindElementByHash" &&
                    call.DeclaringType.FullName == "ElementLoader"),
                "Proto-Matter replacement must resolve the native Igneous Rock element.");

            var spawnInstruction = conversionInstructions.FindIndex(instruction => {
                var call = instruction.Operand as MethodInfo;
                return call != null && call.Name == "SpawnResource" &&
                    call.DeclaringType.FullName == "Substance";
            });
            Check(spawnInstruction >= 8,
                "Proto-Matter replacement must supply the complete native spawn payload.");
            var positionLocal = CapturedLocal(conversionInstructions, "GetPosition");
            var massLocal = CapturedLocal(conversionInstructions, "get_Mass");
            var temperatureLocal = CapturedLocal(conversionInstructions, "get_Temperature");
            var diseaseIndexLocal = CapturedLocal(conversionInstructions, "get_DiseaseIdx");
            var diseaseCountLocal = CapturedLocal(conversionInstructions, "get_DiseaseCount");
            var expectedLocals = new[] {
                positionLocal, massLocal, temperatureLocal, diseaseIndexLocal, diseaseCountLocal
            };
            for (var argument = 0; argument < expectedLocals.Length; argument++) {
                var actual = LocalIndex(
                    conversionInstructions[spawnInstruction - 8 + argument], false);
                Check(actual == expectedLocals[argument],
                    "SpawnResource must receive the captured position, mass, temperature, disease index, and disease count in order.");
            }
            Check(Constant(conversionInstructions[spawnInstruction - 3]) == 1,
                "Native replacement must prevent merging so each object's captured state is preserved.");
            Check(Constant(conversionInstructions[spawnInstruction - 2]) == 0 &&
                Constant(conversionInstructions[spawnInstruction - 1]) == 0,
                "Native replacement must use the game's ordinary temperature and activation path.");

            var findElementInstruction = conversionInstructions.FindIndex(instruction => {
                var call = instruction.Operand as MethodInfo;
                return call != null && call.Name == "FindElementByHash" &&
                    call.DeclaringType.FullName == "ElementLoader";
            });
            var hashes = game.GetType("SimHashes", true);
            var igneousRock = Convert.ToInt32(Enum.Parse(hashes, "IgneousRock"));
            Check(findElementInstruction > 0 &&
                Constant(conversionInstructions[findElementInstruction - 1]) == igneousRock,
                "Proto-Matter replacement must spawn Igneous Rock, not another native substance.");

            Console.WriteLine(
                "Safe-removal compiled contract passed: inactive discovery, native spawn, state capture, and custom-prefab destruction.");
            return 0;
        } catch (Exception exception) {
            while (exception.InnerException != null) exception = exception.InnerException;
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
'@
[System.IO.File]::WriteAllText($probeSource, $probe, [System.Text.UTF8Encoding]::new($false))

. (Join-Path $ProjectRoot 'scripts\Get-CSharpCompiler.ps1')
$compiler = Get-ForbiddenTechnologyCSharpCompiler -ProjectRoot $ProjectRoot
& $compiler /nologo /target:exe /langversion:7.3 "/out:$probeAssembly" $probeSource
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$runtimeConfigBody = @'
{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}
'@
[System.IO.File]::WriteAllText($runtimeConfig, $runtimeConfigBody, [System.Text.UTF8Encoding]::new($false))
& dotnet $probeAssembly $managedDirectory $rawAssembly (Join-Path $ProjectRoot 'lib')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Execute the actual controller source against a deterministic managed boundary.
# Unity native objects cannot be instantiated in this CLI process, so this boundary
# models the two relevant engine contracts: SpawnResource may return null, and
# KDestroyGameObject leaves an object discoverable until the end of the frame.
$behaviorSource = Join-Path $probeDirectory 'SafeRemovalBehavior.cs'
$behaviorAssembly = Join-Path $probeDirectory 'SafeRemovalBehavior.exe'
$behavior = @'
using System;
using System.Collections.Generic;
using System.Linq;
using ForbiddenTechnologyPack.Core;

namespace UnityEngine {
    public enum FindObjectsInactive { Exclude = 0, Include = 1 }
    public enum FindObjectsSortMode { None = 0, InstanceID = 1 }
    public struct Vector3 {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static readonly Vector3 zero = new Vector3();
    }
    public class Object {
        public static readonly List<Object> Registry = new List<Object>();
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode sort)
                where T : Object {
            return Registry.OfType<T>().ToArray();
        }
    }
    public class Transform {
        public Vector3 position;
    }
    public class GameObject : Object {
        private readonly Dictionary<Type, Component> components = new Dictionary<Type, Component>();
        public string name;
        public readonly Transform transform = new Transform();
        public T GetComponent<T>() where T : class {
            Component value;
            return components.TryGetValue(typeof(T), out value) ? value as T : null;
        }
        public void Add(Component component) {
            component.gameObject = this;
            components[component.GetType()] = component;
        }
    }
    public class Component : Object {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
    }
    public static class Debug {
        public static readonly List<string> Errors = new List<string>();
        public static void LogError(object message) { Errors.Add(message == null ? "<null>" : message.ToString()); }
    }
}

public static class TransformExtensions {
    public static UnityEngine.Vector3 GetPosition(this UnityEngine.Transform transform) {
        return transform.position;
    }
}

namespace HarmonyLib {
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HarmonyPatch : Attribute {
        public HarmonyPatch(Type type, string methodName) {}
    }
}

public enum SimHashes { ProtoMatter = 1, IgneousRock = 2 }
public struct Tag { public Tag(string id) {} }

public sealed class PrimaryElement : UnityEngine.Component {
    public SimHashes ElementID;
    public float Mass;
    public float Temperature;
    public byte DiseaseIdx;
    public int DiseaseCount;
}

public sealed class Substance {
    public static readonly Queue<Func<UnityEngine.GameObject>> Results =
        new Queue<Func<UnityEngine.GameObject>>();
    public UnityEngine.GameObject SpawnResource(UnityEngine.Vector3 position, float mass,
            float temperature, byte diseaseIndex, int diseaseCount,
            bool prevent_merge = false, bool forceTemperature = false,
            bool manual_activation = false) {
        return Results.Dequeue()();
    }
}

public sealed class Element { public Substance substance = new Substance(); }
public static class ElementLoader {
    public static readonly Element IgneousRock = new Element();
    public static Element FindElementByHash(SimHashes hash) { return IgneousRock; }
}

public static class Util {
    public static readonly List<UnityEngine.GameObject> Destroyed =
        new List<UnityEngine.GameObject>();
    public static void KDestroyGameObject(UnityEngine.GameObject gameObject) {
        // Deliberately do not remove components from Object.Registry: the real
        // game defers deletion until the end of the frame.
        Destroyed.Add(gameObject);
    }
}

public sealed class Operational : UnityEngine.Component {
    public sealed class Flag {
        public enum Type { Requirement }
        public Flag(string id, Type type) {}
    }
    public void SetFlag(Flag flag, bool value) {}
}
public class Storage {
    public readonly List<UnityEngine.GameObject> items = new List<UnityEngine.GameObject>();
    public void DropAll(bool a, bool b, UnityEngine.Vector3 c, bool d, object e) {}
}
public class ComplexFabricator : UnityEngine.Component {
    public Storage inStorage = new Storage();
    public Storage buildStorage = new Storage();
    public Storage outStorage = new Storage();
    public void SetQueueDirty() {}
}
public sealed class Deconstructable : UnityEngine.Component {
    public bool HasBeenDestroyed;
    public void ForceDestroyAndGetMaterials() { HasBeenDestroyed = true; }
}
public sealed class BuildingDef { public bool ShowInBuildMenu; }
public static class Assets { public static BuildingDef GetBuildingDef(string id) { return null; } }
public sealed class Tech { public readonly List<string> unlockedItemIDs = new List<string>(); }
public sealed class Techs { public Tech TryGet(string id) { return null; } }
public sealed class Db {
    public Techs Techs;
    public static Db Get() { return null; }
}
public sealed class Game { public static Game Instance; }

namespace ForbiddenTechnologyPack.Core {
    public static class ModIdentity {
        public const string MatterAnalyzerId = "Analyzer";
        public const string MassCrusherId = "Crusher";
        public const string MatterCompilerId = "Compiler";
        public const string MatterReconstructorId = "Reconstructor";
        public const string ResearchId = "Research";
        public const string ProtoFieldResearchId = "ProtoFieldResearch";
    }
}
namespace ForbiddenTechnologyPack.Game.Elements {
    public static class ProtoMatterRegistration {
        public static readonly SimHashes Hash = SimHashes.ProtoMatter;
    }
}
namespace ForbiddenTechnologyPack.Game.Save {
    public sealed class ForbiddenTechSaveData {
        public static ForbiddenTechSaveData Instance;
        public bool SafeRemovalStarted;
        public void BeginSafeRemoval() { SafeRemovalStarted = true; }
        public void SetSafeRemovalCompleted(bool value) {}
    }
}
namespace ForbiddenTechnologyPack.Game.Buildings.Analyzer {
    public class MatterAnalyzer : ComplexFabricator {}
}
namespace ForbiddenTechnologyPack.Game.Buildings.Crusher {
    public class MassCrusher : ComplexFabricator {}
}
namespace ForbiddenTechnologyPack.Game.Buildings.Compiler {
    public class MatterCompiler : ComplexFabricator {}
    public sealed class CoolantController : UnityEngine.Component { public Storage storage; }
}
namespace ForbiddenTechnologyPack.Game.Buildings.Reconstructor {
    public class MatterReconstructor : ComplexFabricator {}
}

internal static class SafeRemovalBehavior {
    private static void Check(bool condition, string message) {
        if (!condition) throw new InvalidOperationException(message);
    }
    private static PrimaryElement Proto(string name, float mass) {
        var gameObject = new UnityEngine.GameObject { name = name };
        gameObject.transform.position = new UnityEngine.Vector3(mass, mass + 1f, mass + 2f);
        var primary = new PrimaryElement {
            ElementID = SimHashes.ProtoMatter,
            Mass = mass,
            Temperature = 273.15f + mass,
            DiseaseIdx = (byte)mass,
            DiseaseCount = (int)mass * 10
        };
        gameObject.Add(primary);
        UnityEngine.Object.Registry.Add(primary);
        return primary;
    }
    private static int Main() {
        try {
            var nullFailed = Proto("failed-null-spawn", 3f);
            var throwingFailed = Proto("failed-throwing-spawn", 5f);
            var successful = Proto("successful-deferred-destroy", 7f);
            Substance.Results.Enqueue(() => null);
            Substance.Results.Enqueue(() => {
                throw new InvalidOperationException("synthetic SpawnResource failure");
            });
            Substance.Results.Enqueue(() =>
                new UnityEngine.GameObject { name = "igneous-rock" });
            Game.Instance = new Game();
            ForbiddenTechnologyPack.Game.Save.ForbiddenTechSaveData.Instance =
                new ForbiddenTechnologyPack.Game.Save.ForbiddenTechSaveData();
            var report = ForbiddenTechnologyPack.Game.Safety.SafeRemovalController.Execute();

            Check(report.RemainingCustomObjectCount == 2 && !report.IsComplete,
                "Null and throwing spawns must both remain unresolved.");
            Check(!Util.Destroyed.Contains(nullFailed.gameObject),
                "A null replacement must retain the original Proto-Matter GameObject.");
            Check(!Util.Destroyed.Contains(throwingFailed.gameObject),
                "A throwing replacement must retain the original Proto-Matter GameObject.");
            Check(Util.Destroyed.Count == 1 && Util.Destroyed[0] == successful.gameObject,
                "Only a valid native replacement may schedule its custom original for destruction.");
            Check(report.ConvertedObjectCount == 1 && report.ConvertedMassKg == 7f,
                "Only the successfully scheduled replacement may be recorded as converted.");
            Check(UnityEngine.Object.Registry.OfType<PrimaryElement>().Count() == 3,
                "The behavior boundary must preserve deferred-destruction visibility.");
            Check(UnityEngine.Debug.Errors.Any(message => message.Contains("failed-null-spawn")),
                "Null replacement failure must produce a clear object-specific error.");
            Check(UnityEngine.Debug.Errors.Any(message =>
                    message.Contains("failed-throwing-spawn") &&
                    message.Contains("synthetic SpawnResource failure")),
                "Throwing replacement failure must produce a clear object-specific error.");
            Console.WriteLine(
                "Safe-removal behavior passed: null/throwing spawns retained and counted; following deferred destruction converted.");
            return 0;
        } catch (Exception exception) {
            while (exception.InnerException != null) exception = exception.InnerException;
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
'@
[System.IO.File]::WriteAllText($behaviorSource, $behavior, [System.Text.UTF8Encoding]::new($false))
& $compiler /nologo /target:exe /langversion:7.3 "/out:$behaviorAssembly" `
    $behaviorSource `
    (Join-Path $ProjectRoot 'src\Core\SafeRemovalReport.cs') `
    (Join-Path $ProjectRoot 'src\Game\Safety\SafeRemovalController.cs')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $behaviorAssembly
exit $LASTEXITCODE
