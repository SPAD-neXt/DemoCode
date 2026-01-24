**snAudioControl**

Add MasterVolume control to SPAD.neXt (>= v0.9.23.31)

# Installation
* Exit SPAD.neXt
* Download snAudioControl.zip
* Create a directory \<DOCUMENTS\>/SPAD.neXt/addons (where \<DOCUMENTS\/SPAD.neXt> is the SPAD.neXt Documents-Folder (My Documents/SPAD.neXt by default, unless you changed that in SPAD.neXt)
* Copy the snAudioControl.dll from the zip into the new directory
* Right click the dll , select Properties and make sure
  * The file is not blocked by windows defender (it should not say blocked at the bottom of the properies dialog, if it does click unblock)
  * The digital signature is valid (Tab "Digital Signature" , double click the entry, make sure it says valid)
* Start SPAD.neXt

# Usage
The addon provides a variable LOCAL:SPAD_AUDIOCONTROL_VOLUME which will hold the Windows volume of the primary speaker.

You can can change the value using normal actions , or even react on changes in conditions.

The value range is:

```0..100``` (mute .. full)

```-1```     (an error occoured)

Source is available here [https://github.com/SPAD-neXt/DemoCode/tree/main/Addons/snAudioControl]
