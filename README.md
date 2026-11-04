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
when the change happens while they are focused on another task.

The participant starts in a **hub** with three doors, one per room. Every room has the same table with
the same five objects (mug, book, lamp, vase, picture frame) and a plate with 4 buttons. They all have the same task to accomplish. Only the **type of change** differs:

| Room | Change | What happens to the object |
|--- |--- |--- |
| A | Colour | two objects change colour |
| B | Position | two objects shift about 10 cm |
| C | Disappearance | two objects disappear and are replaced by look-alikes |

**Research question**: do participants notice changes that happen without warning while they are busy with a memory task, and does the type of change (colour, position, disappearance) matter? Each trial is logged automatically (sequence accuracy, answer time, confidence, and which object changed), and the final questionnaire asks what the participant noticed.

## Design iterations
1. **Waiting room prototype:** one themed room with the grab/point interactions and trigger-based UI that the later versions reuse.
2. **Change-blindness rooms with a flicker (proposal version):** At first we had 4 trials with a study the table, a grey flicker, then "Which object changed?". We thought it was trivial because the participants knew a change was coming every trial, so the task measured search, not really change blindness.
3. **Hidden changes during a task (current version):** We were inspired by Simons' *Monkey Business Illusion*. Participants are given a demanding memory task and the changes happen during it without warning. Whether they noticed is only asked at the very end, in the questionnaire.

## What's implemented
- [x] **Environment** — Hub and the experiment rooms modelled in Sweet Home 3D and imported as OBJ (Hub, Room A, Room B, Room C, End scene).
- [x] **Gliding locomotion** — left thumbstick glides relative to the head (flying optional), right thumbstick snap-turns 45°. The participant stays inside the room: the walls are found by name and act as limits, and the body is stopped by tables and doors (it slides along them). A Quest has thumbsticks instead of a touchpad, so the thumbstick plays that role (`GlideLocomotion`).
- [x] **Ray-cast manipulation** — a ray from the right controller hovers (green) and selects objects with the trigger (`RayPointer`, `PointerTarget`). Used for doors, the sequence task buttons, confidence buttons, questionnaire answers and picking up objects.
- [x] **Runtime object activation/deactivation** — objects are hidden / swapped for a look-alike at runtime (`RoomObject.SetVisible`, Room C replacement `SetActive`).
- [x] **Canvas + 3D TextMeshPro UI** — instructions, countdown, confidence panel and the questionnaire are world-space 3D TextMeshPro on semi-transparent boxes. The light control panel in the ending scene is a **world-space Canvas** (Image + TextMeshPro UI elements) operated with the ray pointer. Nothing is attached to the screen, which does not work in a headset.
- [x] **Physics-driven interaction** — the book and pen in the ending scene are Rigidbodies: pick them up with the ray, put them down or throw them; they fall, collide and make impact sounds (`GrabbableProp`).
- [x] **Lighting controlled during the interaction** — in the ending scene a light panel (on a stand next to the table) switches the room light on/off and dims or brightens it in six steps (`RoomLight`, `LightPanel`); the room also dims automatically while the participant answers the questionnaire. In the hub, a spotlight above each door stays lit while that room is still to do and fades out once it is completed (`DoorSpotlight`). In the three experiment rooms, each sequence button has its own point light and emissive material that switch on when the button flashes or is pressed (`SequenceButton`).
- [x] **3D positional audio** — pickup, impact and page-turn sounds are played as 3D sources at the object's position (`SoundPlayer`); door sound, looping fan sound and background music in the hub (`GameManager`, Meriem), and in the ending scene the fan sound plus elevator music (`AmbientSound`) that replaces the global background music there.
- [x] **Countdown timer** — a small experiment timer (5 minutes, label "Experiment duration") fixed to the bottom right of the view in every scene, amber in the last minute and red at 00:00 (`ExperimentTimer`); plus a 3 seconds countdown before each sequence of the memory task (`SequenceTask`).
- [x] **Reset logic** — each room starts from its original state when the scene loads (the changed objects stay changed until the room ends, on purpose, so the participant can notice them later); `RoomObject.ResetToOriginal` restores position, colour and visibility and hides Room C look alikes; props go back to the table before the questionnaire, and `GameManager.ResetSession()` starts a new participant. _(No reset button in VR yet.)_
- [x] **Data logging** — `trials.csv` and `questionnaire.csv` (see below).
- [x] **Scripting of object behaviours and logic** — in the sequence memory task (`SequenceTask`) random sequences of increasing length and speed, input checking, two hidden changes per room on random objects timed with a button flash, confidence after each trial; buttons with a press animation (`SequenceButton`); the `SequencePad` prefab shares the same task across rooms A, B and C.

