# qpckEater
C# tool for unpacking and repacking .qpck files from God Eater Resurrection and God Eater 2 Rage Burst PC versions.

## Usage
Extract: `qpckEater -x <qpck_file>`

Extract and unpack (.blz4, .pres): `qpckEater -xp <qpck_file>`

Repack: `qpckEater -c <folder>`

## Dependencies
This project uses ZLIB.NET. Get the binary for building yourself [here](http://www.componentace.com/zlib_.NET.htm). 
The release builds are distributed with all dependencies.
