[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
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

$gamePath = 'D:\steam\steamapps\common\OxygenNotIncluded'
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
[System.IO.File]::WriteAllText((Join-Path $probeDirectory 'CrusherRailProbe.runtimeconfig.json'), '{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}')
& dotnet $probeAssembly (Join-Path $gamePath 'OxygenNotIncluded_Data\Managed') (Join-Path $ProjectRoot 'obj\ForbiddenTechnologyPack.raw.dll') (Join-Path $ProjectRoot 'lib')
exit $LASTEXITCODE
