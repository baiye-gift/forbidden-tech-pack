[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRoot
)

$ErrorActionPreference = 'Stop'
$gamePath = 'D:\steam\steamapps\common\OxygenNotIncluded'
$managedDirectory = Join-Path $gamePath 'OxygenNotIncluded_Data\Managed'
$rawAssembly = Join-Path $ProjectRoot 'obj\ForbiddenTechnologyPack.raw.dll'
$probeDirectory = Join-Path $ProjectRoot 'test-artifacts\research-registration-runtime'
$probeSource = Join-Path $probeDirectory 'ResearchRegistrationRuntimeProbe.cs'
$probeAssembly = Join-Path $probeDirectory 'ResearchRegistrationRuntimeProbe.exe'
$runtimeConfig = Join-Path $probeDirectory 'ResearchRegistrationRuntimeProbe.runtimeconfig.json'

& (Join-Path $ProjectRoot 'build.ps1') -GamePath $gamePath
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

New-Item -ItemType Directory -Force -Path $probeDirectory | Out-Null
$source = @'
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

internal static class ResearchRegistrationRuntimeProbe {
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

    private static void SetField(Type type, object instance, string name, object value) {
        for (var current = type; current != null; current = current.BaseType) {
            var field = current.GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) {
                field.SetValue(instance, value);
                return;
            }
        }
        throw new MissingFieldException(type.FullName, name);
    }

    private static object CreateNode(Type nodeType, string id, float x, float y, float width, float height) {
        var node = Activator.CreateInstance(nodeType);
        SetField(nodeType, node, "Id", id);
        SetField(nodeType, node, "Name", id);
        SetField(nodeType, node, "nodeX", x);
        SetField(nodeType, node, "nodeY", y);
        SetField(nodeType, node, "width", width);
        SetField(nodeType, node, "height", height);
        return node;
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
            Assembly.LoadFrom(Path.Combine(gameManaged, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(args[1]);
            var registration = mod.GetType(
                "ForbiddenTechnologyPack.Game.Registration.ForbiddenResearchRegistration", true);

            var patch = registration.GetCustomAttributes(false).Single(attribute =>
                string.Equals(attribute.GetType().FullName, "HarmonyLib.HarmonyPatch", StringComparison.Ordinal));
            var patchInfo = patch.GetType().GetField("info").GetValue(patch);
            var methodName = (string)patchInfo.GetType().GetField("methodName").GetValue(patchInfo);
            if (!string.Equals(methodName, "Load", StringComparison.Ordinal)) {
                throw new InvalidOperationException(
                    "Forbidden research registration must target Database.Techs.Load before node-less techs are removed.");
            }

            var createNode = registration.GetMethod("CreateNode", BindingFlags.Static | BindingFlags.NonPublic);
            if (createNode == null) {
                throw new InvalidOperationException("Forbidden research registration must provide a node-construction helper.");
            }

            var nodeType = firstPass.GetType("ResourceTreeNode", true);
            var loaderOpenType = firstPass.GetType("ResourceTreeLoader`1", true);
            var loaderType = loaderOpenType.MakeGenericType(nodeType);
            var loader = FormatterServices.GetUninitializedObject(loaderType);
            var nodes = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(nodeType));
            nodes.Add(CreateNode(nodeType, "MatterDeconstruction", 100f, -60f, 40f, 20f));
            nodes.Add(CreateNode(nodeType, "HighTempForging", 180f, -100f, 50f, 30f));
            SetField(loaderType, loader, "resources", nodes);

            var node = createNode.Invoke(null, new[] { loader, (object)"MatterDeconstruction" });
            if (node == null) {
                throw new InvalidOperationException("CreateNode must return a research node for a present prerequisite.");
            }

            var id = (string)nodeType.BaseType.GetField("Id").GetValue(node);
            var x = (float)nodeType.GetField("nodeX").GetValue(node);
            var y = (float)nodeType.GetField("nodeY").GetValue(node);
            var width = (float)nodeType.GetField("width").GetValue(node);
            var height = (float)nodeType.GetField("height").GetValue(node);
            if (!string.Equals(id, "BaiyeForbiddenMatterEngineering", StringComparison.Ordinal)) {
                throw new InvalidOperationException("The custom research node must use BaiyeForbiddenMatterEngineering.");
            }
            if (width <= 0f || height <= 0f) {
                throw new InvalidOperationException("The custom research node must have positive dimensions.");
            }
            if (x <= 230f) {
                throw new InvalidOperationException("The custom research node must be outside the existing right bound.");
            }
            if (y != -60f || width != 40f || height != 20f) {
                throw new InvalidOperationException(
                    "The custom research node must preserve the prerequisite row and dimensions.");
            }

            Console.WriteLine("Research registration runtime contract passed.");
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
