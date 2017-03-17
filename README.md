# SteamTools
Various tools for Steam Groups

Installation :-

Download Publish/setup.exe and run, program will autoupdate when new builds are published.

Should the ClickOnce installer not work for you, or you are not interested in auto updates you can use the standalone installer in Output/setup.exe

Features :-

Process a Steam community group members list and generate a comparison of games and which users own them.  This list can be filtered to only show games which a selected list of users own and also by steam community tags on the games.  Games from this list can be selected to view a combined gallery of all screenshots for this game from members of the group.  Should you wish, an HTML version of the screen can be exported, exluding the screenshot gallery functionality.

Data is cached in JSON files to minimize the number of web requests performed (game tags require loading the store page for each game which takes a while if the group have many games!)

Attempting to scrape a large number of games in a short space of time can and will result in Steam returning HTTP Status 429 errors, meaning that you have made too many requests and must wait until more can be made, please note that this will also apply to the Steam client but is only tied to your IP, not your Steam account.

Steam groups with multiple pages of users are catered for, but the same warning as above applies.
