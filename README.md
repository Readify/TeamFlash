# TeamFlash

TeamFlash is a super simple tool for connecting TeamCity to a Delcom USB build light.

It's tested with the [Delcom 904003](http://www.delcomproducts.com/productdetails.asp?productnum=904003) and [Delcom 804003](http://www.delcomproducts.com/productdetails.asp?productnum=804003) (old generation).

And now you can run it on your [Raspberry Pi](http://raspberrypi.org) with Raspbian "wheezy"!

## Installing

### On Windows
1. Grab [the compiled bits, hot off our build server](https://tc.readifycloud.com/viewLog.html?buildTypeId=bt10&buildId=lastSuccessful&tab=artifacts&guest=1)
2. Run them
3. There is no installer

### On RPi
1. Install mono and git by running **sudo apt-get install mono-complete git**
1. Clone [libdelcom](https://github.com/ducas/libdelcom.git) and install it
2. Clone TeamFlash and run xbuild
3. Run it...

## Outputs

<dl>
  <dt>Red</dt>
  <dd>one or more builds are broken, and not being investigated</dd>
  <dt>Amber</dt>
  <dd>all builds are either under investigation (requires TeamCity 7.1), or passing</dd>
  <dt>Green</dt>
  <dd>all builds are passing</dd>
</dl>

Paused builds are ignored.

## Code

…is moderately average. We know. We hacked it together on a client site eons ago, then published it when we wanted to re-use it elsewhere. Don't hate us: send a pull request instead.

We also know that there are other projects that do the same thing, like [SouthsideSoftware/BuildLight](https://github.com/SouthsideSoftware/BuildLight). We wrote this a while ago, and wanted to publish it anyway.
