# VoiceAttack demo profile

`SPAD.Next_VA_Demo-Profile.zip` contains a minimal [VoiceAttack](https://voiceattack.com/)
profile (`SPAD.Next_VA_Demo-Profile.vap`) that shows how VoiceAttack talks to
SPAD.neXt through the SPAD.neXt VoiceAttack plugin.

It defines one voice command, **"spad status"**. The command calls the plugin's
`GetValue` function for `LOCAL:SYSTEM READY` and speaks the result
(`Value: …`), or the error message if the call fails.

To use it, import the profile in VoiceAttack (*Profile → Import Profile*) and
enable plugin support in the VoiceAttack options.
