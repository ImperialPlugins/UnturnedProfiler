# UnturnedProfiler
A plugin to profile Unturned servers for analyzing lag causes and for profiling plugin performance

Since this started to draw a lot of attention let me make this clear:
This plugin is meant for developers to optimize their plugins. It is currently not user friendly and it will not help you find any lag cause if you do not have required knowledge about plugin development.

The plugin is not on RocketMod or other plugin sites on purpose.

For developers:
Install this plugin on a server, run `/startp`, wait about 2-3 minutes and then do `/stopp` (stops profiling, not server).
After that a Profiler.log will be written at `Unturned/Servers/<server instance>/Rocket/`.

The log will show how much time a method requires to execute. Currently only Update(), FixedUpdate(), LateUpdate() and some methods which use Rocket Events are profiled.

Depending on the result of this suggestion: https://github.com/pardeike/Harmony/issues/36 I will add even more profiling.
