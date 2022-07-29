## Project Earth Api
*The core API for Project Earth*

## **WARNING: This thing is very early work in the progress, so not everything might work as expected**

## What does this component do?
The core API handles the bulk of game functionality - pretty much everything that isn't direct AR gameplay is done here.

## Building
 - Place the config in the same folder as whereever your built executable is going to end up
 - Open the sln in your IDE of choice
 - Build & run

## Setting up the Project Earth server infrastructure.

### Getting all the parts

to start, ensure that you have built copies of all the required components downloaded:
- A built copy of the Api (you are in this repo), which you can fetch from [our jenkins](https://ci.rtm516.co.uk/job/ProjectEarth/job/Api/job/master/)
- Our [ApiData](https://github.com/Project-Earth-Team/ApiData) repo, or your own data. In addition, you'll need the Minecraft Earth resource pack file, renamed to `vanilla.zip` and placed in the `resourcepacks` subfolder of the ApiData repo. You can procure the resourcepack from [here](https://cdn.mceserv.net/availableresourcepack/resourcepacks/dba38e59-091a-4826-b76a-a08d7de5a9e2-1301b0c257a311678123b9e7325d0d6c61db3c35), provided you're setting up before June 30th, 2021. Rename your clone to `data`, and place it next to your Api executable.
- Our fork of [Cloudburst](https://github.com/Project-Earth-Team/Server). Builds of this can be found [here](https://ci.rtm516.co.uk/job/ProjectEarth/job/Server/job/earth-inventory/). This jar can be located elsewhere from the Api things.
- Run Cloudburst once to generate the file structure.
- In the plugins folder, you'll need [GenoaPlugin](https://github.com/Project-Earth-Team/GenoaPlugin), and [GenoaAllocatorPlugin](https://github.com/Project-Earth-Team/GenoaAllocatorPlugin). The CI for this can be found [here](https://ci.rtm516.co.uk/job/ProjectEarth/job/GenoaPlugin/job/master/) and [here](https://ci.rtm516.co.uk/job/ProjectEarth/job/GenoaAllocatorPlugin/job/main/). **Note: make sure to rename your GenoaAllocatorPlugin.jar to ZGenoaAllocatorPlugin.jar, or you will run into issues with class loading** 

### Setting up

On the cloudburst side:
- within the `plugins` folder, create a `GenoaAllocatorPlugin` folder, and in there, make a `key.txt` file containing a base64 encryption key. An example key is
 ```
/g1xCS33QYGC+F2s016WXaQWT8ICnzJvdqcVltNtWljrkCyjd5Ut4tvy2d/IgNga0uniZxv/t0hELdZmvx+cdA==
```
- edit the cloudburst.yml file, and chan ge the core api url to the url your Api will be accessible from
- on the Api side, go to `data/config/apiconfig.json`, and add the following:
```json
"multiplayerAuthKeys": {
        "Your cloudburst server IP here": "the same key you put in key.txt earlier"
 }
```
- Start up the Api
- Start up cloudburst. After a short while the Api should mention a server being connected.
- If you run into issues, retrace your steps, or [contact us on discord](https://discord.gg/Zf9aYZACU4)
- If everything works, your next challenge is to get Minecraft Earth to talk to your Api. If you're on Android, you can utilize [our patcher](https://github.com/Project-Earth-Team/PatcherApp). If you're on IOS, the only way to accomplish this without jailbreak is to utilize a DNS, such as bind9. Setup for that goes beyond the scope of this guide.


