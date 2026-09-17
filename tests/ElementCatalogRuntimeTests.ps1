[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$gamePath = 'D:\steam\steamapps\common\OxygenNotIncluded'
$managedDirectory = Join-Path $gamePath 'OxygenNotIncluded_Data\Managed'
$rawAssembly = Join-Path $ProjectRoot 'obj\ForbiddenTechnologyPack.raw.dll'
$probeDirectory = Join-Path $ProjectRoot 'test-artifacts\element-catalog-runtime'
$probeSource = Join-Path $probeDirectory 'ElementCatalogRuntimeProbe.cs'
$probeAssembly = Join-Path $probeDirectory 'ElementCatalogRuntimeProbe.exe'
$runtimeConfig = Join-Path $probeDirectory 'ElementCatalogRuntimeProbe.runtimeconfig.json'

& (Join-Path $ProjectRoot 'build.ps1') -GamePath $gamePath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

New-Item -ItemType Directory -Force -Path $probeDirectory | Out-Null
$source = @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

internal static class ElementCatalogRuntimeProbe {
    private static readonly Dictionary<short, OpCode> OpCodesByValue =
        typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(OpCode))
            .Select(field => (OpCode)field.GetValue(null))
            .ToDictionary(opCode => opCode.Value);

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

    private static IEnumerable<MethodBase> CalledMethods(MethodInfo caller) {
        var body = caller.GetMethodBody();
        if (body == null) {
            yield break;
        }

        var il = body.GetILAsByteArray();
        for (var offset = 0; offset < il.Length;) {
            short value = il[offset++];
            if (value == 0xfe) {
                value = (short)(0xfe00 | il[offset++]);
            }

            OpCode opCode;
            if (!OpCodesByValue.TryGetValue(value, out opCode)) {
                throw new InvalidOperationException("Unknown IL opcode: " + value);
            }

            if (opCode.OperandType == OperandType.InlineMethod) {
                var token = BitConverter.ToInt32(il, offset);
                yield return caller.Module.ResolveMethod(token);
            }
            offset += OperandSize(opCode.OperandType, il, offset);
        }
    }

    private static IEnumerable<string> InlineStrings(MethodInfo caller) {
        var body = caller.GetMethodBody();
        if (body == null) {
            yield break;
        }

        var il = body.GetILAsByteArray();
        for (var offset = 0; offset < il.Length;) {
            short value = il[offset++];
            if (value == 0xfe) {
                value = (short)(0xfe00 | il[offset++]);
            }

            OpCode opCode;
            if (!OpCodesByValue.TryGetValue(value, out opCode)) {
                throw new InvalidOperationException("Unknown IL opcode: " + value);
            }

            if (opCode.OperandType == OperandType.InlineString) {
                var token = BitConverter.ToInt32(il, offset);
                yield return caller.Module.ResolveString(token);
            }
            offset += OperandSize(opCode.OperandType, il, offset);
        }
    }

    private static bool EndsWithFalseReturn(MethodInfo method) {
        var body = method.GetMethodBody();
        if (body == null) {
            return false;
        }
        var il = body.GetILAsByteArray();
        return il.Length >= 2 && il[il.Length - 2] == OpCodes.Ldc_I4_0.Value &&
            il[il.Length - 1] == OpCodes.Ret.Value;
    }

    private static void AssertCanonicalTag(MethodInfo addClassificationTags, string sourceTag,
            string expectedCanonicalTag) {
        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { sourceTag };
        addClassificationTags.Invoke(null, new object[] { tags, "LegacyFixture" });
        if (!tags.Contains(expectedCanonicalTag)) {
            throw new InvalidOperationException(sourceTag + " must canonicalize to " + expectedCanonicalTag + ".");
        }
    }

    private static int OperandSize(OperandType operandType, byte[] il, int offset) {
        switch (operandType) {
        case OperandType.InlineNone:
            return 0;
        case OperandType.ShortInlineBrTarget:
        case OperandType.ShortInlineI:
        case OperandType.ShortInlineVar:
            return 1;
        case OperandType.InlineVar:
            return 2;
        case OperandType.InlineBrTarget:
        case OperandType.InlineField:
        case OperandType.InlineI:
        case OperandType.InlineMethod:
        case OperandType.InlineSig:
        case OperandType.InlineString:
        case OperandType.InlineTok:
        case OperandType.InlineType:
        case OperandType.ShortInlineR:
            return 4;
        case OperandType.InlineI8:
        case OperandType.InlineR:
            return 8;
        case OperandType.InlineSwitch:
            return 4 + (BitConverter.ToInt32(il, offset) * 4);
        default:
            throw new InvalidOperationException("Unsupported IL operand type: " + operandType);
        }
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
            var firstPass = Assembly.LoadFrom(Path.Combine(gameManaged, "Assembly-CSharp-firstpass.dll"));
            var manager = firstPass.GetType("DlcManager", true);
            var subscribed = manager.GetMethod("IsContentSubscribed", BindingFlags.Public | BindingFlags.Static);
            var obsolete = manager.GetMethod("IsContentActive", BindingFlags.Public | BindingFlags.Static);
            if (subscribed == null || obsolete == null) {
                throw new InvalidOperationException("The current game assembly must expose both DLC query APIs.");
            }

            var expansion1Id = (string)manager.GetField("EXPANSION1_ID",
                BindingFlags.Public | BindingFlags.Static).GetValue(null);
            for (var number = 2; number <= 5; number++) {
                var fieldName = "DLC" + number + "_ID";
                var field = manager.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                if (field == null || field.FieldType != typeof(string)) {
                    throw new InvalidOperationException("The current game assembly must expose " + fieldName + ".");
                }
                var id = (string)field.GetValue(null);
                if (string.IsNullOrEmpty(id)) {
                    throw new InvalidOperationException(fieldName + " must have a non-empty value.");
                }
                if (string.Equals(id, expansion1Id, StringComparison.Ordinal)) {
                    throw new InvalidOperationException(fieldName + " must be distinct from EXPANSION1_ID.");
                }
            }

            var obsoleteStrings = InlineStrings(obsolete).ToArray();
            var obsoleteCalls = CalledMethods(obsolete).ToArray();
            var equalityCount = obsoleteCalls.Count(method => method.DeclaringType == typeof(string) &&
                string.Equals(method.Name, "op_Equality", StringComparison.Ordinal));
            var callsSubscribedFromLegacyApi = obsoleteCalls.Any(method => method.DeclaringType == manager &&
                string.Equals(method.Name, "IsContentSubscribed", StringComparison.Ordinal));
            const string newerDlcError = "IsContentActive was called with a newer DLC which is not allowed.";
            if (equalityCount != 2 || !obsoleteStrings.Contains(string.Empty) ||
                    !obsoleteStrings.Contains(expansion1Id) || !obsoleteStrings.Contains(newerDlcError) ||
                    !callsSubscribedFromLegacyApi || !EndsWithFalseReturn(obsolete)) {
                throw new InvalidOperationException(
                    "Current IsContentActive IL must permit only vanilla/EXPANSION1 before its explicit newer-DLC false path.");
            }

            Assembly.LoadFrom(Path.Combine(gameManaged, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(args[1]);
            var adapter = mod.GetType("ForbiddenTechnologyPack.Game.Elements.ElementCatalogAdapter", true);
            var isDlcActive = adapter.GetMethod("IsDlcActive", BindingFlags.Static | BindingFlags.NonPublic);
            if (isDlcActive == null) {
                throw new InvalidOperationException("ElementCatalogAdapter.IsDlcActive was not found.");
            }
            var addClassificationTags = adapter.GetMethod("AddClassificationTags",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (addClassificationTags == null) {
                throw new InvalidOperationException("ElementCatalogAdapter.AddClassificationTags was not found.");
            }
            AssertCanonicalTag(addClassificationTags, "MetalOre", "OreOrOrganic");
            AssertCanonicalTag(addClassificationTags, "Organic", "OreOrOrganic");
            AssertCanonicalTag(addClassificationTags, "Agricultural", "Common");

            var calls = CalledMethods(isDlcActive).ToArray();
            var callsSubscribed = calls.Any(method => method.DeclaringType == manager &&
                string.Equals(method.Name, "IsContentSubscribed", StringComparison.Ordinal));
            var callsObsolete = calls.Any(method => method.DeclaringType == manager &&
                string.Equals(method.Name, "IsContentActive", StringComparison.Ordinal));
            if (!callsSubscribed || callsObsolete) {
                throw new InvalidOperationException(
                    "ElementCatalogAdapter.IsDlcActive must call DlcManager.IsContentSubscribed, not obsolete IsContentActive.");
            }

            if (!(bool)isDlcActive.Invoke(null, new object[] { string.Empty })) {
                throw new InvalidOperationException("Vanilla elements with an empty DLC ID must remain active.");
            }
            Console.WriteLine(
                "DLC boolean invocation is limited to the initialization-free vanilla path; subscription semantics are characterized by real IL because Unity/platform initialization is unavailable in this probe.");
            Console.WriteLine("Element catalog runtime contract passed.");
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
exit $LASTEXITCODE
