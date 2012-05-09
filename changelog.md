CHANGELOG
=========
0.8.1.0 - 9/5/12
----------------
- Instruments now play quieter when you hit them with less of a force.
- The Keyboard now has colour overlays to show which key you're pressing.
- Kinect Guide now has a strength scroll effect.
- The difference between fast and slow scrolling has been reduced.
- The zone where you are stationary in the Kinect Guide has been increased.

0.8.0.0 - 8/5/12
----------------
- The Primary Player feature has been removed.
	- Any player may now use any gesture. It's done on a "who shoots first" basis - ie if someone else is already selecting something, you can't
	- Voice commands now guess who said them based on the Kinect's microphone array
- There is now an electric guitar in Band Mode
- New players to Band Mode now have a random instrument assigned to them
- The Wall of Sound will now have less dead panels
- The metronome beat is now the sound of a kickdrum
- There are new UI sounds

0.7.1.0 - 6/5/12
----------------
- There are now help prompts for voice. Saying "Kinect Help Me" will bring up a list of commands you can say
- Added a triangle to Band Mode
- Lowered the confidence required for voice commands. May result in more false positives.
- Moto now stops listening after a voice command. Just say "Kinect" or "Moto" and it will start listening again if you require that.

0.7.0.0 - 5/5/12
----------------
- Tutorials have been added. They aim to give you basic help on how to play Moto.
	- To run Moto with tutorials enabled, create a shortcut to Moto and add " tutorials" to the end of the path. [See example](http://lockerz.com/s/206713785)
- The UI now has audial feedback on the microphone start/stop and the Kinect Guide ticks
- Wall of Sound should no longer ignore voice commands to change the angle of the Kinect
- If you select the metronome and leave Band Mode, then re-enter, it will no longer re-enter metronome setting mode

0.6.0.0 - 3/5/12
----------------
- Voice commands no longer require a visible microphone before working. To voice a command, say "Moto" or "Kinect", then a command and you will see the confirmation appear on screen as normal.
	- In a later version, saying "Moto/Kinect Help Me" will show a list of commands available to you at that time
- The photo upload number now is independent of the photo preview phase of the picture taking process. This allows asyncronous uploading of the photo to the website, catering for slower connections
- The photo upload notifcation will now appear for 5 seconds, and will not stay indefinitely if the upload speed is too slow
- You now cannot take more than one photo at a time
- Fixed the bug where guitar overlays would stay on screen when you left it
- The Wall of Sound no longer crashes if you use voice commands while not visible on screen
- Fixed a crash bug that occured when you went to use an instrument you had already used

0.5.2.9 - 2/5/12
----------------
(This update may require you to uninstall and [redownload Moto](http://mattcrouch.net/moto/download "Direct link to download file") manually)

- Band Mode now has a keyboard. Select it as you would from the menu or say 'Switch to Keyboard' while Moto is listening
- The guitar now has strings
- The Kinect Guide gesture now has reduced amounts of false positives
- Moto now has a newer, higher-definition icon.

0.5.2.5 - 30/4/12
-----------------
- Correct fatal error handling inside Moto. Visual feedback will instruct you on what action you need to take if it ever happens
	- There is also a new development keyboard shortcut - R - to restart Moto if these fatal errors don't correctly get handled and a manual restart is necessary
- Drum sounds are now more responsive
- Start screen gestures now favour the first gesture, and are now less glitchy
- Removed any references to the depth stream. Moto should now load faster and be more responsive
- Instruments in Band Mode and in Wall of Sound now do not show while the primary player controls the Kinect Guide
- There are now more media slots available within the Wall of Sound

0.5.2.4 - 29/4/12
-----------------
- Vertical scrolling menus now have nicer graphics on them
- Vertical scrolling menu now replaces the horizontal swipe menu within Band Mode
- Vertical scrolling menus now require less emphasised movements to scroll. Fast scrolling is now achieved anywhere past either your shoulder or bellybutton area for the respective direction
- Moto no longer crashes when issuing voice commands when no player is visible
- It is now easier to set a faster beat on the metronome

0.5.2.3 - 27/4/12
-----------------
- Audio confirmation is now in all parts of Moto
- You can now change the angle of the Kinect wherever you are within Moto
- Fixed an intermittent crash when confirming voice navigation actions

0.5.2.2 - 26/4/12
-----------------
- Removed audio glitches when sounds were interrupted
- All WoS panels now glow while they are playing

0.5.2.1 - 25/4/12
-----------------
- You can now record your own custom Wall of Sound by using the 'Record New' option in the Kinect Guide menu
	- To play a custom wall, select 'Custom Wall' in the Kinect Guide menu
- Added an 8-bit Wall of Sound
- Two players can each control their own wall content
- Graphics now appropriately layer on top of each other when taking a photo
- Improved the reliability of the 'Kinect' listening keyword
- The Kinect Guide gesture has now been slightly refined to avoid any false triggers.

0.5.2.0 - 22/4/12
-----------------
- The guitar has been repositioned to align just above the hip, as opposed to on it
- The guitar strumming action has been fixed and is now triggered by strumming in the centre of the guitar body
- All components of the drum kit now function
- Instruments in Band Mode and WoS now scale in Z
- The vertical scrolling menu now resets it's position after being re-triggered
- WoS is now 9 front panels, with two side panels
- Instruments now no longer show while the image stream is preparing to take, or is showing you, the picture

0.5.1.3 - 19/4/12
-----------------
- Fixed a crashing bug with high definition image capture within the Wall of Sound
- Improved responsiveness of the vertical scrolling menu
- Added placeholder labels to the vertical scrolling menu
- Swipe-to-select no longer needs to be as agressive
- You now cannot change the selected item accidently while you are swiping

0.5.1.2 - 17/4/12
-----------------
- Instruments are now translucent
- Wall of Sound vertical scrolling has updated visuals

0.5.1.1 - 16/4/12
-----------------
- Upped the minimum build requirement to 0.5.1.1
- Fixed a bug which would crash Moto if a photo was taken while offline

0.5.1.0 - 16/4/12
-----------------
- Start screen visuals have been updated
- 'Instruments' is now called 'Band Mode'
- Added basic graphic overlays to the guitar and drumset in Band Mode
- Added a basic horizontal scrolling menu to the Wall of Sound
- Higher definition images are now captured when Moto takes a picture
- Improved the accuracy of both modes when players are at the extreme edges of the play area

0.5.0.11 - 5/4/12
-----------------
- Changed download link to [MattCrouch.net/moto/download](http://www.mattcrouch.net/moto/download).
- Moto now automatically checks for updates as they become available

0.5.0.5 - 5/4/12
----------------

- Wall of Sound now has 8 (4x2) sound panels activated by the hands
- RGB now has darker edges
- Primary player changes are now indicated through a glow in that user's hands
- /Should/ install Kinect SDK as a prereq if not currently installed


0.5.0.0 - 4/4/12
----------------
- Initial upload to Github
- Added basic Wall of Sound mechanics