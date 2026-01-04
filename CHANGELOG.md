# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

No unreleased changes yet.

## [0.1.9] - 2026-01-04

### Fixes
- Translocator layer ([7244a61](https://github.com/mja00/VS-LiveMap-Revival/commit/7244a61)) #43

### Refactor
- Remove some redundant reflections ([2c7ada4](https://github.com/mja00/VS-LiveMap-Revival/commit/2c7ada4))

## [0.1.8] - 2026-01-03

### Features
- Add support for Cartographer's shared waypoint layer ([860c747](https://github.com/mja00/VS-LiveMap-Revival/commit/860c747)) #41
- Show mod version in the frontend ([f28316e](https://github.com/mja00/VS-LiveMap-Revival/commit/f28316e))
- Add basic webserver cache ([f715a7f](https://github.com/mja00/VS-LiveMap-Revival/commit/f715a7f)) #39

### Refactor
- Extract from LiveMap.cs ([3b96c29](https://github.com/mja00/VS-LiveMap-Revival/commit/3b96c29)) #40

### Chores
- Bump to 0.1.8 ([6ca6dbd](https://github.com/mja00/VS-LiveMap-Revival/commit/6ca6dbd))

## [0.1.7] - 2026-01-03

### Features
- Run fullrender on colormap received ([143f26e](https://github.com/mja00/VS-LiveMap-Revival/commit/143f26e))

### Fixes
- Use kebab case where appropriate ([49b445e](https://github.com/mja00/VS-LiveMap-Revival/commit/49b445e))
- Don't throw exceptions when stopping ([143f26e](https://github.com/mja00/VS-LiveMap-Revival/commit/143f26e))
- Resolve critical bugs and refactor magic numbers ([1f313d5](https://github.com/mja00/VS-LiveMap-Revival/commit/1f313d5)) #37

### Performance
- Comprehensive performance optimizations ([a82c73b](https://github.com/mja00/VS-LiveMap-Revival/commit/a82c73b)) #38

## [0.1.6] - 2026-01-02

### Features
- Better translation support ([c40fb92](https://github.com/mja00/VS-LiveMap-Revival/commit/c40fb92)) #35

### Fixes
- Attempt to improve color accuracy ([29c260c](https://github.com/mja00/VS-LiveMap-Revival/commit/29c260c)) #36
- Added using statement to SqliteCommand in ChunkLoader ([d7f1508](https://github.com/mja00/VS-LiveMap-Revival/commit/d7f1508)) #34

### Chores
- Run ReSharper ([69af792](https://github.com/mja00/VS-LiveMap-Revival/commit/69af792))

## [0.1.5] - 2026-01-02

### Features
- Add renderer buttons ([a5e8f5c](https://github.com/mja00/VS-LiveMap-Revival/commit/a5e8f5c)) #33

## [0.1.4] - 2026-01-02

### Features
- Add code quality checks ([a426e66](https://github.com/mja00/VS-LiveMap-Revival/commit/a426e66)) #32

### Fixes
- Chunk colormap on send ([f6530e8](https://github.com/mja00/VS-LiveMap-Revival/commit/f6530e8)) #31
- Materialize map positions to avoid repeated DB queries ([4b16f7d](https://github.com/mja00/VS-LiveMap-Revival/commit/4b16f7d)) #29

## [0.1.3] - 2026-01-01

### Fixes
- Queueing chunks up on load ([3c11d36](https://github.com/mja00/VS-LiveMap-Revival/commit/3c11d36)) #28

## [0.1.2] - 2026-01-01

### Features
- Boilerplate test framework ([9813f3c](https://github.com/mja00/VS-LiveMap-Revival/commit/9813f3c)) #25

### Fixes
- Update links ([f821477](https://github.com/mja00/VS-LiveMap-Revival/commit/f821477)) #27

## [0.1.1] - 2026-01-01

### Refactor
- Swap to genHTTP ([a792160](https://github.com/mja00/VS-LiveMap-Revival/commit/a792160)) #23

### Chores
- Add eslint ([924e916](https://github.com/mja00/VS-LiveMap-Revival/commit/924e916)) #22
- Add code review reports ([61685f6](https://github.com/mja00/VS-LiveMap-Revival/commit/61685f6))

## [0.1.0] - 2025-12-31

### Features
- Initial release of VS-LiveMap-Revival

### Fixes
- Tile rendering ([b8e8626](https://github.com/mja00/VS-LiveMap-Revival/commit/b8e8626))
- Return sidebar ([0a2b56c](https://github.com/mja00/VS-LiveMap-Revival/commit/0a2b56c))
- Download link ([8cd87fe](https://github.com/mja00/VS-LiveMap-Revival/commit/8cd87fe))

### Chores
- Add more gitignores ([5582a95](https://github.com/mja00/VS-LiveMap-Revival/commit/5582a95))
- Get running webserver ([7eb9311](https://github.com/mja00/VS-LiveMap-Revival/commit/7eb9311))
- Update readme ([72d10b4](https://github.com/mja00/VS-LiveMap-Revival/commit/72d10b4))
- Update demo map ([c009c52](https://github.com/mja00/VS-LiveMap-Revival/commit/c009c52))
- Enable doc building ([ffe3eef](https://github.com/mja00/VS-LiveMap-Revival/commit/ffe3eef))
- Allow manual dispatch ([4dc8d4a](https://github.com/mja00/VS-LiveMap-Revival/commit/4dc8d4a))
- Upgrade doxygen ([73c5375](https://github.com/mja00/VS-LiveMap-Revival/commit/73c5375))
- Update readme ([1fc2ff3](https://github.com/mja00/VS-LiveMap-Revival/commit/1fc2ff3))

## [0.0.13] - 2024-12-24

### Fixes
- ArgumentOutOfRangeException with sepia colors ([5257dec](https://github.com/mja00/VS-LiveMap-Revival/commit/5257dec))

## [0.0.12] - 2024-12-24

### Features
- Add friendly URLs setting to config file ([9a055c9](https://github.com/mja00/VS-LiveMap-Revival/commit/9a055c9))

## Pre-0.1.0 - Development History

### 2024-12-18
- Update to Vintage Story 1.20.0-rc.X ([a20bf47](https://github.com/mja00/VS-LiveMap-Revival/commit/a20bf47))

### 2024-07-11
- Add wiki link for httpd access denied error ([72dbf23](https://github.com/mja00/VS-LiveMap-Revival/commit/72dbf23))

### 2024-07-06
- Cleanup csproj ([3c33c6a](https://github.com/mja00/VS-LiveMap-Revival/commit/3c33c6a))

### 2024-07-04
- Summer colors ([eb62919](https://github.com/mja00/VS-LiveMap-Revival/commit/eb62919))

### 2024-06-21
- Stop trader markers from multiplying since they walk around ([dd33b22](https://github.com/mja00/VS-LiveMap-Revival/commit/dd33b22))

### 2024-06-20
- Fix build ([636aa26](https://github.com/mja00/VS-LiveMap-Revival/commit/636aa26))
- Change logger messages to debug level ([9226d11](https://github.com/mja00/VS-LiveMap-Revival/commit/9226d11))

### 2024-06-16
- Include debug symbols in build ([a21f8fd](https://github.com/mja00/VS-LiveMap-Revival/commit/a21f8fd))

### 2024-06-11
- Tweak cluster radius ([385a4bf](https://github.com/mja00/VS-LiveMap-Revival/commit/385a4bf))
- Cluster markers ([4af5c26](https://github.com/mja00/VS-LiveMap-Revival/commit/4af5c26))

### 2024-06-10
- Fix some stuffs ([9a6ae1f](https://github.com/mja00/VS-LiveMap-Revival/commit/9a6ae1f))
- Make traders work finally ([120c5b7](https://github.com/mja00/VS-LiveMap-Revival/commit/120c5b7))

### 2024-06-07
- Implement account entitlement colors ([476d1b3](https://github.com/mja00/VS-LiveMap-Revival/commit/476d1b3))

### 2024-06-02
- Remove unnecessary annotations ([4786a1b](https://github.com/mja00/VS-LiveMap-Revival/commit/4786a1b))
- Do not render incomplete chunks ([777b8d8](https://github.com/mja00/VS-LiveMap-Revival/commit/777b8d8))

### 2024-06-01
- More work on structure layers ([7c2dbf2](https://github.com/mja00/VS-LiveMap-Revival/commit/7c2dbf2))
- Some progress on config and structure layers ([0a0a5d5](https://github.com/mja00/VS-LiveMap-Revival/commit/0a0a5d5))
- Cleanup chunkloader code ([3d10f2e](https://github.com/mja00/VS-LiveMap-Revival/commit/3d10f2e))

### 2024-05-31
- Don't fade/blink tile images ([de6be86](https://github.com/mja00/VS-LiveMap-Revival/commit/de6be86))
- Work on markers some more ([03efc40](https://github.com/mja00/VS-LiveMap-Revival/commit/03efc40))

### 2024-05-30
- Default players and spawn marker layers to enabled true ([9b89fea](https://github.com/mja00/VS-LiveMap-Revival/commit/9b89fea))
- Fix build ([050d977](https://github.com/mja00/VS-LiveMap-Revival/commit/050d977))
- Add more player marker options ([74ea382](https://github.com/mja00/VS-LiveMap-Revival/commit/74ea382))
- Only poll layers if previous poll is complete ([c2d13cf](https://github.com/mja00/VS-LiveMap-Revival/commit/c2d13cf))

### 2024-05-28
- Fix ugly white borders on slow loading tiles ([baaf6e9](https://github.com/mja00/VS-LiveMap-Revival/commit/baaf6e9))
- Configurable URL in command ([f73fa32](https://github.com/mja00/VS-LiveMap-Revival/commit/f73fa32))
- Finish the apothemrender command ([f6f4fec](https://github.com/mja00/VS-LiveMap-Revival/commit/f6f4fec))

### 2024-05-27
- Finish refactor ([633bc72](https://github.com/mja00/VS-LiveMap-Revival/commit/633bc72))
- Move logo SVG into index.html ([7288f14](https://github.com/mja00/VS-LiveMap-Revival/commit/7288f14))
- Fix logo spacing issue in sidebar ([6f4d0f3](https://github.com/mja00/VS-LiveMap-Revival/commit/6f4d0f3))
- Remove all data files (part 2) ([f2e2950](https://github.com/mja00/VS-LiveMap-Revival/commit/f2e2950))
- Remove all data files ([cac40c3](https://github.com/mja00/VS-LiveMap-Revival/commit/cac40c3))
- Refactor ([4b0dd59](https://github.com/mja00/VS-LiveMap-Revival/commit/4b0dd59))

### 2024-05-26
- Refactor ([4b0dd59](https://github.com/mja00/VS-LiveMap-Revival/commit/4b0dd59))

### 2024-05-23
- Fail cleanly ([8608cf1](https://github.com/mja00/VS-LiveMap-Revival/commit/8608cf1))
- Save config changes ([9bf616d](https://github.com/mja00/VS-LiveMap-Revival/commit/9bf616d))

### 2024-05-21
- Cleanup client stuff to prepare for what's next ([6f63c4f](https://github.com/mja00/VS-LiveMap-Revival/commit/6f63c4f))
- Cleanup some statics ([decb46d](https://github.com/mja00/VS-LiveMap-Revival/commit/decb46d))
- Customizable logo stuff ([006550b](https://github.com/mja00/VS-LiveMap-Revival/commit/006550b))
- Fix up the tasks/threads ([b93a340](https://github.com/mja00/VS-LiveMap-Revival/commit/b93a340))

### 2024-05-19
- Yeah, that looks good ([c5a8ad7](https://github.com/mja00/VS-LiveMap-Revival/commit/c5a8ad7))
- Add more opengraph images ([3e30a65](https://github.com/mja00/VS-LiveMap-Revival/commit/3e30a65))

### 2024-05-13
- Some more progress ([245b04a](https://github.com/mja00/VS-LiveMap-Revival/commit/245b04a))
- z-index ([d46e0be](https://github.com/mja00/VS-LiveMap-Revival/commit/d46e0be))

### 2024-05-11
- More progress ([78f238a](https://github.com/mja00/VS-LiveMap-Revival/commit/78f238a))

### 2024-05-09
- Fix player rotation ([23fc78a](https://github.com/mja00/VS-LiveMap-Revival/commit/23fc78a))
- Colormap tweaks ([e422a1f](https://github.com/mja00/VS-LiveMap-Revival/commit/e422a1f))

### 2024-05-08
- Fix players with non-alphanumeric UIDs ([d741f5d](https://github.com/mja00/VS-LiveMap-Revival/commit/d741f5d))

### 2024-05-07
- Get rid of layers.png file ([87e2e82](https://github.com/mja00/VS-LiveMap-Revival/commit/87e2e82))
- Push up progress ([55c8e36](https://github.com/mja00/VS-LiveMap-Revival/commit/55c8e36))

### 2024-05-05
- Separate title and logo, and make logo a link ([a18db16](https://github.com/mja00/VS-LiveMap-Revival/commit/a18db16))
- More progress ([3be520d](https://github.com/mja00/VS-LiveMap-Revival/commit/3be520d))

### 2024-05-04
- Progress ([ed396dc](https://github.com/mja00/VS-LiveMap-Revival/commit/ed396dc))
- Dark/light themed favicon.ico ([e61b9f2](https://github.com/mja00/VS-LiveMap-Revival/commit/e61b9f2))
- Tweak friendly URL stuff ([ec02a3c](https://github.com/mja00/VS-LiveMap-Revival/commit/ec02a3c))
- Share page title with sidebar logo ([eba4c64](https://github.com/mja00/VS-LiveMap-Revival/commit/eba4c64))
- Tweak sidebar transitions timing ([8e21629](https://github.com/mja00/VS-LiveMap-Revival/commit/8e21629))
- Push up some progress ([9569950](https://github.com/mja00/VS-LiveMap-Revival/commit/9569950))

### 2024-05-03
- Fix index.html's meta tags ([419ff64](https://github.com/mja00/VS-LiveMap-Revival/commit/419ff64))
- Allow logo.svg to be replaced ([2fab85d](https://github.com/mja00/VS-LiveMap-Revival/commit/2fab85d))
- Push up some progress ([068f84e](https://github.com/mja00/VS-LiveMap-Revival/commit/068f84e))

### 2024-05-02
- Rename medieval to sepia ([6efa02c](https://github.com/mja00/VS-LiveMap-Revival/commit/6efa02c))
- Cleanup some stuff ([53634b8](https://github.com/mja00/VS-LiveMap-Revival/commit/53634b8))
- Fix white lines in map on chunk slice edges ([a1d807e](https://github.com/mja00/VS-LiveMap-Revival/commit/a1d807e))
- Fix config packet serialization ([1e3fc49](https://github.com/mja00/VS-LiveMap-Revival/commit/1e3fc49))
- Push up progress ([ead11f4](https://github.com/mja00/VS-LiveMap-Revival/commit/ead11f4))

### 2024-05-01
- Version bump ([1b20393](https://github.com/mja00/VS-LiveMap-Revival/commit/1b20393))
- Refactor ([78553c8](https://github.com/mja00/VS-LiveMap-Revival/commit/78553c8))
- Update meta card ([fcf97b4](https://github.com/mja00/VS-LiveMap-Revival/commit/fcf97b4))
- Scan once, render twice ([c3c7865](https://github.com/mja00/VS-LiveMap-Revival/commit/c3c7865))
- More progress ([c8165fc](https://github.com/mja00/VS-LiveMap-Revival/commit/c8165fc))
- Push up progress ([a5f0e4d](https://github.com/mja00/VS-LiveMap-Revival/commit/a5f0e4d))

### 2024-04-30
- Upload some progress ([ff212c5](https://github.com/mja00/VS-LiveMap-Revival/commit/ff212c5))

### 2024-04-28
- Put comments on halfslider element ([c6644e3](https://github.com/mja00/VS-LiveMap-Revival/commit/c6644e3))
- Move stuff around ([849e4ba](https://github.com/mja00/VS-LiveMap-Revival/commit/849e4ba))
- More progress ([f9768f0](https://github.com/mja00/VS-LiveMap-Revival/commit/f9768f0))
- Use more dependency injection ([2d847a9](https://github.com/mja00/VS-LiveMap-Revival/commit/2d847a9))
- Push up some more progress ([4b5333b](https://github.com/mja00/VS-LiveMap-Revival/commit/4b5333b))

### 2024-04-27
- Remove the rest of Lang so it compiles ([71a5581](https://github.com/mja00/VS-LiveMap-Revival/commit/71a5581))
- Progress ([0f5a3f7](https://github.com/mja00/VS-LiveMap-Revival/commit/0f5a3f7))
- Stop browsers from logging 404 errors in developer console ([30c9479](https://github.com/mja00/VS-LiveMap-Revival/commit/30c9479))

### 2024-04-23
- Refactor namespaces - convention was too restricting ([2fcdf79](https://github.com/mja00/VS-LiveMap-Revival/commit/2fcdf79))

### 2024-04-22
- Use only URLs nuget trusts for images ([a1458c7](https://github.com/mja00/VS-LiveMap-Revival/commit/a1458c7))
- Update badges and have tokei ignore .conf files ([111d6e8](https://github.com/mja00/VS-LiveMap-Revival/commit/111d6e8))
- Fix some badges in README.md ([0c7e47e](https://github.com/mja00/VS-LiveMap-Revival/commit/0c7e47e))
- Work on README.md some more ([1504dd1](https://github.com/mja00/VS-LiveMap-Revival/commit/1504dd1))

### 2024-04-21
- Version bump ([b0c2289](https://github.com/mja00/VS-LiveMap-Revival/commit/b0c2289))
- Hide dependencies from nuget ([1490b83](https://github.com/mja00/VS-LiveMap-Revival/commit/1490b83))
- Update modicon.png ([3603b67](https://github.com/mja00/VS-LiveMap-Revival/commit/3603b67))
- Set version earlier in build process ([922e0ea](https://github.com/mja00/VS-LiveMap-Revival/commit/922e0ea))
- Update README.md ([88c8d99](https://github.com/mja00/VS-LiveMap-Revival/commit/88c8d99))
- Remove debug stuff ([0678bdd](https://github.com/mja00/VS-LiveMap-Revival/commit/0678bdd))
- Fix version ([6c109ae](https://github.com/mja00/VS-LiveMap-Revival/commit/6c109ae))
- Include version in filename ([c9d9de6](https://github.com/mja00/VS-LiveMap-Revival/commit/c9d9de6))
- Progress ([1d7a09d](https://github.com/mja00/VS-LiveMap-Revival/commit/1d7a09d))

### 2024-04-20
- Cleanup JSON deserializing error handling ([892eeae](https://github.com/mja00/VS-LiveMap-Revival/commit/892eeae))
- Save points as floored ints in JSON ([61fdeda](https://github.com/mja00/VS-LiveMap-Revival/commit/61fdeda))
- Cleanup imports ([60fe799](https://github.com/mja00/VS-LiveMap-Revival/commit/60fe799))
- Update README.md ([9f21065](https://github.com/mja00/VS-LiveMap-Revival/commit/9f21065))
- Move doxygen to gh actions ([2396df9](https://github.com/mja00/VS-LiveMap-Revival/commit/2396df9))
- Make Point a struct ([0fb728d](https://github.com/mja00/VS-LiveMap-Revival/commit/0fb728d))
- Remove patronizing statement from docs ([fc56085](https://github.com/mja00/VS-LiveMap-Revival/commit/fc56085))
- Color struct ([ccece58](https://github.com/mja00/VS-LiveMap-Revival/commit/ccece58))
- Get rid of useless JsonSerializable interface ([ae90551](https://github.com/mja00/VS-LiveMap-Revival/commit/ae90551))
- Rename doxygen config ([1a6b3f8](https://github.com/mja00/VS-LiveMap-Revival/commit/1a6b3f8))
- Remove broken rules ([ab10b15](https://github.com/mja00/VS-LiveMap-Revival/commit/ab10b15))
- More API stuffs ([27634b6](https://github.com/mja00/VS-LiveMap-Revival/commit/27634b6))

### 2024-04-18
- C# Point API ([a64a6c7](https://github.com/mja00/VS-LiveMap-Revival/commit/a64a6c7))
- Location to Point ([1d2616e](https://github.com/mja00/VS-LiveMap-Revival/commit/1d2616e))

### 2024-04-17
- Progress ([e5e610b](https://github.com/mja00/VS-LiveMap-Revival/commit/e5e610b))
- Time to upload some progress ([cb8baab](https://github.com/mja00/VS-LiveMap-Revival/commit/cb8baab))

### 2024-04-16
- Fix double mousewheel zoom on Chrome-based browsers on Linux ([5d9ad23](https://github.com/mja00/VS-LiveMap-Revival/commit/5d9ad23))
- PNG to SVG ([b029200](https://github.com/mja00/VS-LiveMap-Revival/commit/b029200))
- Update README.md ([34e97a7](https://github.com/mja00/VS-LiveMap-Revival/commit/34e97a7))

### 2024-04-15
- Progress ([325f970](https://github.com/mja00/VS-LiveMap-Revival/commit/325f970))

### 2024-04-14
- Progress ([d099992](https://github.com/mja00/VS-LiveMap-Revival/commit/d099992))

### 2024-04-13
- Fixup location stuff ([ad8cf51](https://github.com/mja00/VS-LiveMap-Revival/commit/ad8cf51))
- Simplify regex and parse URL params better ([df48c29](https://github.com/mja00/VS-LiveMap-Revival/commit/df48c29))
- Context menu progress ([bcb7078](https://github.com/mja00/VS-LiveMap-Revival/commit/bcb7078))

### 2024-04-12
- Allow use of inline SVGs in marker icons ([8b689c1](https://github.com/mja00/VS-LiveMap-Revival/commit/8b689c1))
- Stick to relative paths ([6064f50](https://github.com/mja00/VS-LiveMap-Revival/commit/6064f50))
- Progress ([5ef6ee3](https://github.com/mja00/VS-LiveMap-Revival/commit/5ef6ee3))

### 2024-04-11
- Handle point/latlng better ([254c959](https://github.com/mja00/VS-LiveMap-Revival/commit/254c959))

### 2024-04-10
- Handle point/latlng better ([254c959](https://github.com/mja00/VS-LiveMap-Revival/commit/254c959))
- .tokeignore ([c6eb4f8](https://github.com/mja00/VS-LiveMap-Revival/commit/c6eb4f8))
- README badges ([2636eee](https://github.com/mja00/VS-LiveMap-Revival/commit/2636eee))
- Default settings needed ([e797ed6](https://github.com/mja00/VS-LiveMap-Revival/commit/e797ed6))
- Version bump ([4c05cd0](https://github.com/mja00/VS-LiveMap-Revival/commit/4c05cd0))

### 2024-04-08
- More progress ([30f9c0d](https://github.com/mja00/VS-LiveMap-Revival/commit/30f9c0d))
- More context menu work ([6df1772](https://github.com/mja00/VS-LiveMap-Revival/commit/6df1772))

### 2024-04-07
- More work on context menu ([b347953](https://github.com/mja00/VS-LiveMap-Revival/commit/b347953))

### 2024-04-05
- More progress ([e6beb26](https://github.com/mja00/VS-LiveMap-Revival/commit/e6beb26))
- Do not refresh/reload page when clicking link button ([9dec907](https://github.com/mja00/VS-LiveMap-Revival/commit/9dec907))
- Prevent click event propagation on custom control boxes ([5429a71](https://github.com/mja00/VS-LiveMap-Revival/commit/5429a71))

### 2024-04-04
- Let to const ([6dab576](https://github.com/mja00/VS-LiveMap-Revival/commit/6dab576))
- Add and cleanup the game's worldmap icons ([f3260f1](https://github.com/mja00/VS-LiveMap-Revival/commit/f3260f1))
- Finish up the markers stuff ([9f99f2d](https://github.com/mja00/VS-LiveMap-Revival/commit/9f99f2d))
- Cleanup ColorMapCommand for codefactor.io ([4b2d4bf](https://github.com/mja00/VS-LiveMap-Revival/commit/4b2d4bf))
- A little cleaning up ([b58eb8c](https://github.com/mja00/VS-LiveMap-Revival/commit/b58eb8c))

### 2024-04-03
- Move some images around ([0ea59fc](https://github.com/mja00/VS-LiveMap-Revival/commit/0ea59fc))
- Make default layer interval 5 minutes ([0e2e991](https://github.com/mja00/VS-LiveMap-Revival/commit/0e2e991))
- Add marker layer options ([e03e417](https://github.com/mja00/VS-LiveMap-Revival/commit/e03e417))
- Remove left over debug message ([35c5c79](https://github.com/mja00/VS-LiveMap-Revival/commit/35c5c79))
- Progress ([3c33788](https://github.com/mja00/VS-LiveMap-Revival/commit/3c33788))
- More codefactor.io fixes ([bd2d69f](https://github.com/mja00/VS-LiveMap-Revival/commit/bd2d69f))
- More easy fixes ([0220642](https://github.com/mja00/VS-LiveMap-Revival/commit/0220642))
- Fix low hanging fruit for codefactor.io ([88797b2](https://github.com/mja00/VS-LiveMap-Revival/commit/88797b2))
- Move some files around ([b2e5a50](https://github.com/mja00/VS-LiveMap-Revival/commit/b2e5a50))
- Get markers working in the frontend ([13a5246](https://github.com/mja00/VS-LiveMap-Revival/commit/13a5246))

### 2024-04-02
- Better variable names for file streams ([2a9337c](https://github.com/mja00/VS-LiveMap-Revival/commit/2a9337c))
- Color micro optimization ([2dc1936](https://github.com/mja00/VS-LiveMap-Revival/commit/2dc1936))
- Full PNG compression (we don't need quality) ([ec219be](https://github.com/mja00/VS-LiveMap-Revival/commit/ec219be))
- Version bump ([bb20f80](https://github.com/mja00/VS-LiveMap-Revival/commit/bb20f80))
- Remove livemap.admin privilege ([9ad5f06](https://github.com/mja00/VS-LiveMap-Revival/commit/9ad5f06))
- Auto reload config ([a7697ca](https://github.com/mja00/VS-LiveMap-Revival/commit/a7697ca))
- Tweak the zoom stuff ([8cf8ddf](https://github.com/mja00/VS-LiveMap-Revival/commit/8cf8ddf))
- Version bump ([6435bb5](https://github.com/mja00/VS-LiveMap-Revival/commit/6435bb5))

### 2024-04-01
- Process zoomed out tiles ([4433b6b](https://github.com/mja00/VS-LiveMap-Revival/commit/4433b6b))
- Don't compile test tiles ([de9fe9e](https://github.com/mja00/VS-LiveMap-Revival/commit/de9fe9e))
- Fix database reader ([7e30ace](https://github.com/mja00/VS-LiveMap-Revival/commit/7e30ace))
- Fix logger crap ([d5e6063](https://github.com/mja00/VS-LiveMap-Revival/commit/d5e6063))
- Cleanup stuff ([fd13b59](https://github.com/mja00/VS-LiveMap-Revival/commit/fd13b59))
- Move logger and config stuff around ([ab97ba1](https://github.com/mja00/VS-LiveMap-Revival/commit/ab97ba1))
- More progress ([192bb27](https://github.com/mja00/VS-LiveMap-Revival/commit/192bb27))
- Add tile layers ([65a8b05](https://github.com/mja00/VS-LiveMap-Revival/commit/65a8b05))
- Interrupt the render task ([7033f22](https://github.com/mja00/VS-LiveMap-Revival/commit/7033f22))

### 2024-03-31
- More progress ([708c6d0](https://github.com/mja00/VS-LiveMap-Revival/commit/708c6d0))
- Fix build ([477e3f7](https://github.com/mja00/VS-LiveMap-Revival/commit/477e3f7))
- Progress ([6baefe2](https://github.com/mja00/VS-LiveMap-Revival/commit/6baefe2))
- Switch to Jenkins ([eadb229](https://github.com/mja00/VS-LiveMap-Revival/commit/eadb229))
- Work on actions ([c7bb07e](https://github.com/mja00/VS-LiveMap-Revival/commit/c7bb07e))
- Progress ([008c4bf](https://github.com/mja00/VS-LiveMap-Revival/commit/008c4bf))
- TypeScript ([34b049f](https://github.com/mja00/VS-LiveMap-Revival/commit/34b049f))

### 2024-03-30
- Add leaflet extra files back ([f85d135](https://github.com/mja00/VS-LiveMap-Revival/commit/f85d135))
- Progress ([ac99606](https://github.com/mja00/VS-LiveMap-Revival/commit/ac99606))

### 2024-03-29
- Progress ([2200a0a](https://github.com/mja00/VS-LiveMap-Revival/commit/2200a0a))

### 2024-03-15
- Add basic social media meta tags ([f980e57](https://github.com/mja00/VS-LiveMap-Revival/commit/f980e57))
- Add funding.yml ([d024816](https://github.com/mja00/VS-LiveMap-Revival/commit/d024816))

### 2024-03-14
- Ready for pre-alpha testing ([637bfbc](https://github.com/mja00/VS-LiveMap-Revival/commit/637bfbc))
- Delete leaflet JS src files ([29a07f1](https://github.com/mja00/VS-LiveMap-Revival/commit/29a07f1))
- It's almost usable now ([37d6091](https://github.com/mja00/VS-LiveMap-Revival/commit/37d6091))

### 2024-03-13
- More progress ([5b0109a](https://github.com/mja00/VS-LiveMap-Revival/commit/5b0109a))
- Even more ([593f7c6](https://github.com/mja00/VS-LiveMap-Revival/commit/593f7c6))
- More progress ([64a6d4b](https://github.com/mja00/VS-LiveMap-Revival/commit/64a6d4b))
- Progress ([3813749](https://github.com/mja00/VS-LiveMap-Revival/commit/3813749))

### 2024-01-14
- Tidy up the project files ([a4699ef](https://github.com/mja00/VS-LiveMap-Revival/commit/a4699ef))

### 2024-01-12
- Slight style change ([96d30e9](https://github.com/mja00/VS-LiveMap-Revival/commit/96d30e9))

### 2024-01-11
- More progress ([52cb9ff](https://github.com/mja00/VS-LiveMap-Revival/commit/52cb9ff))
- Fix a couple bugs ([eee569c](https://github.com/mja00/VS-LiveMap-Revival/commit/eee569c))
- Progress ([78e038b](https://github.com/mja00/VS-LiveMap-Revival/commit/78e038b))

### 2024-01-10
- Slight progress ([7b3991d](https://github.com/mja00/VS-LiveMap-Revival/commit/7b3991d))
- More progress ([b92b1cc](https://github.com/mja00/VS-LiveMap-Revival/commit/b92b1cc))
- Getting closer ([b2c33e1](https://github.com/mja00/VS-LiveMap-Revival/commit/b2c33e1))

### 2024-01-09
- More progress ([b2a86c1](https://github.com/mja00/VS-LiveMap-Revival/commit/b2a86c1))
- Remove unneeded code ([c517c2f](https://github.com/mja00/VS-LiveMap-Revival/commit/c517c2f))
- Remove region class (replace with long index) ([a1b84db](https://github.com/mja00/VS-LiveMap-Revival/commit/a1b84db))
- Move logger and lang to util ([70fe20f](https://github.com/mja00/VS-LiveMap-Revival/commit/70fe20f))
- Even more progress ([728601b](https://github.com/mja00/VS-LiveMap-Revival/commit/728601b))

### 2024-01-08
- Some progress ([b691b8e](https://github.com/mja00/VS-LiveMap-Revival/commit/b691b8e))

### 2023-08-15
- Progress ([2d43894](https://github.com/mja00/VS-LiveMap-Revival/commit/2d43894))

### 2023-08-14
- Get colormap from admin's client ([fb641e4](https://github.com/mja00/VS-LiveMap-Revival/commit/fb641e4))
- Initial commit ([edaa5ae](https://github.com/mja00/VS-LiveMap-Revival/commit/edaa5ae))
