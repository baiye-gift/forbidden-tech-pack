[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)]
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'
$managedDirectory = Join-Path $GamePath 'OxygenNotIncluded_Data\Managed'
$gameAssemblyPath = Join-Path $managedDirectory 'Assembly-CSharp.dll'
if (-not (Test-Path -LiteralPath $gameAssemblyPath -PathType Leaf)) {
    throw "GamePath does not contain the required game assembly: '$gameAssemblyPath'."
}

# Build the actual mod against the user's current ONI assemblies first. This is the
# authoritative compile boundary for SafeRemovalController and every building it references.
& (Join-Path $ProjectRoot 'build.ps1') -GamePath $GamePath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$rawAssembly = Join-Path $ProjectRoot 'obj\ForbiddenTechnologyPack.raw.dll'
if (-not (Test-Path -LiteralPath $rawAssembly -PathType Leaf)) {
    throw "Raw mod assembly was not produced: '$rawAssembly'."
}

$controllerSourcePath = Join-Path $ProjectRoot 'src\Game\Safety\SafeRemovalController.cs'
$controllerSource = Get-Content -LiteralPath $controllerSourcePath -Raw
foreach ($token in @(
    'catch (System.Exception exception)',
    'replacement == null',
    'Util.KDestroyGameObject(primary.gameObject)',
    'prevent_merge: true',
    'ProcessEntropyDiverters',
    'ProcessAnnihilationReactors',
    'controller.PrepareForSafeRemoval()',
    'controller.coolantStorage',
    'controller.protoMatterStorage')) {
    if (-not $controllerSource.Contains($token)) {
        throw "Safe removal source is missing required failure/integration boundary '$token'."
    }
}

$probeDirectory = Join-Path $ProjectRoot 'test-artifacts\safe-removal-runtime'
$probeSource = Join-Path $probeDirectory 'SafeRemovalRuntimeProbe.cs'
$probeAssembly = Join-Path $probeDirectory 'SafeRemovalRuntimeProbe.exe'
$runtimeConfig = Join-Path $probeDirectory 'SafeRemovalRuntimeProbe.runtimeconfig.json'
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
                case OperandType.ShortInlineI:
                case OperandType.ShortInlineVar:
                    offset += 1;
                    break;
                case OperandType.InlineVar:
                    offset += 2;
                    break;
                case OperandType.InlineI:
                case OperandType.ShortInlineR:
                case OperandType.InlineBrTarget:
                    offset += 4;
                    break;
                case OperandType.InlineI8:
                case OperandType.InlineR:
                    offset += 8;
                    break;
                case OperandType.InlineSwitch:
                    var count = BitConverter.ToInt32(il, offset);
                    offset += 4 + count * 4;
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
        return Read(method)
            .Where(instruction => instruction.Operand is MethodBase)
            .Select(instruction => (MethodBase)instruction.Operand);
    }

    private static bool CallsClosedGeneric(MethodInfo caller, MethodInfo definition, Type argument) {
        return Calls(caller).OfType<MethodInfo>().Any(call =>
            call.IsGenericMethod && call.GetGenericMethodDefinition() == definition &&
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
            var game = Assembly.LoadFrom(Path.Combine(args[0], "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(args[1]);
            var controller = mod.GetType(
                "ForbiddenTechnologyPack.Game.Safety.SafeRemovalController", true);
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var methods = controller.GetMethods(flags);
            var finder = methods.SingleOrDefault(method =>
                method.Name == "FindAllObjects" && method.IsGenericMethodDefinition &&
                method.GetParameters().Length == 0 && method.ReturnType.IsArray);
            Check(finder != null,
                "Safe removal must centralize inactive-object discovery in FindAllObjects<T>().");

            var execute = controller.GetMethod("Execute", flags);
            var count = controller.GetMethod("CountRemainingCustomObjects", flags);
            var convert = controller.GetMethod("ConvertProtoMatter", flags);
            Check(execute != null && count != null && convert != null,
                "Safe removal core methods are missing.");

            var buildingTypes = new[] {
                "ForbiddenTechnologyPack.Game.Buildings.Analyzer.MatterAnalyzer",
                "ForbiddenTechnologyPack.Game.Buildings.Crusher.MassCrusher",
                "ForbiddenTechnologyPack.Game.Buildings.Compiler.MatterCompiler",
                "ForbiddenTechnologyPack.Game.Buildings.Reconstructor.MatterReconstructor",
                "ForbiddenTechnologyPack.Game.Buildings.EntropyDiverter.EntropyFluxDiverterController",
                "ForbiddenTechnologyPack.Game.Buildings.AnnihilationReactor.MatterAnnihilationReactorController"
            };
            foreach (var typeName in buildingTypes) {
                var type = mod.GetType(typeName, true);
                Check(CallsClosedGeneric(execute, finder, type),
                    "Execute must discover inactive " + type.Name + " objects.");
                Check(CallsClosedGeneric(count, finder, type),
                    "Completion counting must include inactive " + type.Name + " objects.");
            }

            var primaryElement = game.GetType("PrimaryElement", true);
            Check(CallsClosedGeneric(convert, finder, primaryElement),
                "Proto-Matter conversion must discover inactive loose/stored/rail objects.");

            var conversionCalls = Calls(convert).ToArray();
            var spawnIndex = Array.FindIndex(conversionCalls, call =>
                call.Name == "SpawnResource" && call.DeclaringType.FullName == "Substance");
            var destroyIndex = Array.FindIndex(conversionCalls, call =>
                call.Name == "KDestroyGameObject" && call.DeclaringType.FullName == "Util");
            Check(spawnIndex >= 0,
                "Proto-Matter conversion must spawn a native replacement resource.");
            Check(destroyIndex > spawnIndex,
                "The original custom object must be destroyed only after native spawn succeeds.");

            foreach (var getter in new[] {
                    "get_Mass", "get_Temperature", "get_DiseaseIdx", "get_DiseaseCount" }) {
                Check(conversionCalls.Any(call =>
                        call.Name == getter && call.DeclaringType.FullName == "PrimaryElement"),
                    "Proto-Matter conversion must preserve " + getter.Substring(4) + ".");
            }
            Check(conversionCalls.Any(call =>
                    call.Name == "FindElementByHash" &&
                    call.DeclaringType.FullName == "ElementLoader"),
                "Proto-Matter conversion must resolve a native element before spawning.");
            Check(!conversionCalls.Any(call =>
                    call.Name == "SetElement" &&
                    call.DeclaringType.FullName == "PrimaryElement"),
                "Safe removal must replace the custom prefab, not mutate it in place.");

            var processEntropy = controller.GetMethod("ProcessEntropyDiverters", flags);
            var processReactor = controller.GetMethod("ProcessAnnihilationReactors", flags);
            Check(processEntropy != null && processReactor != null,
                "Non-fabricator Phase-2 buildings must have explicit cleanup handlers.");
            Check(Calls(processEntropy).Any(call => call.Name == "PrepareForSafeRemoval"),
                "Entropy Diverter cleanup must disable runtime flow before dropping storage.");
            Check(Calls(processReactor).Any(call => call.Name == "PrepareForSafeRemoval"),
                "Reactor cleanup must disable generation/interference before dropping storage.");

            Console.WriteLine(
                "Safe-removal runtime contract passed: all six buildings, inactive discovery, native Proto-Matter replacement, and Phase-2 cleanup hooks verified.");
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
