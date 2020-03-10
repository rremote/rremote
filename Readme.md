RRemote - A software remote control for your Roku device or television

I wrote this application because Roku discontinued their officially supported
Windows Store app and I have this nifty touch screen setup next to my bed where
I read the news, etc., and often lose the physical remote. 

First, for the non technical people who stumble upon this project a brief
description of its capabilities and limitations. This program can control
almost every feature your Roku offers and automatically discovers all of the
available Rokus on your network. For this to go smoothly two things HAVE to
happen. A) Your Roku and the computer running this software MUST be on the
same subnet and B) your network must not block multicast. B is simple enough
as most home routers do not block multicast. A however is a little more
challenging. Some routers put Wifi and wired devices on different networks,
you can be connected to different Wifi networks, most home devices have their
IP address assigned via DHCP, and the list goes on. Keeping it short, for the
best chance of success one of these two rules should be
enforced:
	1) Your computer running this software and all the Roku devices you want to
	control need to be wired into the same network
	2) Your computer running this software and all the Roku devices you want to
	control need to be all wireless and connected to the EXACT same wireless
	network name (SSID).
Anything beyond one of those configurations and things might not work and is
beyond the scope of this document or what I'll provide help on. If you require
this kind of help find somone who understands networks. You can also add a 
Roku device manually to this program but be forewarned unless you explicitly
assigned your Roku's IP address (static IP) durring setup it might change 
someday, seemly at random, and you'll have to have to find out the new address
and add it again. That's just how DHCP networks work. A final hint is 
you may want to look into putting your Wifi system in bridge mode which will 
combine the wifi and wired networks. That's useful for fixing a lot of other
common communcation problems experienced by users of Sonos and a multitude
of other home entertainment/automation hardware.

Now for the more technical stuff.

This program works by:
1) Discovering all the Rokus on your network, recording their connection info
2) Periodically checking that those Rokus are still online and connectable
3) Allowing you to manually add a device by IP address
4) Allowing you to select a single Roku to control at a time and displaying
all of the channels installed on that device.

Discovery:
All Roku devices require an internet connection to work and each device has
an exposed set of commands you can use to control it. If you know the IP
address of your Roku you can use any internet browser to send those commands.
For instance if your device has an IP addres of 192.168.1.134 then typing 
this URL into your browser, http://192.168.1.134:8060/query/apps, will list all
currently installed channels. You can learn more about these commands here:
https://developer.roku.com/docs/developer-program/debugging/external-control-api.md

IP addresses are assigned either by DHCP, where a device broadcasts a request
to be assigned an address and the nearest router responds with one, or are
setup statically, i.e. you manually chose the address and configured the 
subnet, gateway, and/or DNS settings. DHCP addresses are only good for a short
time which in many cases is 7 days. After that your Roku (or any device using
DHCP) sends out a request for another assignment. Typically the router assigns
the same address as before but that's not guarenteed. Power outages, resetting
your router or having your Roku off for an extended period of time make it
more likely it will be assigned a different address. To combat this address
uncertianty Roku uses a multicast UDP protocol called SSDP (Simple Service 
Discovery Protocol) to allow you to find all the available devices on your 
network. https://en.wikipedia.org/wiki/Simple_Service_Discovery_Protocol

This program sends out a SSDP packet which all Roku devices on the same
network respond to with their connection details. Multicast by default only 
works inside of the subnet it originated from. For example if the computer
running this software is at 192.168.1.101 and the Roku is at 192.168.1.102
then SSDP will work fine. If your Roku is instead at 10.0.1.102 (because
many wifi networks are on different subnets than the wired components)
then SSDP will most likely fail. As long as there is not a firewall
blocking communication between these two subnets then you can always just
manually add the IP address of the Roku to this program and control it.

Every 15 minutes this program rediscovers all the available Rokus updating
the list of devices and removing ones not found. Manually added Rokus can
only be removed by clicking the delete button (a button at the top of the
program that shows a - symbol).

Once you select a Roku the list of apps (channels) installed on it are
downloaded and displayed.

Some of the buttons might not work for your particular Roku. The volume,
channel, mute, and input buttons mostly only work on TVs with embedded
Roku software such as the ones made by TCL. The power off button only
works on TVs connected via HDMI and the TV must support CEC or the branded
equivilant of CEC (for instance Samsung calls it AnyNet+). More info can be
found here https://www.howtogeek.com/207186/how-to-enable-hdmi-cec-on-your-tv-and-why-you-should/
Find My Remote only works with compatible Rokus and special Roku remotes.

The virtual keyboard functionality works just like the other buttons and is
UTF-8 compatible meaning it can send characters like the Euro symbol.

This program is written in C# for UWP apps using Visual Studio 2019 and follows
the MVVM design pattern. The Roku Client code is based off a different GitHub
project that I just can't seem to find again to give proper attribution and 
contains some improvements. It is designed to be compatible with most screen 
types, zoom levels, and and orientations. 

To be continued...
