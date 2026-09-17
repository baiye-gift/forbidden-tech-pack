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
$probeDirectory = Join-Path $ProjectRoot 'test-artifacts\options-localization-runtime'
$probeSource = Join-Path $probeDirectory 'OptionsLocalizationRuntimeProbe.cs'
$probeAssembly = Join-Path $probeDirectory 'OptionsLocalizationRuntimeProbe.exe'
$runtimeConfig = Join-Path $probeDirectory 'OptionsLocalizationRuntimeProbe.runtimeconfig.json'

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

internal static class OptionsLocalizationRuntimeProbe {
    private static string gameManaged;
    private static string libraryDirectory;

    private static Assembly ResolveAssembly(object sender, ResolveEventArgs args) {
        var fileName = new AssemblyName(args.Name).Name + ".dll";
        foreach (var directory in new[] { gameManaged, libraryDirectory }) {
            var candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate)) {
                return Assembly.LoadFrom(candidate);
            }
        }
        return null;
    }

    private static bool ContainsChinese(string value) {
        return !string.IsNullOrWhiteSpace(value) && value.Any(character => character >= '\u4e00' && character <= '\u9fff');
    }

    private static object GetAttribute(MemberInfo member, string fullName) {
        return member.GetCustomAttributes(false).SingleOrDefault(attribute =>
            string.Equals(attribute.GetType().FullName, fullName, StringComparison.Ordinal));
    }

    private static string ReadString(object attribute, string property) {
        return (string)attribute.GetType().GetProperty(property).GetValue(attribute, null);
    }

    private static int Main(string[] args) {
        if (args.Length != 3) {
            Console.Error.WriteLine("Expected game-managed, mod-assembly, and library-directory arguments.");
            return 2;
        }

        gameManaged = args[0];
        libraryDirectory = args[2];
        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        try {
            var mod = Assembly.LoadFrom(args[1]);
            var options = mod.GetType("ForbiddenTechnologyPack.Game.Options.ForbiddenTechOptions", true);
            if (GetAttribute(options, "PeterHan.PLib.Options.RestartRequiredAttribute") == null) {
                throw new InvalidOperationException("The options page must clearly require a restart.");
            }

            var optionProperties = options.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => new {
                    Property = property,
                    Attribute = GetAttribute(property, "PeterHan.PLib.Options.OptionAttribute")
                })
                .Where(item => item.Attribute != null)
                .ToArray();
            if (optionProperties.Length != 14) {
                throw new InvalidOperationException("Expected all 14 user-facing options to have Option attributes.");
            }

            foreach (var item in optionProperties) {
                var title = ReadString(item.Attribute, "Title");
                var tooltip = ReadString(item.Attribute, "Tooltip");
                var category = ReadString(item.Attribute, "Category");
                if (!ContainsChinese(title) || !ContainsChinese(tooltip) || !ContainsChinese(category)) {
                    throw new InvalidOperationException("Option '" + item.Property.Name +
                        "' must have a Chinese title, tooltip, and category.");
                }
            }

            var presetProperty = options.GetProperty("Preset");
            var presetType = presetProperty.PropertyType;
            var expectedTitles = new Dictionary<string, string>(StringComparer.Ordinal) {
                { "Balanced", "相对平衡" },
                { "Strong", "强力（默认）" },
                { "Extreme", "极度超模" },
                { "Custom", "自定义" }
            };
            foreach (var pair in expectedTitles) {
                var field = presetType.GetField(pair.Key, BindingFlags.Public | BindingFlags.Static);
                if (field == null) {
                    throw new InvalidOperationException("Missing preset choice '" + pair.Key + "'.");
                }
                var attribute = GetAttribute(field, "PeterHan.PLib.Options.OptionAttribute");
                if (attribute == null || !string.Equals(ReadString(attribute, "Title"), pair.Value,
                        StringComparison.Ordinal)) {
                    throw new InvalidOperationException("Preset '" + pair.Key + "' is not displayed in Chinese.");
                }
            }

            Console.WriteLine("Chinese options runtime contract passed.");
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
