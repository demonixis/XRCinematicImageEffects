# XRCinematicImageEffects
This is an updated fork of the [Unity's Cinematic Image Effects](https://bitbucket.org/Unity-Technologies/cinematic-image-effects) for Unity 2017.3+ with Single Pass Stereo Rendering. Single Pass Instanced is planned for later. Because some effects are hard to convert, this repository contains only working effects.

## Compatibility
| Effect | Single Pass Stereo | Single Pass Instanced |
|--------|--------------------|-----------------------|
| FXAA | Yes | No |
| SMAA | Yes | No |
| Vignette | Yes but the vignette center is bad | No |
| Chromatic Aberation | Yes | No |
| Heigh Fog | Yes | No |

## About the Post Process Stack V2
The new stack is great, but it's not compatible with the Universal Windows Projects when targeting .Net. Because all projects can't be compiled with IL2CPP, this fork is very usefull if you need post process. You'll find a some great assets on the Asset Store, but nohting about AntiAliasing. This repository has a working FXAA and SMAA in VR with Single Pass Stereo Rendering.

# License
Like the original project on Bitbucket, this project is licensed under the MIT License.
