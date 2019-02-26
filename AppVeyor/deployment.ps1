# Publishing to NuGet feed

Write-Host $env:APPVEYOR_REPO_BRANCH
Write-Host $env:APPVEYOR_REPO_TAG

if($env:APPVEYOR_REPO_BRANCH -ne "master" -and $env:APPVEYOR_REPO_TAG -eq "true"){
  Write-Host "Tag detected"
}