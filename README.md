# XR Cinematic Image Effects
This is an updated fork of the [Unity's Cinematic Image Effects](https://bitbucket.org/Unity-Technologies/cinematic-image-effects) for Unity 2017.3+ with Single Pass Stereo Rendering. Single Pass Instanced is planned for later. Because some effects are hard to convert, this repository contains only working effects.

# Compatibility
| Effect | Note |Single Pass Stereo | Single Pass Instanced |
|--------|------|-------------------|-----------------------|
| FXAA | Ok | Yes | No |
| SMAA | Temporal doesn't work | Yes | No |
| Distortion | The center is not good | Mostly | No |
| Vignette | Ok | Mostly | No |
| Chromatic Aberration | Ok | Yes | No |
| Heigh Fog | Ok | Yes | No |
| Depth Of Field | WIP | Mostly | No |

## About the Post Process Stack V2
The new stack is great, but it's not compatible with the Universal Windows Projects when targeting .Net. Because all projects can't be compiled with IL2CPP, this fork is very usefull if you need post process. You'll find a some great assets on the Asset Store, but nohting about AntiAliasing. This repository has a working FXAA and SMAA in VR with Single Pass Stereo Rendering.

# License
Like the original project on Bitbucket, this project is licensed under the MIT License.
