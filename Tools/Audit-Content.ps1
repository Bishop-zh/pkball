param([string]$ProjectRoot = (Split-Path $PSScriptRoot -Parent))
$ErrorActionPreference = 'Stop'
$data = Get-Content (Join-Path $ProjectRoot 'Assets/PKBall/Resources/Data/items.json') -Raw | ConvertFrom-Json
$issues = [Collections.Generic.List[string]]::new()
$ids = @{}
$presets = @{}
foreach ($entry in $data.items) {
    $item = $entry.item
    if ([string]::IsNullOrWhiteSpace($item.itemId)) { $issues.Add('Missing item ID'); continue }
    if ($ids.ContainsKey($item.itemId)) { $issues.Add('Duplicate item ' + $item.itemId) }
    $ids[$item.itemId] = $true
    $preset = [string]$item.animation.preset
    if (!$presets.ContainsKey($preset)) { $presets[$preset] = 0 }
    $presets[$preset]++
    if (!$item.animation -or $item.animationSchemaVersion -ne 1 -or $item.animation.preset -lt 0 -or $item.animation.preset -gt 16) {
        $issues.Add('Invalid animation schema or preset ' + $item.itemId)
    }
    $effectIds = @{}
    foreach ($effect in $item.effects) {
        if ([string]::IsNullOrEmpty($effect.id)) { $issues.Add('Missing effect ID ' + $item.itemId) }
        elseif ($effectIds.ContainsKey($effect.id)) { $issues.Add('Duplicate effect ID ' + $item.itemId + ':' + $effect.id) }
        else { $effectIds[$effect.id] = $true }
        if ($effect.levelAmounts -and $effect.levelAmounts.Count -ne 4) { $issues.Add('Invalid level amounts ' + $item.itemId) }
        if ($effect.levelGrowthAmounts -and $effect.levelGrowthAmounts.Count -ne 4) { $issues.Add('Invalid level growth ' + $item.itemId) }
    }
}
$directory = Join-Path $ProjectRoot 'Artifacts/ArchitectureAudit'
New-Item -ItemType Directory -Force -Path $directory | Out-Null
$report = [PSCustomObject]@{itemCount=$data.items.Count; uniqueItemIds=$ids.Count; animationPresetDistribution=$presets; issues=@($issues)}
$report | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $directory 'content-audit.json') -Encoding utf8
if ($issues.Count) { throw ($issues -join [Environment]::NewLine) }
Write-Output "Content audit passed: $($ids.Count) items, $($presets.Count) animation presets."
