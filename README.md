# SteamTools
Various tools for Steam Groups

Installation :-

Download Publish/setup.exe and run, program will autoupdate when new builds are published.

Features :-

Process a Steam community group members list and generate an HTML page displaying a list of games and which users own them.  This list can be filtered to only show games what a selected list of users own and also by steam community tags on the games.

Data is cached in JSON files to minimize the number of web requests performed (game tags require loading the store page for each game which takes a while if the group have many games!)

Attempting to scrape a large number of games in a short space of time can and will result in Steam returning HTTP Status 429 errors, meaning that you have made too many requests and must wait until more can be made, please note that this will also apply to the Steam client but is only tied to your IP, not your Steam account.

Steam groups with multiple pages of users are catered for, but the same warning as above applies.
