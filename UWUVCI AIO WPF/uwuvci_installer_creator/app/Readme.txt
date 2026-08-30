Thanks for downloading UWUVCI-3!
V3.N2 updates:
- Refreshed UWUVCI information and general Wii U homebrew pages.
- Required quiz with randomized question selection, question order, and answer order.
- Restored N64 widescreen and dark-filter removal.
- Removed the NDS rendering-size option; the base's default RenderScale is left unchanged.
- Removed the startup and Settings promotion for the separate fork.

If you didn't download it from the official source (https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF/releases), then you might be using a custom version that someone else made.

If you're looking for the FAQ, keep scrolling.

If you're interested in the Discord:
https://discord.gg/mPZpqJJVmZ

If you're curious about the latest changes:
https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF/releases/latest

Written UWUVCI guides:
https://uwuvci-prime.github.io/UWUVCI-Resources/

V3.N2 is my final update to UWUVCI-3 and my retirement release.
I, ZestyTS, am stepping away from maintaining UWUVCI. Any future updates would need to come from other contributors.

For history as to what I've done on the project, I fixed a lot of bugs and helped get UWUVCI-3 out of beta while introducing new features like:
	Widescreen for N64
	DarkFilter removal for N64 and GBA
	C2W for Wii
	Added support for Win7/8
	GCT & Deflicker for Wii
	Support for Unix (helper app), etc.

If you'd like to donate to me:
https://ko-fi.com/zestyts

If you'd like to donate to the creator, NicoAICP:
https://ko-fi.com/uwuvci

For help, start with the FAQ and written guides. For community discussion, use the Discord link above.


##############################################################################################################################################################################
									FAQ
##############################################################################################################################################################################
This FAQ is included with V3.N2, my final UWUVCI update and retirement release.
Look for topics by their titles; entry numbers may change as the FAQ is reorganized.


1) I don't know how to use UWUVCI, can you show me?
	https://uwuvci-prime.github.io/UWUVCI-Resources/
	Select console and follow the guide
 
2) What games are compatible?
	https://uwuvci.net/ → Click "Compatibility" (top right) → Select a console.
	This guide is community-driven, so results may vary.
	If it's not listed, that just means it's Untested
	For GCN, Rhythm Heaven Fever works as a base for all games.

3) I don't understand what it means by "Base" in the drop down menu?
	The base game is what UWUVCI uses to inject your selected game.

4) What does it mean by "Base not downloaded"
	It means the base game cannot be found.
	Click "Enter TKey" and enter the Title Key to fix this.

5) How do I get the Title Key?
	Buy the base from the eshop
	Use Tik2SD to dump the title key
	Note: Title Key sharing is considered piracy

6) What does it mean by Common Key?
	This is the Wii U system key needed for decryption.
		If you have a NAND backup, use otp.bin.
		If not, follow this guide: https://wiiu.hacks.guide/aroma/nand-backup.html

7) The base is taking a while to download
	If not injecting GCN/Wii, it should finish in <5 min.
	Otherwise, Nintendo’s servers may be slow—try again later.
	Some games (like Xenoblade Chronicles) are large (~8.2GB) and take longer.

8) My anti virus said [insert anything]
	There are no malicious files in this program.

9) I can't find my game when I click the "Rom Path" button
	Your game is in a format UWUVCI can't read.
	Revert it to its original state.

10) Wup install failing/Error 199-9999
	Download SigpatchesModuleWiiU:
		https://github.com/V10lator/SigpatchesModuleWiiU/releases/download/v1.0/01_sigpatches.rpx
	Place it in:
		sd:/wiiu/environments/aroma/modules/setup

11) GCN/Wii Injects not working
	There are reports that SDUSB or ISFShax might be the reason why

12) Inject is created, but the game is having issues
	Rhythm Heaven US as a base works on nearly everything
	Stick to using the correct base/game for your region
	Make sure the base game is bigger or equal in size to the game you're trying to inject
	N64 games are notorious for giving issues, they may require different bases
	See "What games are compatible?" for the compatibility guide.

13) Missing pre.iso
	More than likely you have a bad dump of a game or UWUVCI doesn't like the trimmed dump
	Example: 
		if the dump is an "nkit" or a "wbfs" please try using the iso version instead

14) If you're having issues and the fix isn't listed here, see about updating UWUVCI.
	https://github.com/stuff-by-3-random-dudes/UWUVCI-AIO-WPF/releases/latest
 
15) Help with Rom Mods and Hacks
	Don't expect that much help because mods or hacks add an extra level of complexity.
	If the mods work with real hardware then there's a chance it will work on the Wii U
	Ask the mod/hack's community, they'll be more helpful
	It's possible that someone already tried it. See "What games are compatible?" for the compatibility guide.
 
16) Having problems with GCN injects?
	Usually has to do with Nintendont, you can check out their compatibility list here: https://wiki.gbatemp.net/wiki/Nintendont_Compatibility_List
	Here's their main GBATemp thread: 
		https://gbatemp.net/threads/nintendont.349258/
 
17) If you've tried everything in this list then there are a few more things
	See if your antivirus is getting in the way.
	Check to see if you installed to a OneDrive directory.

