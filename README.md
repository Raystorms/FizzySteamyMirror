# FizzySteamyMirror

Fizzcube bringing together [Steam](https://store.steampowered.com/) and [Mirror](https://github.com/vis2k/Mirror)

This project was previously called **"SteamNetNetworkTransport"**, this is Version 2 it's a complete rebuild utilising Async of a Steam P2P network transport layer for [Mirror](https://github.com/vis2k/Mirror)

## Quick start - Demo Project 

(coming soon)

## Dependencies
Both of these projects need to be installed and working before you can use this transport.
1. [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET) FizzySteamyMirror relies on Steamworks.NET to communicate with the [Steamworks API](https://partner.steamgames.com/doc/sdk). **Requires .Net 4.x**
2. [Mirror](https://github.com/vis2k/Mirror) FizzySteamyMirror is also obviously dependant on Mirror which is a streamline, bug fixed, maintained version of UNET for Unity. **Recommended [Stable Version](https://assetstore.unity.com/packages/tools/network/mirror-129321)**

## Setting Up
1. Download [**FizzySteamyMirror.unitypackage**](https://github.com/Raystorms/FizzySteamyMirror/releases)
which also has Steamworks.Net included. **(if you already have Steamworks.Net in your project, you might need to delete either your import or the one included in the release)**

2. In your ![Image](http://i.galtrox.com/index.php/s/LX2KPkezLwazrTS/preview) object replace ![Image](http://i.galtrox.com/index.php/s/LTwTTyZLtbmGHY6/preview) with ![Image](http://i.galtrox.com/index.php/s/5PJBqPjJiFdqxG9/preview) 

**Note: if your new to Mirror and you dont know "NetworkManager" is all about we recommended going to   [Doc](https://vis2k.github.io/Mirror/General/Start)**

## Building
1. Go into your build and make a **"steam_appid.txt"** and place **480** in side.

![Image](http://i.galtrox.com/index.php/s/KLB8W6kFtnjwQPJ/preview)

2. When running the game make sure you have placed it into steam as a **Non-Steam Game**

**Note: The 480(Spacewar) appid is a very grey area, technically, it's not allowed but they don't really do anything about it.
If you know a better way around this please make a [Issue ticket.](https://github.com/FizzCube/FizzySteamyMirror/issues)**

**Note: When you have a appid from steam then replace the 480 with your own game appid.**

## Host
1. Open your game through Steam
2. Host your game ![Image](http://i.galtrox.com/index.php/s/ycNEwXKf5jdYD8T/preview)
3. if it says your playing **"Spacewar"** in Steam **"congrats its working!"**

**Note: You can run it in Unity aswell**

## Client
1. Send the game to your buddy.
2. The client needs the steam64id of the host to be able to connect.
3. Place the steam64id into ![Image](http://i.galtrox.com/index.php/s/py8ZgqtkbrzyC3B/preview) then click "Lan Client"
4. **Bing bash bong DONE!**
