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

    private static object GetField(Type type, object instance, string name) {
        for (var current = type; current != null; current = current.BaseType) {
            var field = current.GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null) {
                return field.GetValue(instance);
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

    private static object CreateLoader(Type loaderType, Type nodeType) {
        var loader = FormatterServices.GetUninitializedObject(loaderType);
        var nodes = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(nodeType));
        nodes.Add(CreateNode(nodeType, "MatterDeconstruction", 100f, -60f, 40f, 20f));
        nodes.Add(CreateNode(nodeType, "HighTempForging", 180f, -100f, 50f, 30f));
        SetField(loaderType, loader, "resources", nodes);
        return loader;
    }

    private static object CreateTech(Type techType, object techs, string id, string[] unlockedIds) {
        var ids = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<string>));
        foreach (var unlockedId in unlockedIds) {
            ids.Add(unlockedId);
        }
        var costs = (IDictionary)Activator.CreateInstance(
            typeof(System.Collections.Generic.Dictionary<string, float>));
        costs["basic"] = 1f;
        return techType.GetConstructors().Single().Invoke(new[] { (object)id, ids, techs, costs });
    }

    private static object CreateTechs(Type techsType, Type techType) {
        var techs = FormatterServices.GetUninitializedObject(techsType);
        var resourceType = techsType.BaseType.BaseType.BaseType;
        var guidType = resourceType.GetProperty("Guid").PropertyType;
        var guid = Activator.CreateInstance(guidType, new object[] { "Techs", null });
        SetField(techsType, techs, "<Guid>k__BackingField", guid);
        SetField(techsType, techs, "Id", "Techs");
        SetField(techsType, techs, "Name", "Techs");
        var resources = Activator.CreateInstance(
            typeof(System.Collections.Generic.List<>).MakeGenericType(techType));
        SetField(techsType, techs, "resources", resources);
        return techs;
    }

    private static object FindTech(Type techsType, object techs, string id) {
        return techsType.GetMethod("TryGet", new[] { typeof(string) }).Invoke(techs, new object[] { id });
    }

    private static void SetTechNode(Type techType, object tech, object node, string category) {
        techType.GetMethod("SetNode").Invoke(tech, new[] { node, (object)category });
    }

    private static void RemoveNodeLessTechs(Type techsType, Type techType, object techs) {
        var count = (int)techsType.GetProperty("Count").GetValue(techs, null);
        var getResource = techsType.GetMethod("GetResource");
        var remove = techsType.GetMethod("Remove");
        for (var index = count - 1; index >= 0; index--) {
            var tech = getResource.Invoke(techs, new object[] { index });
            var foundNode = (bool)techType.GetProperty("FoundNode").GetValue(tech, null);
            if (!foundNode) {
                remove.Invoke(techs, new[] { tech });
            }
        }
    }

    private static bool Calls(MethodInfo caller, MethodInfo callee) {
        var body = caller.GetMethodBody();
        if (body == null) {
            return false;
        }
        var il = body.GetILAsByteArray();
        var token = BitConverter.GetBytes(callee.MetadataToken);
        for (var index = 0; index <= il.Length - token.Length; index++) {
            var matches = true;
            for (var offset = 0; offset < token.Length; offset++) {
                if (il[index + offset] != token[offset]) {
                    matches = false;
                    break;
                }
            }
            if (matches) {
                return true;
            }
        }
        return false;
    }

    private static void ConfigureDlcCache(Assembly firstPass) {
        var manager = firstPass.GetType("DlcManager", true);
        var cache = (IDictionary)manager.GetField("dlcSubscribedCache",
            BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        foreach (var field in manager.GetFields(BindingFlags.Static | BindingFlags.Public)) {
            if (field.FieldType == typeof(string) && field.Name.EndsWith("_ID", StringComparison.Ordinal)) {
                var id = (string)field.GetValue(null);
                if (!string.IsNullOrEmpty(id)) {
                    cache[id] = false;
                }
            }
        }
    }

    private static void AssertRegistered(Type techsType, Type techType, object techs,
            object prerequisite, string expectedCategory, MethodInfo postfix) {
        var custom = FindTech(techsType, techs, "BaiyeForbiddenMatterEngineering");
        if (custom == null) {
            throw new InvalidOperationException("The registration boundary must add the forbidden research tech.");
        }
        if (!(bool)techType.GetProperty("FoundNode").GetValue(custom, null)) {
            throw new InvalidOperationException("The forbidden research tech must have FoundNode == true before cleanup.");
        }

        var required = (IList)GetField(techType, custom, "requiredTech");
        var unlocked = (IList)GetField(techType, prerequisite, "unlockedTech");
        if (!required.Contains(prerequisite) || !unlocked.Contains(custom)) {
            throw new InvalidOperationException("The registered tech must be linked to its selected prerequisite.");
        }

        var expectedBuildings = new[] { "BaiyeMatterAnalyzer", "BaiyeMassCrusher", "BaiyeMatterCompiler" };
        var actualBuildings = ((IEnumerable)GetField(techType, custom, "unlockedItemIDs"))
            .Cast<string>().OrderBy(value => value).ToArray();
        if (!actualBuildings.SequenceEqual(expectedBuildings.OrderBy(value => value))) {
            throw new InvalidOperationException("The registered tech must retain all enabled building IDs.");
        }

        var costs = (IDictionary)GetField(techType, custom, "costsByResearchTypeID");
        if (!costs.Contains("basic") || !costs.Contains("advanced") ||
                (float)costs["basic"] != 120f || (float)costs["advanced"] != 80f) {
            throw new InvalidOperationException("The registered tech must retain its explicit research costs.");
        }

        RemoveNodeLessTechs(techsType, techType, techs);
        if (!ReferenceEquals(FindTech(techsType, techs, "BaiyeForbiddenMatterEngineering"), custom)) {
            throw new InvalidOperationException("The forbidden research tech must survive node-less cleanup.");
        }

        postfix.Invoke(null, new[] { techs });
        if (!string.Equals((string)GetField(techType, custom, "category"), expectedCategory,
                StringComparison.Ordinal)) {
            throw new InvalidOperationException("The registered tech must copy its prerequisite category.");
        }

        var prerequisiteNode = GetField(techType, prerequisite, "node");
        var customNode = GetField(techType, custom, "node");
        var edges = (IEnumerable)GetField(prerequisiteNode.GetType(), prerequisiteNode, "edges");
        var hasVisibleEdge = edges.Cast<object>().Any(edge =>
            ReferenceEquals(edge.GetType().GetProperty("source").GetValue(edge, null), prerequisiteNode) &&
            ReferenceEquals(edge.GetType().GetProperty("target").GetValue(edge, null), customNode));
        if (!hasVisibleEdge) {
            throw new InvalidOperationException("The actual prerequisite node must own a visible edge to the custom node.");
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
            ConfigureDlcCache(firstPass);
            var game = Assembly.LoadFrom(Path.Combine(gameManaged, "Assembly-CSharp.dll"));
            var mod = Assembly.LoadFrom(args[1]);
            var registration = mod.GetType(
                "ForbiddenTechnologyPack.Game.Registration.ForbiddenResearchRegistration", true);
            var techsType = game.GetType("Database.Techs", true);
            var techType = game.GetType("Tech", true);

            var patch = registration.GetCustomAttributes(false).Single(attribute =>
                string.Equals(attribute.GetType().FullName, "HarmonyLib.HarmonyPatch", StringComparison.Ordinal));
            var patchInfo = patch.GetType().GetField("info").GetValue(patch);
            var methodName = (string)patchInfo.GetType().GetField("methodName").GetValue(patchInfo);
            var declaringType = (Type)patchInfo.GetType().GetField("declaringType").GetValue(patchInfo);
            if (!string.Equals(methodName, "Load", StringComparison.Ordinal) || declaringType != techsType) {
                throw new InvalidOperationException(
                    "Forbidden research registration must target Database.Techs.Load before node-less techs are removed.");
            }

            var prefix = registration.GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
            var register = registration.GetMethod("Register", BindingFlags.Static | BindingFlags.NonPublic);
            if (prefix == null || register == null || !Calls(prefix, register)) {
                throw new InvalidOperationException(
                    "The Database.Techs.Load prefix must call the testable registration boundary.");
            }
            var postfix = registration.GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
            if (postfix == null) {
                throw new InvalidOperationException("Database.Techs.Load must retain its registration postfix.");
            }

            var createNode = registration.GetMethod("CreateNode", BindingFlags.Static | BindingFlags.NonPublic);
            if (createNode == null) {
                throw new InvalidOperationException("Forbidden research registration must provide a node-construction helper.");
            }

            var nodeType = firstPass.GetType("ResourceTreeNode", true);
            var loaderOpenType = firstPass.GetType("ResourceTreeLoader`1", true);
            var loaderType = loaderOpenType.MakeGenericType(nodeType);
            var loader = CreateLoader(loaderType, nodeType);

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

            var firstTechs = CreateTechs(techsType, techType);
            var matterNode = CreateNode(nodeType, "MatterDeconstruction", 100f, -60f, 40f, 20f);
            var matter = CreateTech(techType, firstTechs, "MatterDeconstruction", new string[0]);
            SetTechNode(techType, matter, matterNode, "SolidMaterial");
            register.Invoke(null, new[] { firstTechs, loader });
            AssertRegistered(techsType, techType, firstTechs, matter, "SolidMaterial", postfix);

            var secondTechs = CreateTechs(techsType, techType);
            var forgingNode = CreateNode(nodeType, "HighTempForging", 180f, -100f, 50f, 30f);
            var forging = CreateTech(techType, secondTechs, "HighTempForging", new string[0]);
            SetTechNode(techType, forging, forgingNode, "Advanced");
            register.Invoke(null, new[] { secondTechs, loader });
            AssertRegistered(techsType, techType, secondTechs, forging, "Advanced", postfix);

            Console.WriteLine("Research registration runtime contract passed.");
            return 0;
        } catch (Exception exception) {
            var root = exception;
            while (root.InnerException != null) {
                root = root.InnerException;
            }
            Console.Error.WriteLine(root.ToString());
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
