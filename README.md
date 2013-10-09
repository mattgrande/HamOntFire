# HamOntFire

## What is this?

This site pulls data from the Hamilton Fire Service's Twitter account, [@HFS_Incidents](https://twitter.com/HFS_Incidents). I thought it was great that HFS was providing this information, but I wanted to have a better idea where things were happening.

## Why isn't it broken?

HamOntFire was written when using the Twitter API version 1 was deprecated, but still live. Twitter has since fully removed API 1 in favour of 1.1. This means I'll have to get a site key and all that junk.

It's something I'm planning on doing; just not something I've done yet.

## Who made this?

Matt Grande is a software developer from Hamilton, Ontario. He works in C# and Ruby on Rails. He is overweight and has bad hair.

[Twitter](https://twitter.com/mattgrande) | [Blog](http://mattgrande.com/) | [LinkedIn](http://www.linkedin.com/in/mattgrande) | [StackOverflow](http://stackoverflow.com/users/74727/matt-grande) | [Email](mailto:matt.grande@gmail.com)

## What's the tech running it?

* The site is programmed in C#
* The web framework is ASP.Net MVC 3.0
* Data is stored in [RavenDB](http://ravendb.net/) hosted by [RavenHQ](https://ravenhq.com/)
* Hosting is provided by [AppHarbor](https://appharbor.com/)
* The tweets are pulled using the [Twitter REST API](https://dev.twitter.com/docs/api/1/get/statuses/user_timeline). Yes, I'm still using Version 1. Yes, I know it's deprecated. Yes, I'm planning on jumping to Version 1.1. No, I'm not looking forward to it.
* Geocoding & maps are provided by the [Google Maps Places API](https://developers.google.com/maps/location-based-apps).
* Real time updating is provided by [SignalR](http://signalr.net/).
* Graphs are provided by [HighchartsJS](http://www.highcharts.com/).
* Special thanks to Dylan Vester for his [excellent article](http://dylanvester.com/post/Creating-Heat-Maps-with-NET-20-(C-Sharp).aspx) on creating heat maps with .Net.

## How can I help?

Do you have a suggestion for a feature? Stumble across a bug? Either submit a bug, or fix it yourself! Download the code from [GitHub](https://bitbucket.org/mattgrande/hamontfire).
