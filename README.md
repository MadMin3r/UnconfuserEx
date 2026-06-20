# UnConfuserEx
A tool for deobfuscating C# binaries protected with ConfuserEx2.

This was originally a private project that I decided to make public after it had no real use to me anymore. The removal of protections was focused on one specific binary I was reversing and they seemed to be using a slightly modified version of ConfuserEx, so it's possible that some things will be broken.

## Usage

UnConfuserEx.exe <obfuscated_target> <optional: deobfuscated_output>

If no output path is specified, the deobfuscated output will be in the same directory as the target with the "-deobfuscated" suffix applied.

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
    - [ ] Normal
    - [ ] Dynamic (expression) (This might be removed as well? Never tested)
    - [x] x86
- [x] Renamer (All non-ASCII names are replaced with a generic (Type)No to improve readability)
- [ ] Resources
    - [x] Normal
    - [ ] Dynamic

There may be more that I've missed.

## Issues
If you run into a consistent error trying to deobfuscate a binary, upload the binary to any file sharing site and raise an issue with a link to the file, as well as the console output.

## Contributing
All contributions are welcome, make a pull request and I'll try to respond in a reasonable amount of time.