## How it works

**Flow:** Hub → Room A / B / C (any order) → after the third room the participant is sent to the **End** scene.

**Controls:** left thumbstick = glide, right thumbstick = snap turn, **right trigger (or A)** = select / pick up / put down.
In the Meta XR Simulator, point and click moves the controller ray and sends the trigger press.

**One room (3 trials):**
1. A floating message asks the participant to walk to the table and press the trigger when ready.
2. **Sequence memory** — starting with a 3 seconds countdown. 4 coloured buttons light up in a random order (3, then 4, then 5 flashes, each trial faster).
3. **Hidden changes** in trials 2 and 3, one random object on the table changes (colour in A, position in B, swapped for a look-alike in C) at the exact moment a button flashes. Nothing announces it.
4. **Repeat the sequence** — the participant repeats the order by pointing at the buttons with the ray and pressing the trigger. The answer time is measured from "Your turn" to the last click and the result is not shown.
5. **Confidence** — a 0–5 panel (0 = pure guess, 5 = completely sure), selected with the ray.
6. The trial is written to `trials.csv` and the next trial starts. After the last one the room is marked as completed (its door locks in the hub) and the participant returns to the hub.

**Ending scene (after the three rooms):** a table with a **book and a pen** (physics). When the participant has picked up both, the book opens as a **paper questionnaire** (handwritten look, one question per page: easiest / hardest room, easiest change, effect of distractions…). Answers go to `questionnaire.csv`. Then a **results page** summarises the participant's performance (correct answers overall and per room, average confidence, confidence when right vs. wrong, average reaction time, and the total time of the experiment), and the participant **turns the page** to reach "The End — thank you for taking part".

**Data files** (`Application.persistentDataPath`, printed in the Console on first write):
- Mac editor: `~/Library/Application Support/DefaultCompany/Experiment-Room/`
- Quest: `/sdcard/Android/data/<package>/files/` (pull with `adb` or Meta Quest Developer Hub)

| File | Columns |
|--- |--- |
| `trials.csv` | one row per sequence trial: participantId, room, trialIndex, changeType, targetObject (the object that changed in that trial, `none` in trial 1), selectedObject (`sequence`), correct (1 = sequence repeated correctly), reactionTimeMs (time to repeat the sequence), confidence, timestamp |
| `questionnaire.csv` | participantId, questionId, question, answerIndex, answer, timestamp |

Delete both files between test sessions and real participants. The participant ID is `GameManager.participantId`.

### Code structure (`Assets/Scripts`)

