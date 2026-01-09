# VapourSynth Studio - Manual Test Checklist

Use this checklist to verify application functionality before releases. Each section covers a specific feature area.

---

## A. Application Startup

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| App launches | Double-click VapourSynthPortable.exe | Application window opens without errors | [ ] |
| Log viewer panel | Check bottom of main window | Log viewer panel is visible | [ ] |
| Status bar | Check status bar at bottom | Shows VapourSynth/Python/FFmpeg status indicators | [ ] |
| Default page | Observe initial view | Restore page is shown by default | [ ] |
| No crash on startup | Launch app multiple times | No crashes or hangs | [ ] |

---

## B. Media Page Testing

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| Import single file (drag) | Drag a video file onto media pool | File appears in media pool | [ ] |
| Import single file (dialog) | File > Import Media, select file | File appears in media pool | [ ] |
| Import multiple files | Select multiple files in import dialog | All files appear in media pool | [ ] |
| Thumbnail generation | Import a video file | Thumbnail appears after brief loading | [ ] |
| Double-click source | Double-click item in media pool | Item becomes current source, preview updates | [ ] |
| Right-click delete | Right-click item > Delete | Item removed from pool | [ ] |
| Metadata display | Select item in pool | Shows resolution, duration, codec info | [ ] |
| Duplicate import | Import same file twice | Returns existing item, no duplicate | [ ] |
| Invalid file import | Try importing non-media file | Graceful error, no crash | [ ] |

---

## C. Edit Page (Timeline) Testing

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| Timeline displays | Navigate to Edit page | Time ruler and tracks visible | [ ] |
| Default tracks | Check track list | V1, V2, A1, A2 tracks present | [ ] |
| Add clip to timeline | Drag media from pool to track | Clip appears on timeline | [ ] |
| Move clip | Drag clip along timeline | Clip repositions correctly | [ ] |
| Trim clip start | Drag left edge of clip | In-point changes, clip shortens | [ ] |
| Trim clip end | Drag right edge of clip | Out-point changes, clip shortens | [ ] |
| Delete clip | Select clip, press Delete | Clip removed from timeline | [ ] |
| Playhead click | Click on time ruler | Playhead moves to click position | [ ] |
| Playhead drag | Drag playhead | Playhead follows mouse, preview updates | [ ] |
| Zoom in | Mouse wheel up or zoom button | Timeline zooms in, more detail visible | [ ] |
| Zoom out | Mouse wheel down or zoom button | Timeline zooms out, more clips visible | [ ] |
| Preview playback | Press spacebar | Playback starts/stops | [ ] |
| Snap to playhead | Drag clip near playhead | Clip snaps to playhead position | [ ] |

---

## D. Restore Page Testing

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| Load video | Select source from media pool | Video appears in preview panel | [ ] |
| Play/Pause | Click play button or spacebar | Video plays/pauses | [ ] |
| Seek slider | Drag seek slider | Video seeks to position | [ ] |
| Frame step forward | Press right arrow | Video advances one frame | [ ] |
| Frame step backward | Press left arrow | Video goes back one frame | [ ] |
| Apply deinterlace | Select deinterlace filter | Preview shows deinterlaced output | [ ] |
| Apply denoise | Select denoise filter | Preview shows denoised output | [ ] |
| Before/After toggle | Click compare button | Toggles between filtered/original | [ ] |
| Generate script | Click Generate Script | Creates valid .vpy file | [ ] |
| Node editor visible | Check node editor panel | Nodes and connections visible | [ ] |
| Add filter node | Add node from menu/palette | New node appears in editor | [ ] |
| Connect nodes | Drag from output to input | Connection line appears | [ ] |
| Delete connection | Click connection, press Delete | Connection removed | [ ] |

---

## E. Color Page Testing

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| Color wheels visible | Navigate to Color page | Lift/Gamma/Gain wheels displayed | [ ] |
| Lift adjustment | Drag lift wheel | Shadows affected in preview | [ ] |
| Gamma adjustment | Drag gamma wheel | Midtones affected in preview | [ ] |
| Gain adjustment | Drag gain wheel | Highlights affected in preview | [ ] |
| RGB curves | Adjust curve points | Color response changes in preview | [ ] |
| Vectorscope | Check scopes panel | Vectorscope shows color distribution | [ ] |
| Waveform | Check scopes panel | Waveform shows luminance levels | [ ] |
| Histogram | Check scopes panel | Histogram shows tonal distribution | [ ] |
| Reset controls | Click Reset button | All adjustments return to neutral | [ ] |

---

## F. Export Page Testing

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| Preset list | Navigate to Export page | H.264, H.265, ProRes presets available | [ ] |
| Select H.264 | Click H.264 preset | Settings update for H.264 | [ ] |
| Select H.265 | Click H.265 preset | Settings update for H.265/HEVC | [ ] |
| Select ProRes | Click ProRes preset | Settings update for ProRes | [ ] |
| Quality slider | Adjust quality/CRF slider | Value updates, affects output quality | [ ] |
| Browse output | Click Browse for output path | File dialog opens | [ ] |
| Set output path | Select destination folder/file | Path displays correctly | [ ] |
| Start encode | Click Export/Encode button | Progress bar appears, encoding starts | [ ] |
| Progress updates | Monitor during encode | Progress bar updates, percentage shown | [ ] |
| Cancel encode | Click Cancel during encode | Encoding stops, partial file may remain | [ ] |
| Encode completes | Wait for encode to finish | Success message, file created | [ ] |
| Verify output | Play exported file | Video plays correctly in external player | [ ] |

---

