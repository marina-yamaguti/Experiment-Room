# VR Psychological Experiment Room

Fundamentals of eXtended Reality - Master HCI, Université Paris-Saclay
Final group project, September–October 2026

## Group 2

| Name |
|--- |
| Marina GELLER YAMAGUTI |
| Fatoumata OULARE |
| Meriem AIT AHMED |

## Project overview

A configurable VR room for psychological research: researchers can vary size, lighting,
colour, sound, furniture, and object presence to study how the environment affects a
participant's perception, behaviour, and emotional response. Participants complete simple
in-VR tasks (decisions, distance estimates, comfort/stress/attention self-reports) while the
app records measurable responses (completion time, choices, questionnaire results).

## What's implemented

_Update this section as features land — bullet points, not a changelog._

- [ ] Environment (SweetHome3D/Blender import)
- [ ] Touchpad locomotion (gliding/flying)
- [ ] Ray-cast / hand manipulation
- [ ] Runtime object activation/deactivation
- [ ] Canvas + 3D TextMeshPro UI
- [ ] Physics-driven interaction
- [ ] Interactive lighting
- [ ] 3D positional audio
- [ ] Countdown timer
- [ ] Reset logic on interactive objects/scenes

## How it works

_Short mechanism description goes here once the core loop is built — what the participant
does, in what order, and what the app records._

## Setup

- Unity **6.3 LTS (6000.3.24f1)** 
- Meta XR Core SDK + Interaction SDK + OpenXR Plugin (already resolved in `Packages/manifest.json`).
- Meta XR Simulator for headset-less development; a Meta Quest 3 for full testing.
- Git LFS is required before cloning/committing binary assets — run `git lfs install` once per machine.

## Submission

- Video recording of the app in the repo.
- This ReadMe kept current (members, contributions, implementation, mechanism).
- Unity project kept under 2GB total.
- Due: midnight, Thursday October 22, 2026 — repo link emailed to huyen.nguyen@universite-paris-saclay.fr.
