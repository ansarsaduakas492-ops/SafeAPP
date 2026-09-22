param([string]$FragmentPath)

$ErrorActionPreference = 'Stop'
$documentsPath = Split-Path $PSScriptRoot -Parent
$jsonPath = Join-Path $documentsPath 'roadmap.json'
$jsonText = [IO.File]::ReadAllText($jsonPath)
$data = $jsonText | ConvertFrom-Json
$milestoneStates = @('planned', 'active', 'blocked', 'ready_for_review', 'accepted')
$taskStates = @('pending', 'doing', 'blocked', 'done')
$forecastStates = @('not_assessed', 'on_track', 'at_risk', 'late')
$knownIds = @{}
$taskIds = @{}
if ($data.schemaVersion -ne 1) { throw 'Unsupported roadmap schema.' }
foreach ($milestone in $data.milestones) {
    if ($knownIds.ContainsKey($milestone.id)) { throw "Duplicate milestone: $($milestone.id)" }
    $knownIds[$milestone.id] = $true
    if ($milestone.status -notin $milestoneStates) { throw "Invalid milestone status: $($milestone.id)" }
    if ($milestone.forecast -notin $forecastStates) { throw "Invalid forecast: $($milestone.id)" }
    $dueDate = [datetime]::ParseExact($milestone.due, 'yyyy-MM-dd', [Globalization.CultureInfo]::InvariantCulture)
    if ($milestone.kind -eq 'call' -and $dueDate.DayOfWeek -notin @('Tuesday', 'Thursday')) { throw "Call is not Tuesday/Thursday: $($milestone.id)" }
    foreach ($task in $milestone.tasks) {
        if ($taskIds.ContainsKey($task.id)) { throw "Duplicate task: $($task.id)" }
        $taskIds[$task.id] = $true
        if ($task.status -notin $taskStates) { throw "Invalid task status: $($task.id)" }
        if ($task.owner -notin @('agent', 'student', 'mentor')) { throw "Invalid owner: $($task.id)" }
        if ($task.status -eq 'done' -and @($task.evidence | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -eq 0) { throw "Done task needs evidence: $($task.id)" }
    }
    if ($milestone.status -in @('ready_for_review', 'accepted')) {
        if (@($milestone.tasks | Where-Object status -ne 'done').Count -gt 0) { throw "Milestone has incomplete tasks: $($milestone.id)" }
        if ([string]::IsNullOrWhiteSpace($milestone.review) -or @($milestone.acceptanceEvidence | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -eq 0) { throw "Milestone needs review and acceptance evidence: $($milestone.id)" }
    }
    if ($milestone.status -eq 'accepted' -and [string]::IsNullOrWhiteSpace($milestone.mentorAcceptance)) { throw "Mentor acceptance missing: $($milestone.id)" }
}
foreach ($milestone in $data.milestones) {
    foreach ($dependency in $milestone.dependsOn) {
        if (-not $knownIds.ContainsKey($dependency) -or $dependency -eq $milestone.id) { throw "Invalid dependency in $($milestone.id): $dependency" }
    }
}
$templatePath = Join-Path $PSScriptRoot 'roadmap-view.template.html'
$template = [IO.File]::ReadAllText($templatePath)
# Escape script delimiters while retaining valid JSON in the embedded data block.
$safeJson = $jsonText.Replace('&', '\u0026').Replace('<', '\u003c').Replace('>', '\u003e')
$fragment = $template.Replace('__ROADMAP_DATA__', $safeJson)
$wrapperStart = @'
<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>SafePhrase learning roadmap</title>
<style>
:root{color-scheme:light dark;--background:light-dark(#fafaf8,#151819);--foreground:light-dark(#202726,#e7edea);--muted-foreground:light-dark(#53635d,#b4c4bd);--border:light-dark(#ccd6d0,#415249);--primary:light-dark(#214e3b,#bce4cc);--primary-foreground:light-dark(#fff,#14271d);--accent:light-dark(#e3eee7,#253b30);--accent-foreground:var(--foreground);--font-size-base:16px;--viz-series-1:light-dark(#347558,#a8d4bc)}
body{margin:0;background:var(--background);color:var(--foreground);font:400 var(--font-size-base)/1.55 system-ui,sans-serif}main{max-width:960px;margin:auto;padding:28px 20px}h1,h2,h3{font-weight:500}h1{font-size:1.65em}h2{font-size:1.25em}h3{font-size:1.05em}button,select{font:inherit}.btn{padding:10px 14px;border:1px solid var(--border);border-radius:8px;color:var(--foreground);background:transparent;cursor:pointer}.btn[aria-pressed="true"]{background:var(--primary);color:var(--primary-foreground)}.form-select{padding:8px;max-width:100%;color:var(--foreground);background:var(--background);border:1px solid var(--border);border-radius:6px}.text-small{font-size:.875em}.text-muted{color:var(--muted-foreground)}.viz-row{display:flex;gap:12px;align-items:center;flex-wrap:wrap}.form-label{display:grid;gap:4px}a{color:inherit}hr{border:0;border-top:1px solid var(--border);margin:22px 0}button:focus-visible,select:focus-visible,summary:focus-visible{outline:3px solid var(--viz-series-1);outline-offset:3px}button:disabled{opacity:.6}code{overflow-wrap:anywhere}
</style></head><body><main>
'@
$outputPath = Join-Path $documentsPath 'roadmap.html'
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText($outputPath, $wrapperStart + "`n" + $fragment + "`n</main></body></html>", $utf8)
if ($FragmentPath) {
    [IO.File]::WriteAllText([IO.Path]::GetFullPath($FragmentPath), $fragment, $utf8)
}
Write-Output "Validated $($data.milestones.Count) milestones and $($taskIds.Count) tasks; generated $outputPath"
