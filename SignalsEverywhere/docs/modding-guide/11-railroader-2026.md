# 11. Railroader 2026.1

Railroader 2026.1 (Maintenance of Way) has its own mod loader. Railloader, RailForge, FUSE and StrangeCustoms don't run on it, and the map editor can't make signals yet. SignalsEverywhere has a 2026.1 build that loads signal mods itself. The signal data is the same, so almost everything in this guide still applies. This page covers what's different.

## Getting SignalsEverywhere for 2026.1

The 2026.1 build is built from this repository with `dotnet build SignalsEverywhere -p:RR=2026`. Its mod identifier is `joo/signals-everywhere`.

It includes every fix listed under [Known problems](07-patching.md#known-problems) and the features of Tetz's SignalsEverywhere Fixes (map feature gates, panel scrolling). SignalsEverywhere Fixes is Legacy only; don't port it.

Players need two things:

- SignalsEverywhere is a DLL mod, so **Allow Unsafe DLL Mods** must be on (Preferences > Features > Experimental).
- SignalsEverywhere and your mod must both be turned on in the game's **Mods** menu. Restart the game after turning mods on: some mods don't load properly when they're turned on during a session.

## The mod files

A 2026.1 mod is a folder in the game's `Mods` folder (`Documents\Railroader\Mods` unless the game is started with another home folder) with an `Info.json`. Put the mixintos in the same file:

```json
{
  "identifier": "yourname/my-signals",
  "version": "1.0.0",
  "name": "My Signals",
  "author": "yourname",
  "description": "CTC at ...",
  "dependencies": {
    "required": [ "rr/game", "joo/signals-everywhere" ]
  },
  "mixintos": {
    "signals": [ "file(signals.json)" ],
    "ctcPanel": [ "file(ctc-panel.json)" ]
  }
}
```

- **`identifier`** is `<your name>/<mod>`, in lower case. Other mods name yours by it.
- **`dependencies.required`**: the game and SignalsEverywhere, plus any mod your signals patch or build on.
- **`mixintos`**: the same as in a Legacy `Definition.json`. A `Definition.json` with a `mixintos` object beside `Info.json` works too, so a Legacy mod can keep its file. SignalsEverywhere only reads mixintos from mods the game has loaded.
- Don't give the mod a `scope`. SignalsEverywhere is a content mod, which every player in a multiplayer game has, and signals must be the same for every player.

Mixinto lists are plain file names on 2026.1. A list entry with its own `requires` (an object instead of a string, see [The mod definition](01-getting-started.md#the-mod-definition)) is skipped. For a file that should only apply when another mod is installed, ship it as a second small mod that requires that mod.

### Load order

SignalsEverywhere applies mods in dependency order: a mod after every mod it lists under `dependencies.required` or `dependencies.optional` (and, in a `Definition.json`, `requires` and `loadAfter`), otherwise by identifier. So list every mod whose signals or panel you patch, the way you'd list it in `loadAfter`. A mod you only need when it's there goes under `optional`.

### Two mods with the same signals

If you publish both a combined mod and separate mods with the same signals, list the others under `dependencies.incompatible` in each, so the game won't run both. This replaces RailForge's `conflictsWith`.

## The map

2026.1's map isn't Legacy's. The main line is realigned at East Whittier, Dillsboro and Sylva, Walker has lost its switchbacks, and the stock signals were reworked with it. Don't assume a Legacy id or location still fits.

- **Track ids.** Many ids survived: segments and nodes Legacy had are often still there under the same id, but a lot of them have moved, some by tens of meters. New ids don't start with `S` or `N`; they're six random characters, like `qefeuz` or `7a3rv9`. Check every location against the 2026.1 track, and every switch node, even where the id is unchanged.
- **Where to find them.** The base map is `Railroader_Data\StreamingAssets\AssetPacksCore\nc-bryson-terrain\Data\Map.json` in the game folder. Its `graph` has every node (`position`) and every segment (its end nodes `a` and `b`). A location's `Start` is the segment's `a` end and `End` its `b` end. The 2026.1 dump (`/signaldebug dump signals`) gives every id the stock signals use.
- **Features and modules.** Dump `signal-old.json` on 2026.1 before you patch anything: module keys and which module holds a block changed. Sylva West's block `sy-w` is in module `SY-App`, for example, and Alarka Jct West already has its own module, `AJ-W`. A patch aimed at the wrong module doesn't fail cleanly: a `$replace` on something that isn't there is copied in raw, and then building the signals fails ("Error building signals").
- **Track changes.** There are no `game-graph` mixintos on 2026.1. Mods change the map with a map mod or a Relay `Data.rl` script (`modifyMap`). Signals on a map mod's track need that mod as a dependency, so its track exists before your signals are built.

### Porting a Legacy signal mod

1. Dump the 2026.1 signals and compare the modules you patch with the Legacy dump. Rewrite patches whose target moved or changed.
2. Move every location to the 2026.1 track. Where the segment still runs between the same two nodes and the location sits on a node, it can stay. Anything else needs a fresh look: find the point on the old track, then the nearest point on the new one, keeping the end it's measured from.
3. Check every switch node in `switchSets` and `Switch` predicates exists and is the switch you mean.
4. Write the `Info.json`, and move conditional mixintos into their own mods.
5. Test it as you would a new mod ([A test run](09-testing-and-debugging.md#a-test-run)).

## Without RailForge

RailForge did three things for signal mods. On 2026.1 nobody does them:

- **Untouched control points are deleted again.** Patch anything in a module with an interlocking, intermediate or crossover and don't touch that component, and it's deleted, as under plain Railloader. Always touch it (see [What gets rebuilt](07-patching.md#what-gets-rebuilt)).
- **Nothing checks your modules first.** There's no support report and no deferring. A bad id or location shows as an error in `Player.log`, and the rest works or fails without a word on screen.
- **No forward-reference check.** SignalsEverywhere indexes every existing control point before it builds, so a predicate signal may name a stock control point in a later module. A new control point should still come before the signals that name it.

## Your own signal milestone

On Legacy, a mod adds a milestone with a progression section in a `game-graph` file. On 2026.1, milestones are part of the game's scenes, out of reach of data files and Relay. SignalsEverywhere lets a signal mod add one with a `milestones.json` in its folder:

```json
{
  "mapFeatures": [
    {
      "identifier": "signals-alarka",
      "displayName": "Signals - Alarka Branch",
      "description": "CTC on the Alarka Branch from Alarka Jct to Cochran and Alarka.",
      "defaultEnableInSandbox": true
    }
  ],
  "milestones": [
    {
      "identifier": "yourname-sig-alarka",
      "progressions": [ "ewh" ],
      "title": "Signals: Alarka Branch",
      "description": "Extend signals up the Alarka Branch to Cochran and Alarka.",
      "cost": 2500,
      "prerequisites": [ "sig-hw-bk", "alarka-branch" ],
      "enableFeaturesOnComplete": [ "signals-alarka" ],
      "industryComponentId": "yourname-cochran-signal-site.site-signals",
      "deliveries": [ { "loadId": "rr/signal-parts", "carTypeFilter": "XM*", "count": 3 } ]
    }
  ]
}
```

- **`mapFeatures`**: new map features. `defaultEnableInSandbox` sets the sandbox default.
- **`milestones`**: delivery milestones like the game's own signal milestones. `progressions` (optional) limits one to those progressions; `prerequisites` are milestone ids that must be done first; `enableFeaturesOnComplete` the map features it unlocks; `deliveries` what the player ships to finish it.
- **`industryComponentId`**: the delivery site, `<industry>.<component>`. It must be a `ProgressionIndustryComponent` on the map, which your mod adds with a Relay `Data.rl`.

A milestone whose prerequisites, map features or delivery site the running game doesn't have (another start, another map) is left out.

The delivery site, added from `Scripts/Data.rl`. This one puts an industry with one component on the Cochran siding, and only on a map that has the Cochran area and its track:

```text
import json

func modifyMap(map) {
    map.modify((data) => {
        var areas = data["ops"]?["areas"]
        if areas == null || areas["cochran"] == null || data["graph"]?["segments"]?["Sls0"] == null {
            return
        }
        areas["cochran"]["industries"]["yourname-cochran-signal-site"] = json.load("cochran-signal-site.json")
    })
}
```

```json
{
  "name": "Cochran Signal Site",
  "position": [ 2050.0, 594.0, -265.0 ],
  "usesContract": false,
  "trackSpans": { "site-signals": [ "Sls0|a|59.014", "Sug6|a|1.968" ] },
  "components": {
    "site-signals": {
      "kind": "ProgressionIndustryComponent",
      "name": "Cochran Signal Site",
      "trackSpans": [ "site-signals" ],
      "carTypeFilter": "*"
    }
  }
}
```

Map data writes a location as `segment|end|distance`, with ends `a` and `b`.

Then tie your modules to the map feature with a `map-feature-gates.json` ([Tying modules to a milestone](02-how-ctc-works.md#tying-modules-to-a-milestone)). On 2026.1, SignalsEverywhere reads it itself. Use the line's own map feature (here `alarka`) as the `fallbackMapFeature`, so starts without your milestone still get the signals with the track.

## Tools on 2026.1

The game has no mod tabs, so SignalsEverywhere's buttons are console commands. Run them as the host:

| Command | Does |
|---|---|
| `/signaldebug dump signals` | Writes `signal-old.json` and `signal-patched.json`. |
| `/signaldebug dump panel` | Writes `panel-dump.json`. |
| `/signaldebug reload` | Re-reads every `signals` file and rebuilds (Reload Signal Definitions). |
| `/signaldebug rebuild-panel` | Re-reads every `ctcPanel` file and redraws the panel (Rebuild CTCPanel). |
| `/signaldebug show-panel` | Opens the CTC panel (also the N key). |

The files are written to SignalsEverywhere's own folder in `Mods`. There's no Blocks highlight: check a new block with a car and the panel light. SignalsEverywhere logs to `Player.log` (`%USERPROFILE%\AppData\LocalLow\Giraffe Lab LLC\Railroader\Player.log`).

## An example

[Tetz's Branch Junctions](https://www.nexusmods.com/railroader/mods/1809) has 2026.1 files: the combined mod and four separate ones that declare each other incompatible, patches to stock control points (Alarka Jct West, Bryson East), a new two-plant junction, and Alarka Branch Signals with its own milestone, delivery site and gate.
