$DataDir = "./data"
$SenaFile = "$DataDir\sena-3.zip"
$ElawaFile = "$DataDir\elawa.zip"
$SenaChecksum = "BEC5131799DB07BF8D84D8FC1F3169FB2574F2A1F4C37F6898EAB563A4AE95B8"
$ElawaChecksum = "E3608F1E3188CE5FDB166FBF9D5AAD06558DB68EFA079FB453881572B50CB8E3"

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

Get-File -FileName "Sena-3" `
                     -FileUrl 'https://drive.google.com/uc?export=download&id=1I-hwc0RHoQqW774gbS5qR-GHa1E7BlsS' `
                     -ExpectedChecksum $SenaChecksum `
                     -FilePath $SenaFile

Get-File -FileName "Elawa" `
                     -FileUrl 'https://drive.usercontent.google.com/download?export=download&id=1Jk-eSDho8ATBMS-Kmfatwi-MWQth26ro&confirm=t' `
                     -ExpectedChecksum $ElawaChecksum `
                     -FilePath $ElawaFile
