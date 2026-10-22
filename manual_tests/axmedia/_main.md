# AxMedia manual tests

The steps and every expected announcement are in `docs/axmedia/nvda-test-plan.md`. This file says which folder each section uses. Run `.\manual_tests\prepare.ps1 axmedia` first.

Every sound file says what it is at its start and then the time every 5 seconds, so you hear which file plays and where.

## first-queue-play-this-folder

Section 1 and section 3, steps 1 to 4 and 8.

- Four tracks of 15 seconds, each saying "First queue, track n of 4".
- `not-media-a-text-file.txt`: add it with Ctrl+O, All files; Enter on it says "Cannot play" and the next plays.
- `cannot-decode-broken.opus`: not real sound; "Cannot play" and the next plays.
- Start AxMedia empty, Ctrl+Shift+O, pick this folder: "Added 5", the four tracks and the `.opus` file, which counts as sound by its name.

## replacement-queue-play-to-replace-the-first

Does playing a folder replace the queue? Play `first-queue-play-this-folder` first. Then, from AxFile on this folder, Ctrl+Enter (or `axmedia` with this folder's path): the queue now holds only the three "Replacement queue" tracks, and the first queue's tracks are gone. Section 2, steps 1 and 2.

## add-to-queue-folder-a and add-to-queue-folder-b

Does adding keep the queue? With anything playing, in AxFile select both folders, Ctrl+Shift+Q: "Added 6 to AxMedia's queue", the sound goes on. In AxMedia, End: the last tracks say "Added folder B". Section 2, steps 3 and 4. From the console: `axmedia --add <path of add-to-queue-folder-a>`.

## seek-and-speed-one-long-file

Section 3, steps 2 to 6. One file of 3 minutes that says the time every 5 seconds.

- Ctrl+Right, Ctrl+Left: the voice you hear names the time you landed on.
- Ctrl+T, `1:00`, Enter: you hear "1 minute" within 5 seconds.
- Ctrl+Shift+Period: faster, at the same pitch.
- Ctrl+T, `2:55`, Enter, wait: "End of queue" when it is the only file.

## end-of-queue-two-short-files

Section 3, steps 5 and 6. Two files of a few seconds: the first, then "Playing short-2-of-2-last", then "End of queue" with the chime.

## video

Section 3, step 7. `video-short-mp4-from-windows-setup.mp4` is a short video copied from Windows' setup; the picture shows at the top of the window. When this Windows had none, the folder holds a text file asking for any short .mp4. `sound-after-the-video.wav` plays after it, and the picture goes.

## no-media-folder

Section 1, step 2: Ctrl+Shift+O on this folder says "No sound or video files in no-media-folder".

## Not in a folder

- Section 2, step 5, the default program: needs Windows Settings. Use the `.wav` files here, or your own `.mp3` files.
- Section 2, step 6, the console: `axmedia <a file here>`, `axmedia --add <a folder here>`, `axmedia --new <a file here>`.
- Section 4: any queue.