| Script | Role |
|--- |--- |
| `GameManager` | persistent; participant ID, completed rooms, scene loading (sends the participant to `End` after the third room) |
| `GlideLocomotion`, `RayPointer`, `PointerTarget`, `SceneLink` | locomotion, ray pointer, selectable objects, doors |
| `TrialManager` | the trial loop of the previous version (study, flicker, "which object changed"). Kept for reference but switched off in the rooms |
| `SequenceTask`, `SequenceButton` | the sequence memory task in every room: 3 trials, countdown, hidden changes on 2 random objects at a button flash, confidence after each trial, logging (`SequenceTask`); one button with light-up and press animation adapted from XR Lab 2 (`SequenceButton`). Packed in the `SequencePad` prefab (`Assets/Prefabs`) |
| `ChangeApplier` → `ColorChange`, `PositionChange`, `DisappearChange` | the only thing that differs between the rooms. Called by `SequenceTask` during the task |
| `RoomObject`, `GreyFlicker`, `RoomHud`, `ConfidencePanel`, `WorldTextBox` | room objects, flicker mask (previous version, not used any more), floating text and panels |
| `TrialLogger`, `QuestionnaireLogger`, `ResultsSummary` | CSV logging and the numbers on the results page |
| `EndingManager`, `GrabbableProp`, `PaperQuiz` | ending scene: pickup physics, paper questionnaire, results, page turn |
| `SoundPlayer`, `AmbientSound` | 3D sound effects, background music |
| `RoomLight`, `LightPanel` | scene lighting (on/off, 6 brightness steps, automatic dimming) and its world-space Canvas control panel |
| `ExperimentTimer` | the experiment countdown at the bottom right of the view (creates itself in every scene) |
| `DoorSpotlight` | the spotlight above each hub door: lit until the room is completed |
| `DoorSign`, `FanSpinner` | door signs, spinning ceiling fan |
| `Assets/Editor/*` | menu tools that group the imported Sweet Home 3D parts and set up the End scene |

### Adding or changing a room

Rooms share the same scripts and the `SequencePad` prefab. A new change type is a script that extends `ChangeApplier` (`ChangeType` and `Apply(RoomObject target)`) placed on the `Trial` object, with `TrialManager` unticked. On the prefab's `SequenceTask`, set `Room Id` to the scene name and drag the `Trial` object into `Applier`; keep the pad and the five objects at the same positions in every room. Hide objects with `SetVisible(false)`, never `SetActive(false)` (it would remove the collider). The changed objects are random, so every object needs a value in the change script. Sequence lengths and flash speeds are set at the top of `SequenceTask`.


## Contributions


### Marina GELLER YAMAGUTI
- Repository, Unity project set-up (URP, Meta XR Core + Interaction SDK, OpenXR, Quest/Android build settings, Git LFS, README).
- **Hub:** Sweet Home 3D model and import, three doors with coloured frames and "Experiment 1/2/3" signs (`DoorSign`), gliding locomotion and snap-turn (`GlideLocomotion`), ray pointer and door navigation (`RayPointer`, `PointerTarget`, `SceneLink`), persistent `GameManager`.
- **Room A (colour) and the shared template for all rooms:** room model; editor tools that regroup the imported parts (`GroupHubParts`, `GroupRoomParts`); `TrialManager` (trial loop, previous version), `ChangeApplier` / `ColorChange`, `RoomObject`, `GreyFlicker` (previous version), `RoomHud` and `WorldTextBox` (readable floating text), `ConfidencePanel` (0–5 rating), `TrialLogger` (CSV).
- **Ending scene:** physics book and pen (`GrabbableProp`), paper questionnaire in an open-book style with handwriting font and procedurally drawn pages (`PaperQuiz`, `QuestionnaireLogger`), results page and page turn (`ResultsSummary`), `EndingManager`, editor tool `Tools > End > Set up ending scene`, redirect to the ending after the third room.
- **Lighting and Canvas UI:** world-space Canvas light panel (`LightPanel`) and lighting control (`RoomLight`) in the ending scene, with automatic dimming during the questionnaire; spotlights above the Hub doors that switch off once a room is completed (`DoorSpotlight`).
- **Experiment timer:** 5-minute countdown at the bottom of the view in every scene (`ExperimentTimer`); the total time is shown on the results page.
- **Collisions and room limits:** gap fixes in the Hub and End walls (`SealRoomGaps`), and `GlideLocomotion` keeps the participant inside the room and stops them at walls, tables and doors.
- **Demo video:** recorded and edited the video demo (see Additional information).
- **Audio:** `SoundPlayer` / `AmbientSound`; all sound effects and the elevator-music loop were synthesised with Python (no third-party audio), stored in `Assets/Resources/Audio`.

