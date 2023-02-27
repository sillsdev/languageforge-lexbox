echo off
rmdir /s /q hg-web\repos\sena-3
powershell -Command "Invoke-WebRequest 'https://drive.google.com/uc?export=download&id=1I-hwc0RHoQqW774gbS5qR-GHa1E7BlsS' -OutFile sena-3.zip"
powershell -Command "Expand-Archive sena-3.zip -DestinationPath hg-web\repos"
del sena-3.zip