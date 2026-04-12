# Constraints [Ver. 0.1]
The Constraints tool is an Unity Editor Window tool that allows the user to create transformation constraints stored in the Animator. A transform constrant allows objects to move based on another objects behaviour as if they follow a relationship. After creation, the keyframes can be further adjusted manually by the user, or restored to their original form before adding the constraints. 

**Complete feature list**
   - Create transform/rotation/scale constraints
   - Adjust weight individually, or all together
   - Adjust offset of the target bone, match offset to the targets values.
   - Set animation to Relative, allowing for movements to be relative to the source object.

### Let me know if you have any feedback, or run into issues. If you found the tool useful please give the repository a star!

## Installation
Installation for this should be similar to any other Unity editor tools you have installed. 
**Unity Import Package**
   - In Unity Editor go to Assets -> Import Package -> Custom Package...
   - Select the unity package file you have downloaded.
> [!NOTE]
> The **Constraints** tool relies on filepaths, and in it’s current state you **cannot** change the file structure for the editor script! If you need to do so, Ctrl + F ‘Assets/Editor/’ and it should show you the lines that have the filepaths hardcoded. The tool uses filepaths in order to save constraint information.

> [!NOTE]
> I have only tested this project on Windows, whether it works on Linux or Mac is **untested**.


## Inputs
Hovering over labels in the Editor Window should give you a description on what it is, otherwise the following may give you a clearer idea on what is going on.

<p align="center">
<img width="474" height="43" alt="image" src="https://github.com/user-attachments/assets/bd002fd9-8d12-4c5d-ba09-cd13544d7f7d" />
</p>

| Label  | Description |
| ------------- | ------------- |
| Source Animation  | Refers to the animation that you wish to fix broken keys from.  |
| Root Scene Object  | This is the object at the top of the hierarchy that holds the animation data.  |

<p align="center">
<img width="474" height="17" alt="image" src="https://github.com/user-attachments/assets/06d88615-ca75-4b0f-9d8b-0e699bd89d33" />
</p>

| Label  | Description |
| ------------- | ------------- |
| Source  | The source is the object the target inherits a relationship from: whatever keyframes source has, target will be constrained to them. |
| Target  | The target is the object that will be animated, in relations to source.   |
| Find  | The find buttons here will open a dropdown that let you pick the values for the source and target. <img width="479" height="245" alt="image" src="https://github.com/user-attachments/assets/a7a9fa1f-ae42-4d1f-9705-649174c12e23" /> _Dropdown that appears after clicking ‘find’_|

<p align="center">
<img width="474" height="85" alt="image" src="https://github.com/user-attachments/assets/97a4cb57-45ec-43d9-8aaf-b63bed1f0707" />
</p>

_The offset, if enabled, allows you to offset the target bone from the original animation._

| Label  | Description |
| ------------- | ------------- |
| Relative | Adds the source transform to the final transform of the target. This is meant to be used with 'Match Offset' |
| Transform  | Offsets the transform keyframe values by the values inside here. They are added after the final transform has been calculated from the weight.  |
| Rotation  | Offsets the transform keyframe values by the values inside here. They are added after the final rotation has been calculated from the weight.  |
| Scale | Offsets the transform keyframe values by the values inside here. They are added after the final scale has been calculated from the weight. |
| Match Offset |Sets the offset to be equal to the base transformation of the object. |
| Reset Offset | Sets offset values to 0 |

<p align="center">
<img width="474" height="104" alt="image" src="https://github.com/user-attachments/assets/d6646c69-2745-48b1-a925-03481dbbc333" />
</p>
The mix, determines how much weight the constraint has over the target bone. A mix value of 1 will have the bone inherit values exactly, and a mix value of 0 will keep the bone at its base values.
<p align="center">
<img width="474" height="115" alt="image" src="https://github.com/user-attachments/assets/8f75a75d-47e0-4b54-9b45-6a443462c6b7" />
</p>
To ease this process, you can also link these values. By linking these values they all inherit the same value from the top slider.

| Label  | Description |
| ------------- | ------------- |
| Key  | Clicking this activates the script to create a ‘constraint’. It will save the values into a scriptable object — if one doesnt already exist. If one exists already, it will overwrite it. It will also write keyframes onto the target object.  |
| Clear keys  | Clicking this removes keyframes from the target, resets it back to its ‘base’ initial keyframe, and deletes the scriptable object data.   |

> [!NOTE]
> You can always view your keyframe data in _Assets/AniToolkit/Constraints/_ unless you have edited the directories.

> [!WARNING]
> If you have changed the filepath, the tools may not work as expected, or at all. 

-- 

## Special thanks
**Alain Schneuwly**, for feedback, supervision, and tremendous support.

**Quimby, the cat**, for supervision. Bug inspector.

[**Caiden Muro**](https://www.caidendmuro.design/), for UI, UX feedback. This tool used to be a lot uglier, and clunkier to use without their feedback!

[**Corbyn Lamar**](https://corbyn-lamar.com/), for your knowledge and helpful tips. They’re amazing!

[**hfcRed**](https://github.com/hfcRed). When doing early research I had found their [tool](https://github.com/hfcRed/Animation-Repathing/tree/main), and it served as a initial reference point for editing animation data. 

**Skyler** @magussparky.bsky.social on [Bluesky](https://bsky.app/profile/magussparky.bsky.social), for introducing me to Unity Editor Window! I wouldn’t have gotten the idea to do this project without her.

