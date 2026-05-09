# UnConfuserEx
A tool for deobfuscating C# binaries protected with ConfuserEx2.

This was originally a private project that I decided to make public after it had no real use to me anymore. The removal of protections was very focused on one specific binary I was reversing so it's fairly likely things will break. Removers are all added to a "pipeline" so if there's some custom protection you need removing, it should be fairly straightforward to add it.

 If something does break, either fix it yourself, or raise an issue. Since this code was never meant to see the light of day, it is pretty (read: extremely) poor. I don't like looking at it, so I simply do not look at it any more (Although, I do occasionally feel like fixing some nasty bugs when I'm bored).

## Usage

UnConfuserEx.exe <obfuscated_target> <optional: deobfuscated_output>

All that needs to be specified is the target binary. If no output is specified, the output will be named "obfuscated_target-deobfuscated"

## Protections
Most common protections are removed by UnconfuserEx, but I haven't exhaustively tested permutations of the protections removed by it. YMMV.

- [x] Anti-Debug
    - [x] Safe
    - [x] Win32
    - [x] Antinet
- [x] Anti-Dump
- [ ] Anti-Tamper
    - [x] Normal
    - [ ] Anti (Same as normal just with debugger checks - could be done easily)
    - [ ] JIT (Never looked at this one)
- [ ] Compressor (Haven't seen this one, no attempt made to remove it)
    - [ ] Normal
    - [ ] Compact
- [x] Constants (control flow guard removed for all types as well)
    - [x] Normal
    - [x] Dynamic (expression)
    - [x] x86
- [ ] Control Flow
    - [x] Switch
    - [ ] Jump (I've never seen a Jump-obfuscated binary so :shrug:)
- [ ] Reference Proxy
    - [x] Normal
    - [ ] Dynamic (expression) (This might be removed as well? Never tested)
    - [x] x86
- [x] Renamer (All non-ASCII names are replaced with a generic (Type)No to improve readability)
- [ ] Resources
    - [x] Normal
    - [ ] Dynamic

There may be more that I've missed.

## Issues
If you run into a consistent error trying to deobfuscate a binary and feel like raising an issue, please follow these steps:

1. Remove file extensions from files (I will never execute anything sent to me anyway, it's just a bit of courtesy y'know :) )
2. Zip/rar/7zip/whatever everything up
3. Create an issue with the console output from the failed deobfuscation, and a link to download the above archive. If you've done any investigation into the issue, please do share your findings as well, even if you don't think they'd be useful; you'd be surprised.
4. Pray that I eventually look at it and fix it.

## Contributing
All contributions are welcome, make a pull request and I'll eventually get around to looking at it (if you think it's especially important, find a way to spam me on something because I rarely look at this repo).

No specific requirements for contributions either, just don't be a fool.
