This directory contains a modified version of [FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen), an open source library licensed under LGPL-3.0.
Copyright © Ruslan Balanukhin 2020 All rights reserved.
See LICENSE.txt for details.
Based on version 4.1.

Local modifications:
- Only the generated bindings are included
- PInvoke is used for interop rather than a LoadLibrary/dlopen-based solution.
- Some of the generated code may have been removed
