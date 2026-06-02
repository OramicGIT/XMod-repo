# XMod

## About

XMod is a physics sandbox game where you can build anything you can imagine.

## Getting Started

1. Download the latest release from [itch.io](https://oramix.itch.io/xmod)
2. Extract the archive
3. Run the executable (`.exe` on Windows or `.x86_64` on Linux)

> **Linux:** Before running, open a terminal in the game folder and run:
> ```
> chmod +x XMod.x86_64
> ```

## Modding

XMod supports community mods.

Place mods in the following folder:

- **Windows:** `C:\Users\%USER%\AppData\LocalLow\OramiX\XMod\Mods`
- **Linux:** `/home/<user>/.config/unity3d/OramiX/XMod/Mods`
- **Android:** `/android/data/com.OramiX.XMod/Mods`
- **itch.io WebGL:** `Not supported`

> **Note:** These folders are hidden by default.
> On Linux, press `Ctrl + H` to show hidden files.
> On Windows, enable **Show hidden files** in Folder Options.
> On Mac, press `Command + Shift + period` in Finder to show hidden files

## Building from Source

### Requirements

- Unity **2022.3.62f3** (exactly this version)

### Steps

1. Clone the repository:
   git clone https://github.com/OramicGIT/XMod-repo.git
2. Unpack `Code.zip`
3. Create a new Unity project, then replace the Assets, Packages and ProjectSettings with unpacked folders, and remove the Library and Temp folder before starting Unity again.
4. Open the final project in Unity.
5. Open the main scene (`Menu`) and hit **Play** to test, or build via **File → Build Settings**

## Contributing

Contributions are welcome! Feel free to open an issue!

- **Bug reports:** Use the [Issues](https://github.com/OramicGIT/XMod-repo/issues) tab

## License

This project is licensed under the **GNU General Public License v3.0**.
See the [LICENSE](LICENSE) file for details.

**In simple terms:** You are free to modify the code or assets — just keep the same license and credit the original author.

> Some assets were created by contributors from [itch.io](https://itch.io), [OpenGameArt.org](https://opengameart.org) and [Pixabay](https://pixabay.com/sound-effects/).

## Links

- [GitHub Pages](https://oramicgit.github.io/XMod-repo)
- [itch.io](https://oramix.itch.io/xmod)

## A note
Hello everyone,

I am writing this to share an important update regarding the current status and future direction of this project.

After some careful reflection on the current state of development, I have decided to implement significant changes to the game’s core assets. Moving forward, I will be removing all creative contributions from Emery, including specific character designs and audio tracks that were previously integrated into the build.

This decision stems from a fundamental divergence in creative vision and the operational philosophy behind the project. As the sole developer, it is important to me that every element within the game reflects a singular, cohesive intent. Maintaining external dependencies that no longer align with the project’s evolution has become counterproductive to the experience I aim to create. My goal is to ensure that the project remains a pure reflection of my own design language and technical vision, free from the constraints of collaborative compromises that have hindered the development process.

I want to express my gratitude to those who have followed the development journey so far. This transition is a necessary step to ensure that the project can grow into the vision I have always intended for it. Thank you for your continued patience and understanding as I move forward with these changes.

Best regards,

!OramiX (the lead developer of the game)

P.S. The game is temporarily unavailable for download while I replace certain assets. I’ve decided to move the project in a new direction, as I prefer to keep my creative work free from external ideological agendas and personal stances that don't align with my own. A clean update will be released shortly.
