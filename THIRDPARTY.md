NitroSharp uses the following third-party libraries that may be distributed under licenses different than NitroSharp itself:

## Veldrid
https://github.com/mellinoe/veldrid

Licensed under the [MIT](https://github.com/mellinoe/veldrid/blob/master/LICENSE) license.

Copyright (c) 2017 Eric Mellino and Veldrid contributors.

## ImageSharp
https://github.com/SixLabors/ImageSharp

Licensed under the [Apache License, Version 2.0](https://github.com/SixLabors/ImageSharp/blob/master/LICENSE).

Copyright (c) Six Labors.

## SharpDX
https://github.com/sharpdx/SharpDX

Licensed under the [MIT](https://github.com/sharpdx/SharpDX/blob/master/LICENSE) license.

Copyright (c) 2010-2014 SharpDX - Alexandre Mutel.

## Vortice.Windows
https://github.com/amerkoleci/Vortice.Windows

Licensed under the [MIT](https://github.com/amerkoleci/Vortice.Windows/blob/main/LICENSE) license.

Copyright (c) 2019-2021 Amer Koleci and Vortice contributors.

## MessagePack for C#
https://github.com/neuecc/MessagePack-CSharp

Licensed under the [MIT](https://github.com/neuecc/MessagePack-CSharp/blob/master/LICENSE) license.

Copyright (c) 2017 Yoshifumi Kawai and contributors.

## sprintf\.NET
https://github.com/adamhewitt627/sprintf.NET

Licensed under the [MIT](https://github.com/adamhewitt627/sprintf.NET/blob/master/LICENSE) license.

Copyright (c) 2018 Adam Hewitt.

## FFmpeg
https://www.ffmpeg.org/

Licensed under the [LGPLv2.1](https://git.ffmpeg.org/gitweb/ffmpeg.git/blob_plain/HEAD:/COPYING.LGPLv2.1) license.

The included FFmpeg binaries are built from the [original source code](https://ffmpeg.org/releases/ffmpeg-snapshot.tar.bz2) with no optional GPL-licensed or non-free components enabled. The exact build script used for producing the included binaries can be found at [this location](https://dev.azure.com/goldenjoe/_git/nitrosharp-deps?path=%2Fbuild-ffmpeg.ps1).
NitroSharp uses dynamic linking for the FFmpeg binaries on all supported platforms.

## FreeType
https://www.freetype.org/

Licensed under the [FreeType License](https://gitlab.freedesktop.org/freetype/freetype/-/blob/master/docs/FTL.TXT).

Copyright (C) 2006-2021 by
David Turner, Robert Wilhelm, and Werner Lemberg.

The included FreeType binaries are built from the [original source code](https://git.savannah.gnu.org/git/freetype/freetype2.git). The exact build script can be found at [this location](https://dev.azure.com/goldenjoe/_git/nitrosharp-deps?path=%2Fbuild-freetype.ps1). NitroSharp uses dynamic linking for the FreeType binaries on all supported platforms.

## OpenAL Soft
https://openal-soft.org/

OpenAL cross platform audio library

Copyright (C) 2008 by authors.

Licensed under the [LGPLv2](https://github.com/kcat/openal-soft/blob/master/COPYING) license.

The included OpenAL Soft binaries are built from the [original source code](https://github.com/kcat/openal-soft.git). The exact build script can be found at [this location](https://dev.azure.com/goldenjoe/_git/nitrosharp-deps?path=%2Fbuild-openal.ps1). NitroSharp uses dynamic linking for the OpenAL Soft binaries on all supported platforms.

---

Additionally, the following third-party libraries have been vendored into the repository:

## OpenAL#
Refer to the ``third_party/OpenAL-CS`` directory for more information.

## FFmpeg.AutoGen
Refer to the ``third_party/FFmpeg.AutoGen`` directory for more information.
