# VR Psychological Experiment Room: Change Blindness Study

Fundamentals of eXtended Reality - Master HCI, Université Paris-Saclay
Final group project, September–November 2026

## Group 2

| Name |
|--- |
| Marina GELLER YAMAGUTI |
| Fatoumata OULARE |
| Meriem AIT AHMED |

## Project overview

A VR experiment on **change blindness**: people often fail to notice even large changes in a scene
when the change happens during a brief visual disruption.

The participant starts in a **hub** with three doors, one per room. Every room has the same table with
the same five objects (mug, book, lamp, vase, picture frame). Only the **type of change** differs:

| Room | Change | What happens to the object |
|--- |--- |--- |
| A | Colour | one object changes colour |
| B | Position | one object shifts about 10 cm |
| C | Disappearance | one object disappears and is replaced by a look-alike |

Research question: does the type of change affect how often (and how fast) it is detected, and how
confident are participants about their answers? Every trial is logged automatically (accuracy, reaction
time, confidence), and a final questionnaire in VR compares the participant's impressions with their
real results.

## What's implemented

- [x] **Environment** — Hub and the experiment rooms modelled in Sweet Home 3D and imported as OBJ (Hub, Room A, Room B, Room C, End scene).
- [x] **Gliding locomotion** — left thumbstick glides relative to the head (flying optional), right thumbstick snap-turns 45°. The participant stays inside the room: the walls are found by name and act as limits, and the body is stopped by tables and doors (it slides along them). A Quest has thumbsticks instead of a touchpad, so the thumbstick plays that role (`GlideLocomotion`).
- [x] **Ray-cast manipulation** — a ray from the right controller hovers (green) and selects objects with the trigger (`RayPointer`, `PointerTarget`). Used for doors, table objects, confidence buttons, questionnaire answers and picking up objects.
- [x] **Runtime object activation/deactivation** — objects are hidden / swapped for a look-alike at runtime (`RoomObject.SetVisible`, Room C replacement `SetActive`).
- [x] **Canvas + 3D TextMeshPro UI** — instructions, countdown, confidence panel and the questionnaire are world-space 3D TextMeshPro on semi-transparent boxes. The light control panel in the ending scene is a **world-space Canvas** (Image + TextMeshPro UI elements) operated with the ray pointer. Nothing is attached to the screen, which does not work in a headset.
- [x] **Physics-driven interaction** — the book and pen in the ending scene are Rigidbodies: pick them up with the ray, put them down or throw them; they fall, collide and make impact sounds (`GrabbableProp`).
- [x] **Lighting controlled during the interaction** — in the ending scene a light panel (on a stand next to the table) switches the room light on/off and dims or brightens it in six steps (`RoomLight`, `LightPanel`); the room also dims automatically while the participant answers the questionnaire. In the hub, a spotlight above each door stays lit while that room is still to do and fades out once it is completed (`DoorSpotlight`).
- [x] **3D positional audio** — pickup, impact and page-turn sounds are played as 3D sources at the object's position (`SoundPlayer`); door sound, looping fan sound and background music in the hub (`GameManager`, Meriem), and in the ending scene the fan sound plus elevator music (`AmbientSound`) that replaces the global background music there.
- [x] **Countdown timer** — a small experiment timer (5 minutes, label "Experiment duration") fixed to the bottom right of the view in every scene, amber in the last minute and red at 00:00 (`ExperimentTimer`); plus a 5-second study countdown before every change (`TrialManager`).
- [x] **Reset logic** — every trial restores the objects to their original state (`RoomObject.ResetToOriginal`), props go back to the table before the questionnaire, and `GameManager.ResetSession()` starts a new participant. _(No reset button in VR yet.)_
- [x] **Data logging** — `trials.csv` and `questionnaire.csv` (see below).

## How it works

**Flow:** Hub → Room A / B / C (any order) → after the third room the participant is sent to the **End** scene.

**Controls:** left thumbstick = glide, right thumbstick = snap turn, **right trigger (or A)** = select / pick up / put down.
In the Meta XR Simulator, point and click moves the controller ray and sends the trigger press.

