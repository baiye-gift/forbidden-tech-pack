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

$sourcePath = Join-Path $ProjectRoot 'src\Game\Buildings\Analyzer\MatterAnalyzer.cs'
$source = Get-Content -Raw -LiteralPath $sourcePath
Require-Condition ($source -match 'Set\(ComplexFabricator fabricator, ComplexRecipe\[\] recipes\)') `
    'MatterAnalyzer must assign a ComplexRecipe[] compatible with ComplexFabricator.recipe_list.'
Require-Condition ($source -match '\.Select\(id => RecipeRegistry\.AnalyzerRecipes\[id\]\)\.ToArray\(\)') `
    'MatterAnalyzer must materialize its filtered recipes as an array.'
Require-Condition ($source -notmatch 'Set\(ComplexFabricator fabricator, List<ComplexRecipe> recipes\)') `
    'MatterAnalyzer must not reflectively assign List<ComplexRecipe> to recipe_list.'

$gameAssemblyPath = Join-Path $GamePath 'OxygenNotIncluded_Data\Managed\Assembly-CSharp.dll'
Require-Condition (Test-Path -LiteralPath $gameAssemblyPath -PathType Leaf) `
    "Game assembly was not found for analyzer adapter contract validation: $gameAssemblyPath"

$stream = [System.IO.File]::OpenRead($gameAssemblyPath)
try {
    $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        $provider = [System.Reflection.Metadata.MetadataReaderProvider]::FromMetadataImage(
            $peReader.GetMetadata().GetContent())
        try {
            $metadata = $provider.GetMetadataReader()
            $fabricator = $null
            foreach ($typeHandle in $metadata.TypeDefinitions) {
                $type = $metadata.GetTypeDefinition($typeHandle)
                if ($metadata.GetString($type.Name) -eq 'ComplexFabricator') {
                    $fabricator = $type
                    break
                }
            }
            Require-Condition ($null -ne $fabricator) 'ComplexFabricator type was not found in Assembly-CSharp.'

            $recipeListField = $null
            foreach ($fieldHandle in $fabricator.GetFields()) {
                $field = $metadata.GetFieldDefinition($fieldHandle)
                if ($metadata.GetString($field.Name) -eq 'recipe_list') {
                    $recipeListField = $field
                    break
                }
            }
            Require-Condition ($null -ne $recipeListField) 'ComplexFabricator.recipe_list field was not found.'

            $reader = $metadata.GetBlobReader($recipeListField.Signature)
            Require-Condition ($reader.ReadByte() -eq 0x06) 'recipe_list must have a field signature.'
            Require-Condition ($reader.ReadByte() -eq 0x1d) 'ComplexFabricator.recipe_list must be an SZARRAY.'
            Require-Condition ($reader.ReadByte() -eq 0x12) 'recipe_list array element must be a class type.'
            $codedIndex = $reader.ReadCompressedInteger()
            $tag = $codedIndex -band 3
            $row = $codedIndex -shr 2
            Require-Condition ($tag -eq 0) 'recipe_list element must resolve to a type definition.'
            $elementHandle = [System.Reflection.Metadata.Ecma335.MetadataTokens]::TypeDefinitionHandle($row)
            $elementName = $metadata.GetString($metadata.GetTypeDefinition($elementHandle).Name)
            Require-Condition ($elementName -eq 'ComplexRecipe') 'recipe_list must be a ComplexRecipe array.'
        } finally {
            $provider.Dispose()
        }
    } finally {
        $peReader.Dispose()
    }
} finally {
    $stream.Dispose()
}

Write-Host 'Analyzer adapter contract validation passed.'
