#!/bin/sh

rm -rf hg-web/repos/sena-3
wget -O sena-3.zip 'https://drive.google.com/uc?export=download&id=1I-hwc0RHoQqW774gbS5qR-GHa1E7BlsS'
unzip -q sena-3.zip -d hg-web/repos/
rm sena-3.zip