param (
    [Parameter(Mandatory=$true)]
    [string]$FileName,

    [Parameter(Mandatory=$true)]
    [string]$Url,

    [Parameter(Mandatory=$true)]
    [string]$Checksum
)

$DataDir = "./data"
$FilePath = "$DataDir\$FileName.zip"

function Get-Checksum {
    param (
        [string]$FilePath
    )
    $hash = Get-FileHash -Path $FilePath -Algorithm SHA256
    return $hash.Hash
}

function Get-File {
  param (
      [string]$FileName,
      [string]$FileUrl,
      [string]$ExpectedChecksum,
      [string]$FilePath
  )

  Write-Host("Trying to download $FileName data...")

  if (Test-Path -Path $FilePath) {
      $checksum = Get-Checksum -FilePath $FilePath
      if ($checksum -eq $ExpectedChecksum) {
          # Checksum matches. No need to re-download.
          Write-Output "$FileName already exists."
      } else {
          Write-Output "Checksum does not match. Re-downloading $FileName..."
          Invoke-WebRequest -Uri $FileUrl -OutFile $FilePath
      }
  } else {
      Write-Output "$FileName does not exist. Downloading..."
      Invoke-WebRequest -Uri $FileUrl -OutFile $FilePath
  }
}

Get-File -FileName $FileName `
                     -FileUrl $Url `
                     -ExpectedChecksum $Checksum `
                     -FilePath $FilePath