**One room (4 trials, each on a different object):**
1. A floating message asks the participant to press the trigger when ready.
2. **Study the table** — a 5-second countdown; movement is locked.
3. **Grey flicker** (~0.1 s) covers the view; the change is applied while it is covered.
4. "Which object changed?" — the participant points at the object and presses the trigger. Reaction time is measured from the question to the selection.
5. **Confidence** — a 0–5 panel (0 = pure guess, 5 = completely sure), selected with the ray.
6. The trial is written to `trials.csv`, the table is reset, next trial. After the last one the room is marked as completed (its door locks in the hub) and the participant returns to the hub.

**Ending scene (after the three rooms):** a table with a **book and a pen** (physics). When the participant has picked up both, the book opens as a **paper questionnaire** (handwritten look, one question per page: easiest / hardest room, easiest change, effect of distractions…). Answers go to `questionnaire.csv`. Then a **results page** summarises the participant's performance (correct answers overall and per room, average confidence, confidence when right vs. wrong, average reaction time, and the total time of the experiment), and the participant **turns the page** to reach "The End — thank you for taking part".

**Data files** (`Application.persistentDataPath`, printed in the Console on first write):
- Mac editor: `~/Library/Application Support/DefaultCompany/Experiment-Room/`
- Quest: `/sdcard/Android/data/<package>/files/` (pull with `adb` or Meta Quest Developer Hub)

| File | Columns |
|--- |--- |
| `trials.csv` | participantId, room, trialIndex, changeType, targetObject, selectedObject, correct (0/1), reactionTimeMs, confidence, timestamp |
| `questionnaire.csv` | participantId, questionId, question, answerIndex, answer, timestamp |

Delete both files between test sessions and real participants. The participant ID is `GameManager.participantId`.

### Code structure (`Assets/Scripts`)

| Script | Role |
|--- |--- |
| `GameManager` | persistent; participant ID, completed rooms, scene loading (sends the participant to `End` after the third room) |
| `GlideLocomotion`, `RayPointer`, `PointerTarget`, `SceneLink` | locomotion, ray pointer, selectable objects, doors |
| `TrialManager` | the trial loop of a room |
| `ChangeApplier` → `ColorChange`, `PositionChange`, `DisappearChange` | the only thing that differs between the rooms |
| `RoomObject`, `GreyFlicker`, `RoomHud`, `ConfidencePanel`, `WorldTextBox` | room objects, flicker mask, floating text and panels |
| `TrialLogger`, `QuestionnaireLogger`, `ResultsSummary` | CSV logging and the numbers on the results page |
| `EndingManager`, `GrabbableProp`, `PaperQuiz` | ending scene: pickup physics, paper questionnaire, results, page turn |
| `SoundPlayer`, `AmbientSound` | 3D sound effects, background music |
| `RoomLight`, `LightPanel` | scene lighting (on/off, 6 brightness steps, automatic dimming) and its world-space Canvas control panel |
| `ExperimentTimer` | the experiment countdown at the bottom right of the view (creates itself in every scene) |
| `DoorSpotlight` | the spotlight above each hub door: lit until the room is completed |
| `DoorSign`, `FanSpinner` | door signs, spinning ceiling fan |
| `Assets/Editor/*` | menu tools that group the imported Sweet Home 3D parts and set up the End scene |

### Adding or changing a room

Rooms share the same scripts. A new change type is a script that extends `ChangeApplier` (`ChangeType` and `Apply(RoomObject target)`) placed next to `TrialManager` on the `Trial` object, with `Room Id` set to the scene name. Hide objects with `SetVisible(false)`, never `SetActive(false)` (it would remove the collider). Trials per room, study time and flicker length are fields on `TrialManager`.

## Contributions

_Each member: check your section and edit it (the entries below were written from the Git history)._

