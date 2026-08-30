using System.Collections.Generic;

namespace UWUVCI_AIO_WPF.Models
{
    internal static class TutorialContent
    {
        public const string GuideUrl = "https://uwuvci-prime.github.io/UWUVCI-Resources/";
        public const string HomebrewGuideUrl = "https://wiiu.hacks.guide/";

        public static IReadOnlyList<TutorialPage> Pages { get; } = new[]
        {
            new TutorialPage("Using UWUVCI",
                new[]
                {
                    "UWUVCI creates Virtual Console injections for your Wii U using a game you supply and a suitable base title. Creating an injection on your PC does not install it on the Wii U or set up homebrew on the console.",
                    "Supported systems include NDS, GBA, N64, SNES, NES, TG16, MSX, Wii, and GameCube. GameCube injections use Nintendont, so complete UWUVCI's Nintendont SD setup before playing them.",
                    "A completed build means the files were created. It does not guarantee that every game will work with the selected base or emulator.",
                    "Read each topic, then answer a question about each one. All quiz answers must be correct to continue. A pass is remembered; the information and quiz remain available from Settings."
                },
                new[] { new TutorialLink("UWUVCI Guide", GuideUrl) },
                new[]
                {
                    new TutorialQuestion("What runs GameCube games launched through a UWUVCI injection?",
                        "Nintendont.", "The N64 Virtual Console emulator.", "The Wii U web browser.", "The NDS Virtual Console emulator."),
                    new TutorialQuestion("Does creating an injection on your PC install homebrew on the Wii U?",
                        "No. The console needs its own homebrew setup.", "Yes, as soon as the base finishes downloading.", "Yes, if the PC and Wii U use the same network.", "Only when widescreen is enabled."),
                    new TutorialQuestion("What does a completed injection build establish?",
                        "The output files were created; game compatibility still needs to be checked.", "Every game is guaranteed to work.", "The Wii U already has the game installed.", "Nintendont is no longer needed for GameCube games.")
                }),
            new TutorialPage("Base Games and Compatibility",
                new[]
                {
                    "A base is a Wii U eShop title whose emulator or wrapper is used for your injected game. It is different from the ROM or disc image that you want to play.",
                    "For Virtual Console systems such as N64 and NDS, the chosen base can affect compatibility. Check the documented base, region, and any required configuration before building.",
                    "Use the compatibility lists linked from UWUVCI's guides. A game missing from a community list is untested or undocumented, not automatically compatible or incompatible.",
                    "For N64, follow any base and INI recommendations in the compatibility entry. Changing artwork or enabling widescreen does not make an unsupported game compatible."
                },
                new[] { new TutorialLink("Guides and Compatibility Lists", "https://uwuvci.net/") },
                new[]
                {
                    new TutorialQuestion("What is a base game in UWUVCI?",
                        "The Wii U title that supplies the emulator or wrapper for the injection.", "The SD card's volume name.", "The artwork shown on the Wii U Menu.", "The folder containing your save backups."),
                    new TutorialQuestion("What does a missing entry in a community compatibility list mean?",
                        "The game has not been documented there yet.", "The game is guaranteed to work with every base.", "The game can never work.", "The Wii U needs to be formatted."),
                    new TutorialQuestion("What should guide your choice of base for an N64 injection?",
                        "The compatibility entry, including its base, region, and INI recommendations.", "Whichever title has the best icon.", "The alphabetical order of the base list.", "The amount of free space on the SD card alone.")
                }),
            new TutorialPage("Guides and Getting Help",
                new[]
                {
                    "The bundled ReadMe contains the FAQ and guide links. In UWUVCI, select a console and use its question-mark help button for that console's instructions.",
                    "Follow the written guide for the version and console you are using. Instructions for other versions or homebrew setups may not apply.",
                    "When reporting an error, include the exact message, your UWUVCI version, the selected console and base, the options used, and the relevant log. Read the log before sharing it and remove private information such as keys or personal paths.",
                    "UWUVCI stores logs below %LOCALAPPDATA%\\UWUVCI-V3\\Logs. The ReadMe and the Discord support community are useful starting points when an error is not covered by the console guide."
                },
                new[]
                {
                    new TutorialLink("ReadMe", "readme"),
                    new TutorialLink("UWUVCI Guide", GuideUrl),
                    new TutorialLink("Discord", "https://discord.gg/mPZpqJJVmZ")
                },
                new[]
                {
                    new TutorialQuestion("Where can you find console-specific UWUVCI instructions?",
                        "Select the console and open its question-mark help button.", "Only in the USB drive's format menu.", "In the quiz answer order.", "By changing the output folder name."),
                    new TutorialQuestion("What makes a useful UWUVCI error report?",
                        "The exact error, version, base, options, and a log checked for private information.", "Only a message saying that it does not work.", "Your private title keys posted publicly.", "Only the game's cover image."),
                    new TutorialQuestion("Where is the bundled UWUVCI FAQ?",
                        "In the ReadMe included with UWUVCI.", "Inside every game's save file.", "In the Wii U's serial-number label.", "In the USB Y-cable instructions.")
                }),
            new TutorialPage("Homebrew and Custom Firmware",
                new[]
                {
                    "Homebrew is software made outside Nintendo's official software system. Custom firmware, often shortened to CFW, provides the console environment needed for additional homebrew functions.",
                    "Aroma is a Wii U homebrew environment. Follow the Wii U Hacks Guide to set up the console, and launch the required environment before installing or running custom injections.",
                    "The PC program and the console setup are separate. Installing UWUVCI or copying its executable to an SD card does not install Aroma.",
                    "Use the maintained guide for your actual setup. Do not mix steps from unrelated or older setups, and do not assume that Wii U and vWii instructions are interchangeable."
                },
                new[] { new TutorialLink("Wii U Hacks Guide", HomebrewGuideUrl) },
                new[]
                {
                    new TutorialQuestion("What does CFW stand for?",
                        "Custom firmware.", "Console file width.", "Compressed file wrapper.", "Controller firmware widescreen."),
                    new TutorialQuestion("Which is a Wii U homebrew environment?",
                        "Aroma.", "FrameLayout.arc.", "The Windows Downloads folder.", "An N64 INI file."),
                    new TutorialQuestion("Where should you get instructions for setting up Wii U homebrew?",
                        "The maintained Wii U Hacks Guide for your setup.", "By combining arbitrary steps from unrelated videos.", "By copying the UWUVCI executable onto the SD card.", "By changing the injection's boot image.")
                }),
            new TutorialPage("Signature Patches",
                new[]
                {
                    "Custom injections need signature patches in the console environment. These patches allow content with signatures that the stock system would reject; they do not repair a bad game dump or improve emulator compatibility.",
                    "For Aroma, put 01_sigpatches.rpx in SD:/wiiu/environments/aroma/modules/setup/. This is an environment setup module, not a file for the plugins or install folder.",
                    "The environment must actually be loaded for its patches to take effect. After adding the module, restart into Aroma before trying the injection again.",
                    "If you use another environment, follow its own setup instructions. An installation or launch error should be checked against the guide rather than assumed to have only one possible cause."
                },
                new[] { new TutorialLink("UWUVCI Aroma Instructions", GuideUrl) },
                new[]
                {
                    new TutorialQuestion("Where does 01_sigpatches.rpx go when using Aroma?",
                        "SD:/wiiu/environments/aroma/modules/setup/", "SD:/install/", "SD:/wiiu/environments/aroma/plugins/", "Beside the UWUVCI executable on the PC."),
                    new TutorialQuestion("What should you do after adding the signature-patch setup module?",
                        "Restart into Aroma so the environment loads the module.", "Rename the module to the game's title.", "Delete the Wii U's system files.", "Change the injection's artwork."),
                    new TutorialQuestion("What do signature patches do?",
                        "Allow the console environment to accept content signatures rejected by the stock system.", "Guarantee compatibility with every ROM.", "Supply extra electrical power to a USB drive.", "Repair all incomplete game dumps.")
                }),
            new TutorialPage("SD Cards and Installation",
                new[]
                {
                    "The SD card used for the standard Wii U homebrew setup must be FAT32. Keep the guide's folder structure: Wii U homebrew applications belong under SD:/wiiu/apps/.",
                    "A packed UWUVCI injection is an installable title, not a homebrew app file. Place its complete output folder under SD:/install/, for example SD:/install/My Game/, and use a compatible Wii U installer.",
                    "Copying an output folder onto the SD card does not install it. The installer writes the title to the Wii U's internal storage or its Wii U-formatted USB storage.",
                    "Wii U-formatted USB storage is different from the FAT32 SD card. A PC may ask to format a Wii U USB drive because it cannot normally read it. Cancel that prompt to avoid erasing the drive."
                },
                new[]
                {
                    new TutorialLink("SD Card Preparation", "https://wiiu.hacks.guide/aroma/sd-preparation.html"),
                    new TutorialLink("UWUVCI Guide", GuideUrl)
                },
                new[]
                {
                    new TutorialQuestion("What format does the SD card need for the standard Wii U homebrew setup?",
                        "FAT32.", "NTFS.", "exFAT.", "The Wii U's proprietary USB storage format."),
                    new TutorialQuestion("Where should a packed injection folder be placed for installation from SD?",
                        "Under SD:/install/, keeping the complete title folder.", "Inside SD:/wiiu/environments/aroma/plugins/.", "In place of the SD card's wiiu folder.", "Inside the N64 INI file."),
                    new TutorialQuestion("What should you do if your PC asks to format a Wii U-formatted USB drive?",
                        "Cancel; formatting would erase its data.", "Format it so the installed games become readable.", "Rename it to FAT32 without a backup.", "Delete its partitions before using the Wii U again.")
                }),
            new TutorialPage("USB Drives and Power",
                new[]
                {
                    "Some USB-powered hard drives need more power than a single Wii U USB port can reliably supply. A suitable USB Y-cable draws power from two USB ports; it does not increase storage capacity or make the game run faster.",
                    "A drive with its own power supply is another option. Nintendo recommends externally powered hard drives for reliable Wii U storage. A drive that already has adequate power does not automatically need a Y-cable.",
                    "Insufficient power can cause a drive to stop responding or disconnect, interrupting reads or writes. Check the drive's power and cable connections when investigating storage errors.",
                    "Back up data before allowing the Wii U to format a drive, because formatting erases it. Do not disconnect storage or turn the console off while it is writing data."
                },
                new[] { new TutorialLink("Nintendo USB Storage Guidance", "https://www.nintendo.com/en-gb/Support/Legacy-system/Which-kind-of-USB-HDD-is-suitable-for-use-with-Wii-U-678127.html") },
                new[]
                {
                    new TutorialQuestion("Why might a USB-powered hard drive need a Y-cable on Wii U?",
                        "To draw additional power from a second USB port.", "To double the drive's capacity.", "To enable N64 widescreen.", "To replace signature patches."),
                    new TutorialQuestion("What can happen if a USB drive does not receive enough power?",
                        "It can disconnect or stop responding during reads or writes.", "Its storage capacity automatically increases.", "Its files are converted to a new game format.", "Every injection becomes more compatible."),
                    new TutorialQuestion("What is an alternative to using a Y-cable for a drive that needs more power?",
                        "A suitable drive with its own external power supply.", "A different game icon.", "Renaming the SD card.", "Disabling the NDS dark filter.")
                }),
            new TutorialPage("Backups and Safe Changes",
                new[]
                {
                    "Follow the guide's NAND-backup step when setting up homebrew. A NAND backup contains console system data; it is different from a copy of the SD card or an individual game's save backup.",
                    "Keep your console's backup files somewhere safe away from the SD card, such as on your PC and another backup device. Backups are console-specific. Another person's backup is not a substitute for your own.",
                    "A backup can help recovery, but does not make every change safe or guarantee an easy repair. Restoring a NAND backup can require specialist tools or hardware.",
                    "Read instructions before changing system files, use trusted project sources, and keep copies of important saves and SD files before formatting or replacing storage."
                },
                new[] { new TutorialLink("NAND Backup Guide", "https://wiiu.hacks.guide/aroma/nand-backup.html") },
                new[]
                {
                    new TutorialQuestion("Where should you keep your console's NAND backup?",
                        "In safe storage away from the SD card, with another copy where possible.", "Only on the SD card you intend to format.", "Only inside the injection's temporary folder.", "Nowhere after the first game launches."),
                    new TutorialQuestion("Is another person's NAND backup a substitute for your own?",
                        "No. The backup is specific to its console.", "Yes, if both consoles have the same colour.", "Yes, if their game icons match.", "Yes, whenever both consoles use Aroma."),
                    new TutorialQuestion("Does having a NAND backup make all system changes safe?",
                        "No. Recovery can still require specialist tools or hardware.", "Yes. Every mistake is repaired automatically.", "Yes. The console can no longer lose data.", "Yes. Instructions are no longer necessary.")
                })
        };
    }

    internal sealed class TutorialPage
    {
        public string Title { get; }
        public IReadOnlyList<string> Paragraphs { get; }
        public IReadOnlyList<TutorialLink> Links { get; }
        public IReadOnlyList<TutorialQuestion> Questions { get; }

        public TutorialPage(string title, string[] paragraphs, TutorialLink[] links, TutorialQuestion[] questions)
        {
            Title = title;
            Paragraphs = paragraphs;
            Links = links;
            Questions = questions;
        }
    }

    internal sealed class TutorialLink
    {
        public string Label { get; }
        public string Target { get; }

        public TutorialLink(string label, string target)
        {
            Label = label;
            Target = target;
        }
    }

    internal sealed class TutorialQuestion
    {
        public string Prompt { get; }
        public string CorrectAnswer { get; }
        public IReadOnlyList<string> OtherAnswers { get; }

        public TutorialQuestion(string prompt, string correctAnswer, params string[] otherAnswers)
        {
            Prompt = prompt;
            CorrectAnswer = correctAnswer;
            OtherAnswers = otherAnswers;
        }
    }
}