18) GCN injects boot to the nintendont menu
	You probably used TeconMoon injector before. 
	Delete nincfg.bin from the root of your sd card and the apps/nintendont folder, then do the sd setup again in UWUVCI.

19) Can't find injects in SaveMii
	You'll want to use a modified version called "SaveMii Inject MOD"

20) Missing temp/temp folder
	A temporary folder needed by a later injection step was not found. Check the log for an earlier failure.
	If disc conversion failed, see "Missing pre.iso".
	If the earlier error involves image processing, see "Image format or bit-depth errors".

21) tmd.bin can't be found or parameter invalid or handle invalid
	See "Missing temp/temp folder" and check the log for errors before the missing file was reported.

22) Will there be more UWUVCI updates from ZestyTS?
	No. V3.N2 is my final UWUVCI update and my retirement release.
	Any later maintenance or releases would need to be community-driven.
	The written guides and this FAQ remain available. For community discussion, use the Discord link above.

23) UWUVCI is broken or acting funny
	Check to see if you installed to OneDrive or if your Antivirus is blocking something

24) GB/C games aren't saving when using VC's reset button
	This is because it's using Goombacolor, please use the button reset combination instead

25) boot.dol not found
	UWUVCI mentioned after the GCN inject was done that your SD card must be setup with Nintendont

26) Stuck on "Copying to SD"
	Manually copy the folder yourself.
	Go to where UWUVCI is installed, and then look for the folder "InjectedGames"
	You'll find the folder you're looking for there, just copy it to the install folder on your sd card.

27) UWUVCI says that my drive is full (12G)
	The drive you installed UWUVCI does not have enough space, please install UWUVCI to a different drive that has enough space and try again.

28) Nkit error?
	Sounds like you pirated your game, don't do that.

29) UWUVCI doesn't open
	You're missing .Net Framework 4.8.1 Runtime, download/install it from here:
		https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481

30) UWUVCI still won't open
	If checking the log doesn't have anything in there, then you'll have to look in your Event Viewer to see what the problem is.

31) UWUVCI's progress bar gets stuck (outside of Downloading base)
	Try updating your tools (gear icon to the top right -> "Update Tools")

32) Mac/Linux version?
	This program was built using WPF (Windows Platform Foundation) and that does not natively work on non-Windows platforms.
	Using the exe, you can run it on Mac/Linux using Wine or some other tool like that. 
	The program will know if you're running it not on a Windows platform, and will do it's best to help you.
	UWUVCI V4 will have Mac & Linux support.

33) Where can I find the Log file or the settings file?
	It'll be in your local appdata folder called "UWUVCI-V3", you can find that by going to %localappdata%
	As for my Unix friends, it depends on your tool like if you're using wine, you could do something like this "wine cmd /c echo %localappdata%"
	That'll tell you if your folder is somewhere like ~/.wine/drive_c/users/$USER/Local Settings/Application Data/ or ~/.wine/drive_c/users/$USER/AppData/Local/

34) An error message popped up.
	Read it, it'll explain the problem.

35) On Windows, but the program thinks you're running on Linux/Mac (Unix)?
	You probably have some weird software that's flagging one of the checks.
	Open regedit and go to here
	    HKEY_CURRENT_USER\Software\Wine
	Is there something there?

	If the answer is yes, then that's the problem, so right click the folder and click delete.
	If the answer is no, then I'm sorry but you're going to have to check every single one of these because SOMETHING on your machine is the problem.
		Proton
		Crossover
		BoxedWine
		Lutris
		PlayOnLinux
		DXVK
		ReactOS
		Winetricks
		Cedega (WineX)
		VMWare	
		Parallels
		QEMU
		Xen
		Bhyve
		Bochs
		Openstack
		ProxMox
		Virtuozzo

36) Error when making N64 injects?
	V3.N2 restores the N64 widescreen and dark-filter patches that were broken in earlier versions.
	If an error remains, check that you are using a clean, supported base and include the exact error and log in your report.

37) Where is the NDS rendering-size option?
	It was removed in V3.N2. UWUVCI no longer changes RenderScale in configuration_cafe.json.
	The normal base value is 1. Brightness and PixelArtUpscaler remain separate options.

38) Why do I need to pass a quiz?
	V3.N2 includes information about UWUVCI and Wii U homebrew prerequisites, followed by one question per topic.
	All questions must be answered correctly. A failed attempt returns to the first page.
	Each new attempt selects questions again and shuffles their order and answer choices.
	Passing is remembered, including after restarting the app. Previous tutorial completion does not skip the new quiz.
	You can review the pages and retry the quiz from Settings > Information and Quiz.

39) Image format or bit-depth errors
	Check the image named in the error. The generated TGA files must have the required dimensions and bit depth:
		iconTex: 128x128, 32-bit.
		bootDrcTex: 854x480, 24-bit.
		bootTvTex: 1280x720, 24-bit.
		bootLogoTex: 170x42, 32-bit.
	If automatically downloaded artwork fails, select a different image or provide your own.
	For image preparation, follow the written UWUVCI guide for your console:
		https://uwuvci-prime.github.io/UWUVCI-Resources/
