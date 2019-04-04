# UnturnedProfiler
A plugin to profile Unturned servers for analyzing lag causes and for profiling plugin performance.

## For Users
This plugin is meant for developers to optimize their plugins. It is currently not user friendly and it will not help you find any lag cause if you do not have required knowledge about plugin development.

This plugin was not published on RocketMod or other plugin sites on purpose.

## For Developers
Install this plugin on a server, run `/startp`, wait about 2-3 minutes and then do `/stopp` (stops profiling, not server).
After that a Profiler-XXXXXXX.log will be generated in `Unturned/Servers/<server instance>/Rocket/`.

The log will show how much time a method required to execute. Currently some of the Unity component functions (e.g. Update(), FixedUpdate(), LateUpdate() etc.) and some RocketMod and Unturned Events are profiled.