### Fatoumata OULARE
- **Room C (disappearance/swap):** scene, `DisappearChange` (the object disappears and is swapped for a look-alike so the empty spot does not give the answer away), `replacement` field in `RoomObject`, look-alike models in `Assets/Environment/Room_C` (see credits).
- **Sequence memory task used in all three rooms as the main task:** `SequenceTask` (3 trials of increasing length and speed, countdown, two hidden changes per room on random objects, confidence after each trial, data saved on `trials.csv`) and `SequenceButton` (press animation, light-up feedback), `SequencePad` prefab placed in rooms A, B and C (plate with buttons).
- **Rooms layout:** objects grouped beside the task so they stay at the edge of attention.
- **Hub:** light (ceiling spotlight and fill light).

### Meriem AIT AHMED
- **Room B (position):** scene and implemented `PositionChange`, allowing each object to have a different position offset while ensuring they remain on the table. Each object's original position is restored after every trial using `RoomObject.ResetToOriginal()`.
- **Hub**: Added a spinning ceiling fan using `FanSpinner`. The fan model was created with Sweet Home 3D.
- **Audio**: Integrated the looping fan sound, door sounds and ambient music (Fan, DoorSound, AmbientMusic), stored in Assets/Resources/Audio.
- **Questionnaire**: Added post-experiment questions covering the number and type of changes noticed in each room, as well as participants' confidence in identifying all changes.
  
## Third-party assets and credits

- Fonts: **Devonshire** and **Beth Ellen** (SIL Open Font License, licences in `Assets/Fonts`). Devonshire is used for the handwritten questionnaire.
- Models: rooms made in Sweet Home 3D (see `Assets/Environment`). Room C look alikes: **Mug** and **Desk Lamp1** from the Sweet Home 3D contributors library (Free Art License); **Ceramic Vase 01**, **Binder Notebook** and **Alarm Clock 01** from Poly Haven (CC0).
- Sounds: some audio effects were synthesised using Python scripts and some other sounds were external audio assets sourced from Pixabay.
  
## Setup

- Unity **6.3 LTS (6000.3.24f1)**.
- Meta XR Core SDK + Interaction SDK + OpenXR Plugin (already resolved in `Packages/manifest.json`).
- Meta XR Simulator for headset-less development; a Meta Quest 3 for full testing.
- Git LFS is required before cloning/committing binary assets — run `git lfs install` once per machine.
- Scenes in **File > Build Profiles > Scene List** (Hub must be first): `Hub`, `RoomA`, `RoomB`, `RoomC`, `End`.
- To run: open `Assets/Scenes/Hub.unity` and press Play (a scene can also be played on its own while testing; `GameManager` creates itself). To test the ending directly, play `End.unity`.
- Do not edit scripts while Play mode is on with the Meta XR Simulator (recompiling during Play can crash Unity). Unity > Settings > General > *Script Changes While Playing* = *Recompile After Finished Playing*.

## Limitations

Here are the things we would change if we had more time:
- The light panel only exists in the ending scene (the hub has the door spotlights instead).
- Participant ID is fixed in the editor for now (no in-VR selector).
- In-VR reset button for a new participant.
- Test the full flow on a Quest 3 (not only in the simulator).
- Add more object everywhere in the room to make the changes more subtle.

## Additional information

- **Video demo:** [Google Drive folder](https://drive.google.com/drive/folders/1qDHZGwsvJVTBoF_VSR-R5SPpSv3N-gM8?usp=sharing). The video is not in the repository because of its size: it is recorded in 4K, since we had to film the screen with a camera to capture the sound, which makes the file too large for GitHub.
- **Video demo (YouTube, unlisted):** https://youtu.be/wG9JoYLaDm4 — the same demo, viewable directly without downloading it.