## G. Project Management

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| New project | Ctrl+N or File > New | Blank project created, prompts to save if dirty | [ ] |
| Save project | Ctrl+S or File > Save | Project saved to .vsproj file | [ ] |
| Save As | Ctrl+Shift+S or File > Save As | File dialog, saves to new location | [ ] |
| Open project | Ctrl+O or File > Open | File dialog, project loads | [ ] |
| Recent projects | File > Recent Projects | Shows list of recent files | [ ] |
| Open from recent | Click recent project entry | Project loads correctly | [ ] |
| Unsaved indicator | Modify project | Title bar shows asterisk (*) | [ ] |
| Close with unsaved | Close app with unsaved changes | Prompt to save appears | [ ] |
| Save before close | Click Save in prompt | Project saves, app closes | [ ] |
| Discard changes | Click Don't Save in prompt | App closes without saving | [ ] |
| Cancel close | Click Cancel in prompt | App remains open | [ ] |

---

## H. Video Preview (All Pages)

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| libmpv loads | Check for library errors on startup | No "library not found" errors | [ ] |
| Video displays | Load any video | Frames render in preview panel | [ ] |
| Audio plays | Play video with audio | Sound output works | [ ] |
| Volume control | Adjust volume slider | Audio level changes | [ ] |
| Mute toggle | Click mute button | Audio mutes/unmutes | [ ] |
| Seek accuracy | Click random position in video | Jumps to correct position | [ ] |
| Loop playback | Enable loop, play to end | Video loops back to start | [ ] |
| Aspect ratio | Load 16:9 and 4:3 videos | Correct aspect ratio maintained | [ ] |
| Full-screen preview | Double-click preview | Preview enters full-screen mode | [ ] |
| Exit full-screen | Press Escape | Returns to normal view | [ ] |

---

## I. Log Viewer Panel

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| Toggle visibility | Click Output/Log button | Panel shows/hides | [ ] |
| Logs appear | Perform various actions | Log entries appear in viewer | [ ] |
| Level: All | Select "All" filter | Shows all log levels | [ ] |
| Level: Info | Select "Info" filter | Shows only Info and above | [ ] |
| Level: Warning | Select "Warning" filter | Shows only Warning and Error | [ ] |
| Level: Error | Select "Error" filter | Shows only Error entries | [ ] |
| Search filter | Type in search box | Only matching entries shown | [ ] |
| Clear search | Clear search box | All entries reappear | [ ] |
| Auto-scroll | Generate new log entries | View scrolls to show latest | [ ] |
| Clear logs | Click Clear button | Log viewer empties | [ ] |
| Open Logs folder | Click Open Logs button | File explorer opens log directory | [ ] |
| Resize panel | Drag panel edge | Panel resizes correctly | [ ] |

---

## J. Error Handling

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| Invalid file import | Import corrupt/invalid file | Error message, no crash | [ ] |
| Missing FFmpeg | Remove FFmpeg, try export | Helpful error about missing FFmpeg | [ ] |
| Corrupt project file | Open malformed .vsproj | Error message, no crash | [ ] |
| Missing project media | Open project with deleted media | Warning about missing files | [ ] |
| Encoding failure | Cause encode error (full disk, etc.) | Error shown in logs, no crash | [ ] |
| Plugin load failure | Test with missing plugin DLL | Warning in logs, app continues | [ ] |
| Out of memory | Open very large file | Graceful handling or error message | [ ] |
| Network path unavailable | Reference file on disconnected drive | Error message, no hang | [ ] |

---

## K. Keyboard Shortcuts

| Shortcut | Action | Test Steps | Pass |
|----------|--------|------------|------|
| Ctrl+N | New Project | Press Ctrl+N | [ ] |
| Ctrl+O | Open Project | Press Ctrl+O | [ ] |
| Ctrl+S | Save Project | Press Ctrl+S | [ ] |
| Ctrl+Shift+S | Save As | Press Ctrl+Shift+S | [ ] |
| Ctrl+Z | Undo | Make change, press Ctrl+Z | [ ] |
| Ctrl+Y | Redo | Undo, then press Ctrl+Y | [ ] |
| Space | Play/Pause | Press Space in preview | [ ] |
| Left Arrow | Previous Frame | Press Left in preview | [ ] |
| Right Arrow | Next Frame | Press Right in preview | [ ] |
| Home | Go to Start | Press Home | [ ] |
| End | Go to End | Press End | [ ] |
| Delete | Delete Selection | Select item, press Delete | [ ] |
| Escape | Cancel/Exit | Press during operation | [ ] |

---

## L. Window Management

| Test | Steps | Expected Result | Pass |
|------|-------|-----------------|------|
| Minimize | Click minimize button | Window minimizes to taskbar | [ ] |
| Maximize | Click maximize button | Window fills screen | [ ] |
| Restore | Click restore button | Window returns to previous size | [ ] |
| Resize | Drag window edges | Window resizes correctly | [ ] |
| Remember size | Resize, close, reopen | Window opens at previous size | [ ] |
| Remember position | Move window, close, reopen | Window opens at previous position | [ ] |
| Multi-monitor | Move to second monitor | Window functions correctly | [ ] |
| DPI scaling | Test on high-DPI display | UI scales correctly, no blur | [ ] |

---

## Test Session Record

| Date | Tester | Version | Tests Passed | Tests Failed | Notes |
|------|--------|---------|--------------|--------------|-------|
| | | | / | | |
| | | | / | | |
| | | | / | | |

---

## Issue Tracking

| Issue # | Section | Test | Description | Status |
|---------|---------|------|-------------|--------|
| | | | | |
| | | | | |
| | | | | |

---

## Sign-off

- [ ] All critical tests pass
- [ ] No blocking issues found
- [ ] Application ready for release

**Tested by:** _________________________ **Date:** _____________

**Approved by:** _________________________ **Date:** _____________
