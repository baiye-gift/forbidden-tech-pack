[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$gamePath = 'D:\steam\steamapps\common\OxygenNotIncluded'
$managedDirectory = Join-Path $gamePath 'OxygenNotIncluded_Data\Managed'
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
                    "ForbiddenTechnologyPack.Game.Buildings.Compiler.MatterCompiler" }) {
                var type = mod.GetType(typeName, true);
                Check(CallsClosedGeneric(execute, finder, type),
                    "Execute must include inactive " + type.Name + " objects.");
                Check(CallsClosedGeneric(count, finder, type),
                    "Completion counting must include inactive " + type.Name + " objects.");
            }
            var primaryElement = game.GetType("PrimaryElement", true);
            Check(CallsClosedGeneric(convert, finder, primaryElement),
                "Proto-Matter conversion must include inactive loose, stored, and rail objects.");
            Check(CallsClosedGeneric(count, finder, primaryElement),
                "Completion counting must include inactive Proto-Matter objects.");

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
exit $LASTEXITCODE
