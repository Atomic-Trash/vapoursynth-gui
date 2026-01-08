Build the VapourSynth Portable GUI application.

Instructions:
1. Run `dotnet restore src/gui/VapourSynthPortable.sln`
2. Run `dotnet build src/gui/VapourSynthPortable.sln --configuration Release`
3. Report build results including any warnings or errors

If user specifies `--debug`, use Debug configuration instead.
If user specifies `--clean`, run `dotnet clean` first.
