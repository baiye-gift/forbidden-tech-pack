[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot,
    [Parameter(Mandatory = $true)]
    [string]$GamePath
)

$ErrorActionPreference = 'Stop'

function Require-Condition {
    param(
        [bool]$Condition,
        [string]$Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

$sourcePath = Join-Path $ProjectRoot 'src\Game\Buildings\Crusher\MassCrusherConfig.cs'
$source = Get-Content -Raw -LiteralPath $sourcePath

Require-Condition ($source -match 'fabricator\.outStorage\.allowItemRemoval\s*=\s*true') `
    'Mass Crusher output must permit manual item removal when no rail is attached.'
Require-Condition ($source -match 'fabricator\.outStorage\.allowUIItemRemoval\s*=\s*true') `
    'Mass Crusher output must expose manual item removal in the storage UI.'
Require-Condition ($source -match '2400f\s*\*\s*ForbiddenTechOptions\.Current\.PowerMultiplier') `
    'Mass Crusher active power must use the resolved power multiplier.'
Require-Condition ($source -match '40f\s*\*\s*ForbiddenTechOptions\.Current\.HeatMultiplier') `
    'Mass Crusher self heat must use the resolved heat multiplier.'

Write-Host 'Mass Crusher config contract validation passed.'

$gameAssemblyPath = Join-Path $GamePath 'OxygenNotIncluded_Data\Managed\Assembly-CSharp.dll'
Require-Condition (Test-Path -LiteralPath $gameAssemblyPath -PathType Leaf) `
    "GamePath does not contain the required game assembly: '$gameAssemblyPath'."
& (Join-Path $ProjectRoot 'build.ps1') -GamePath $gamePath
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
$probeDirectory = Join-Path $ProjectRoot 'test-artifacts\crusher-rail-runtime'
New-Item -ItemType Directory -Force -Path $probeDirectory | Out-Null
$probeSource = Join-Path $probeDirectory 'CrusherRailProbe.cs'
$probeAssembly = Join-Path $probeDirectory 'CrusherRailProbe.exe'
$probe = @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
internal static class CrusherRailProbe {
    private static string[] directories;
    private static IEnumerable<MemberInfo> Calls(MethodInfo method) {
        var codes = typeof(OpCodes).GetFields().Where(f => f.FieldType == typeof(OpCode))
            .Select(f => (OpCode)f.GetValue(null)).ToDictionary(c => c.Value);
        var il = method.GetMethodBody().GetILAsByteArray();
        for (int i = 0; i < il.Length;) {
            short value = il[i++];
            if (value == 0xfe) value = (short)(0xfe00 | il[i++]);
            var code = codes[value];
            if (code.OperandType == OperandType.InlineMethod) yield return method.Module.ResolveMethod(BitConverter.ToInt32(il, i));
            if (code.OperandType == OperandType.InlineField) yield return method.Module.ResolveField(BitConverter.ToInt32(il, i));
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
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static int Main(string[] args) {
        directories = new[] { args[0], Path.GetDirectoryName(args[1]), args[2] };
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
            string name = new AssemblyName(e.Name).Name + ".dll";
            foreach (string directory in directories) {
                string path = Path.Combine(directory, name);
                if (File.Exists(path)) return Assembly.LoadFrom(path);
            }
            return null;
        };
        try {
            var game = Assembly.LoadFrom(Path.Combine(args[0], "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(args[1]);
            var payment = mod.GetType("ForbiddenTechnologyPack.Game.Buildings.Common.FabricatorPaymentPatch", true);
            var targets = (IEnumerable<MethodBase>)payment.GetMethod("TargetMethods", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            var methods = targets.ToArray();
            Check(methods.Length == 2 && methods.All(m => m != null && m.DeclaringType == game.GetType("CircuitManager")), "Payment hooks must resolve against the current native CircuitManager.");
            foreach (var method in methods) {
                Check(((MethodInfo)method).ReturnType == typeof(float), "Native payment methods must return unpaid joules.");
                var parameters = method.GetParameters();
                Check(parameters.Any(p => p.Name == "joules_needed" && p.ParameterType == typeof(float)) &&
                    parameters.Any(p => p.Name == "c" && p.ParameterType.Name == "IEnergyConsumer"), "Payment hook parameter names/types changed.");
            }
            var nativeFlags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
            var circuit=game.GetType("CircuitManager",true);
            Check(circuit.GetMethod("Sim200msLast",nativeFlags)!=null,"The native power epoch hook must exist.");
            var energyStatus=game.GetType("EnergyConsumer",true).GetMethod("SetConnectionStatus",nativeFlags);
            Check(energyStatus.GetParameters().Any(p=>p.Name=="connection_status"),"Native power status hook parameter must match Harmony binding.");
            var clock=game.GetType("GameClock",true);
            Check(clock.GetMethod("GetCycle",nativeFlags).ReturnType==typeof(int) && clock.GetMethod("GetTimeSinceStartOfCycle",nativeFlags).ReturnType==typeof(float),"Long-colony time must use stable native cycle components.");
            var nativeDispenser=game.GetType("ConduitDispenser",true);
            Check(nativeDispenser.GetMethod("FindSuitableElement",nativeFlags).ReturnType.Name=="PrimaryElement" && nativeDispenser.GetMethod("Dispense",nativeFlags)!=null,
                "Liquid batching must preserve current native candidate and transport hooks.");
            var coolant = mod.GetType("ForbiddenTechnologyPack.Game.Buildings.Compiler.CoolantController", true);
            var latch = coolant.GetField("releasingHeatedPacket", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(latch != null && latch.GetCustomAttributesData().Any(a => a.AttributeType.Name == "Serialize" || a.AttributeType.Name == "SerializeAttribute"), "The old coolant save latch must remain serialized.");
            var process = coolant.GetMethod("TryProcessWork", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(Calls(process).OfType<MethodInfo>().Any(m => m.DeclaringType.Name == "Storage" && m.Name == "Transfer" &&
                m.ReturnType == typeof(float) && m.GetParameters().Length == 5), "Paid work must split through the current native storage transfer API.");
            Console.WriteLine("Native payment API and coolant transfer/save contract passed (reflection/IL; no Unity/Sim).");
            var reactor = mod.GetType("ForbiddenTechnologyPack.Game.Buildings.MassEnergy.ProtoAnnihilationReactor", true);
            var reactorCalls = Calls(reactor.GetMethod("Sim200ms")).OfType<MethodInfo>().ToArray();
            var consumeFuel = reactorCalls.Single(m => m.DeclaringType.Name == "Storage" && m.Name == "ConsumeAndGetDisease");
            Check(consumeFuel.GetParameters()[2].IsOut && consumeFuel.GetParameters()[2].ParameterType == typeof(float).MakeByRefType(), "Reactor fuel receipt must use the native actual-mass out parameter.");
            Check(reactorCalls.Any(m => m.DeclaringType.Name == "Generator" && m.Name == "GenerateJoules"), "Reactor must supply the native generator buffer.");
            foreach (string field in new[] { "ignitionJoules", "totalFuelKg", "totalGrossJoules" })
                Check(reactor.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetCustomAttributesData().Any(a => a.AttributeType.Name.StartsWith("Serialize")), "Reactor save field must remain serialized: " + field);
            var condenser = mod.GetType("ForbiddenTechnologyPack.Game.Buildings.MassEnergy.EnergyMatterCondenser", true);
            Check(condenser.BaseType == game.GetType("ComplexFabricator"), "Condenser must retain the native queue and progress owner.");
            foreach (string type in new[] { "BaiyeProtoAnnihilationReactorConfig", "BaiyeEnergyMatterCondenserConfig" })
                Check(mod.GetType("ForbiddenTechnologyPack.Game.Buildings.MassEnergy." + type, true).BaseType == game.GetType("IBuildingConfig"), "Mass-energy building must use the native prefab contract.");
            Console.WriteLine("Native mass-energy fuel/generator, save fields and fabricator/prefab contracts passed (reflection/IL; no Unity/Sim).");
            var patch = mod.GetType("ForbiddenTechnologyPack.Game.Buildings.Crusher.MassCrusherRailInputPatch");
            Check(patch != null, "Crusher must filter rail packets before SolidConduitConsumer removes them.");
            var attribute = patch.GetCustomAttributesData().Single(a => a.AttributeType.Name == "HarmonyPatch");
            Check((Type)attribute.ConstructorArguments[0].Value == game.GetType("SolidConduitConsumer") &&
                (string)attribute.ConstructorArguments[1].Value == "ConduitUpdate", "Rail guard must patch SolidConduitConsumer.ConduitUpdate.");
            var prefix = patch.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
            Check(prefix != null && prefix.ReturnType == typeof(bool), "Rail guard must be a bool prefix capable of skipping consumption.");
            var calls = Calls(prefix).ToArray();
            Check(calls.OfType<MethodInfo>().Any(m => m.Name == "GetComponent" && m.IsGenericMethod && m.GetGenericArguments()[0].Name == "MassCrusher"), "Guard must be scoped to MassCrusher.");
            foreach (string name in new[] { "get_IsConnected", "GetUtilityInputCell", "GetContents", "IsValid", "GetPickupable", "get_PrimaryElement", "ElementID", "CanAcceptElement" })
                Check(calls.Any(m => m.Name == name), "Rail guard must inspect live packet through " + name + ".");
            Check(!calls.Any(m => m.Name == "OffsetCell" || m.Name == "RemovePickupable" || m.Name == "Store"), "Guard must use the configured cell and leave rail/storage mutation to the consumer.");
            var inputCell = game.GetType("SolidConduitConsumer").GetMethod("GetInputCell", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(Calls(inputCell).Any(m => m.Name == "GetUtilityInputCell"), "Current game's primary solid consumer cell contract changed.");
            var accept = patch.GetMethod("CanAcceptElement", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            Check(accept != null, "Rail acceptance predicate is missing.");
            var adapter = mod.GetType("ForbiddenTechnologyPack.Game.Elements.ElementCatalogAdapter", true);
            var ruleType = mod.GetType("ForbiddenTechnologyPack.Core.MaterialRule", true);
            var tierType = mod.GetType("ForbiddenTechnologyPack.Core.MaterialTier", true);
            var rules = (IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), ruleType));
            object rule = Activator.CreateInstance(ruleType, new object[] { "Dirt", Enum.Parse(tierType, "Common"), 1f });
            rules.Add("Dirt", rule);
            // Even an accidentally catalogued Proto-Matter packet must be rejected.
            string proto = (string)mod.GetType("ForbiddenTechnologyPack.Core.ModIdentity").GetField("ProtoMatterId").GetRawConstantValue();
            rules.Add(proto, Activator.CreateInstance(ruleType, new object[] { proto, Enum.Parse(tierType, "Common"), 1f }));
            adapter.GetField("rules", BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, rules);
            foreach (string id in new[] { null, "", "UnknownElement", "Unobtanium", proto, "Dirt" }) {
                bool actual = (bool)accept.Invoke(null, new object[] { id });
                Check(actual == (id == "Dirt"), "Unexpected rail acceptance for " + (id ?? "<null>"));
            }
            Console.WriteLine("Crusher rail compiled contract and live predicate passed (valid, unknown, forbidden, empty IDs). Native rail movement requires in-game validation.");
            return 0;
        } catch (Exception e) { while (e.InnerException != null) e = e.InnerException; Console.Error.WriteLine(e); return 1; }
    }
}
'@
[System.IO.File]::WriteAllText($probeSource, $probe, [System.Text.UTF8Encoding]::new($false))
. (Join-Path $ProjectRoot 'scripts\Get-CSharpCompiler.ps1')
$compiler = Get-ForbiddenTechnologyCSharpCompiler -ProjectRoot $ProjectRoot
& $compiler /nologo /target:exe /langversion:7.3 "/out:$probeAssembly" $probeSource
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
[System.IO.File]::WriteAllText((Join-Path $probeDirectory 'CrusherRailProbe.runtimeconfig.json'), '{"runtimeOptions":{"tfm":"net6.0","rollForward":"Major","framework":{"name":"Microsoft.NETCore.App","version":"6.0.0"}}}')
& dotnet $probeAssembly (Join-Path $gamePath 'OxygenNotIncluded_Data\Managed') (Join-Path $ProjectRoot 'obj\ForbiddenTechnologyPack.raw.dll') (Join-Path $ProjectRoot 'lib')
exit $LASTEXITCODE
