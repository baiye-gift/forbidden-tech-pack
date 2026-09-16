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

internal static class AnalyzerRecipeRuntimeProbe {
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
exit $LASTEXITCODE