### Marina GELLER YAMAGUTI
- Repository, Unity project set-up (URP, Meta XR Core + Interaction SDK, OpenXR, Quest/Android build settings, Git LFS, README).
- **Hub:** Sweet Home 3D model and import, three doors with coloured frames and "Experiment 1/2/3" signs (`DoorSign`), gliding locomotion and snap-turn (`GlideLocomotion`), ray pointer and door navigation (`RayPointer`, `PointerTarget`, `SceneLink`), persistent `GameManager`.
- **Room A (colour) and the shared template for all rooms:** room model; editor tools that regroup the imported parts (`GroupHubParts`, `GroupRoomParts`); `TrialManager` (trial loop), `ChangeApplier` / `ColorChange`, `RoomObject`, `GreyFlicker`, `RoomHud` and `WorldTextBox` (readable floating text), `ConfidencePanel` (0–5 rating), `TrialLogger` (CSV).
- **Ending scene:** physics book and pen (`GrabbableProp`), paper questionnaire in an open-book style with handwriting font and procedurally drawn pages (`PaperQuiz`, `QuestionnaireLogger`), results page and page turn (`ResultsSummary`), `EndingManager`, editor tool `Tools > End > Set up ending scene`, redirect to the ending after the third room.
- **Lighting and Canvas UI:** world-space Canvas light panel (`LightPanel`) and lighting control (`RoomLight`) in the ending scene.
- **Audio:** `SoundPlayer` / `AmbientSound`; all sound effects and the elevator-music loop were synthesised with Python (no third-party audio), stored in `Assets/Resources/Audio`.

### Fatoumata OULARE
- **Room C (disappearance):** scene, `DisappearChange` (the object disappears and is swapped for a look-alike so the empty spot does not give the answer away), look-alike replacement models (`Assets/Environment/Room_C`).
- Hub ceiling light.
- _(add anything else here)_

### Meriem AIT AHMED
- **Room B (position):** scene and `PositionChange` (per-object offset, kept on the table).
- Spinning ceiling fan in the hub (`FanSpinner`).
- Sound: door sound when entering a room, looping 3D fan sound, background music managed by `GameManager`.
- _(add anything else here)_

## Third-party assets and credits

- Fonts: **Devonshire** and **Beth Ellen** (SIL Open Font License, licences in `Assets/Fonts`). Devonshire is used for the handwritten questionnaire.
- Models: made in Sweet Home 3D (see `Assets/Environment`); replacement models for Room C by Fatoumata.
- Sounds: generated by script, no external audio.

## Setup

- Unity **6.3 LTS (6000.3.24f1)**.
- Meta XR Core SDK + Interaction SDK + OpenXR Plugin (already resolved in `Packages/manifest.json`).
- Meta XR Simulator for headset-less development; a Meta Quest 3 for full testing.
- Git LFS is required before cloning/committing binary assets — run `git lfs install` once per machine.
- Scenes in **File > Build Profiles > Scene List** (Hub must be first): `Hub`, `RoomA`, `RoomB`, `RoomC`, `End`.
- To run: open `Assets/Scenes/Hub.unity` and press Play (a scene can also be played on its own while testing; `GameManager` creates itself). To test the ending directly, play `End.unity`.
- Do not edit scripts while Play mode is on with the Meta XR Simulator (recompiling during Play can crash Unity). Unity > Settings > General > *Script Changes While Playing* = *Recompile After Finished Playing*.

## Still to do / known limits

- The light panel only exists in the ending scene (the hub has the door spotlights instead).
- Room redesign with a distraction task (button sequence to memorise) — in progress, trials per room may change.
- Participant ID is fixed in the editor for now (no in-VR selector).
- In-VR reset button for a new participant.
- Test the full flow on a Quest 3 (not only in the simulator).

## Submission

- Video recording of the app in the repo.
- This ReadMe kept current (members, contributions, implementation, mechanism).
- Unity project kept under 2GB total.
- Due: **before midnight, Wednesday 4 November 2026** (Chapter 5 logistics slide; earlier decks said 22 October — confirm with the group) — repo link emailed to huyen.nguyen@universite-paris-saclay.fr.
