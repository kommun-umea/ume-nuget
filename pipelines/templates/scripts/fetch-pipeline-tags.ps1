function ThrowError ($message) {
    # Write to DevOps log
    Write-Host "##vso[task.logissue type=error;]$message"

    # Capture call stack information
    $callSite = (Get-PSCallStack)[1]  # index 1 = caller of this function
    $callerInfo = "Thrown at $($callSite.ScriptName):$($callSite.ScriptLineNumber)"

    throw "$message`n$callerInfo"
}
$ErrorActionPreference = 'Stop'

$accessToken = $env:SYSTEM_ACCESSTOKEN
$pipelineBuildId = $env:PUBLISH_PIPELINE_BUILD_ID
$devopsBaseUrl = $env:SYSTEM_COLLECTIONURI
$projectName = $env:SYSTEM_TEAMPROJECT

if ([string]::IsNullOrWhiteSpace($accessToken)) {
    ThrowError("Personal Access Token is not provided.")
}
if ([string]::IsNullOrWhiteSpace($pipelineBuildId)) {
    ThrowError("Pipeline Build ID is not provided.")
}
if ([string]::IsNullOrWhiteSpace($devopsBaseUrl)) {
    ThrowError("DevOps Base URL is not provided.")
}
if ([string]::IsNullOrWhiteSpace($projectName)) {
    ThrowError("Project Name is not provided.")
}

$authenticationHeader = @{
    Authorization = "Bearer $accessToken"
}
$baseUrl = "$devopsBaseUrl$projectName/_apis"
$apiVersion = "api-version=7.1"

Write-Host "Fetching build tags..."
$buildTagsUrl = "$baseUrl/build/builds/$($pipelineBuildId)/tags?$apiVersion"
$tags = Invoke-RestMethod -Method Get -Uri $buildTagsUrl -Headers $authenticationHeader -ContentType 'application/json'

foreach ($tag in $tags.value) {
    # Tag format: "<package>/<version>"
    $packageName = ($tag -split '/')[0]
    Write-Host "##vso[task.setvariable variable=TAG_$packageName;isOutput=true]$tag"
    Write-Host "Set TAG_$packageName = $tag"
}
