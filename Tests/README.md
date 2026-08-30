# V3.N2 Regression Checks

These checks exercise the actual official application assembly without adding a test-framework dependency.

From the repository root, build with Visual Studio MSBuild and run on Windows:

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe'
& $msbuild '.\UWUVCI AIO WPF.sln' /restore /p:RestorePackagesConfig=true /p:Configuration=Release
& $msbuild .\Tests\UWUVCI.RegressionTests.csproj /p:Configuration=Release
& .\Tests\bin\Release\UWUVCI.RegressionTests.exe .\Tests\artifacts
```

A desktop session is needed for the WPF checks. Their windows remain off-screen. The runner does not start the real main view model, download tools, or load/save the user's UWUVCI settings. Generated JSON fixtures and PNG screenshots go into the supplied artifact directory.

Coverage:

- All four N64 widescreen/dark-filter combinations, including a non-picture decoy pane and big-endian scale values.
- Malformed/truncated layouts and rejection without partial patches.
- Question selection, question order, and answer positions across 128 reproducible random seeds.
- Fresh question selection and shuffled questions/answers across repeated failed attempts.
- Incomplete, failed, passed, and back-navigation quiz states.
- QuizScreen defaults to 0, stores a pass as 1, and ignores/removes old tutorial-completion flags when settings are saved.
- Bundled FAQ availability, the V3.N2 retirement note, and named topic references without video-guide links.
- Production NDS JSON edits with brightness and pixel-art options, preserving RenderScale at 1.
- Every tutorial page at normal/minimum dimensions, 200% screenshot output, nonblank pixel checks, text bounds, answer bindings, and modal close/pass behavior.

The synthetic N64 fixture contains no game content. These checks do not replace testing real injections on a Wii U.

Content references: [Wii U Hacks Guide](https://wiiu.hacks.guide/), [official UWUVCI guide](https://uwuvci-prime.github.io/UWUVCI-Resources/), and [Nintendo USB-power guidance](https://www.nintendo.com/en-gb/Support/Legacy-system/Which-kind-of-USB-HDD-is-suitable-for-use-with-Wii-U-678127.html).
