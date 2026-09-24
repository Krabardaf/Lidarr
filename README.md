# Lidarr (Performance & Release Preference Fork)

This fork contains targeted performance optimizations and intelligent release-selection enhancements for [Lidarr](https://github.com/Lidarr/Lidarr), built to solve real-world bottlenecks on medium-to-large music libraries.

---

## 🚀 Key Improvements in this Fork

### 1. API & UI Performance Optimization
* **Upstream Pull Request**: [#5838](https://github.com/Lidarr/Lidarr/pull/5838)
* **Branch**: [`perf/optimize-album-artist-stats`](https://github.com/Krabardaf/Lidarr/tree/perf/optimize-album-artist-stats)

#### The Problem
When requesting albums for a specific artist or subset of artists (e.g. `GET /api/v1/album?artistId=...`), `AlbumControllerWithSignalR.MapToResource()` previously called `_artistStatisticsService.ArtistStatistics()`.

In libraries with hundreds or thousands of artists:
- This executed unconstrained, full-database aggregations across all `Tracks`, `Albums`, `AlbumReleases`, and `TrackFiles`.
- It followed up with an in-memory $O(N \times M)$ nested matching loop in C# across the entire library.
- Although cached under `"AllArtists"`, this cache is frequently evicted across 10 different events (`AlbumUpdatedEvent`, `AlbumImportedEvent`, `ArtistUpdatedEvent`, `TrackFileDeletedEvent`, etc.). Every background import or metadata refresh cleared the cache, causing 1–3+ second UI lockups whenever viewing an artist's discography.

#### The Fix
`MapToResource` now inspects the distinct `ArtistId`s present in the album batch:
- **Batches with $\le 100$ artists**: Statistics are fetched individually by `ArtistId` (`_artistStatisticsService.ArtistStatistics(id)`). This utilizes Lidarr's existing fine-grained per-artist cache (`artistId.ToString()`) and fast indexed SQL query (`WHERE Artists.Id = @artistId`).
- **Full-library queries**: Gracefully falls back to bulk calculation.

---

### 2. Intelligent Release Selection & Monitoring
* **Branch**: [`feature/release-selection-preference`](https://github.com/Krabardaf/Lidarr/tree/feature/release-selection-preference)

#### The Problem
By default, Lidarr automatically selects and monitors the release with the most tracks (`MaxBy(x => x.TrackCount)`).

In practice, this frequently results in:
- Monitoring bloated 40-track "Super Deluxe 30th Anniversary" editions filled with rehearsal outtakes, interviews, and low-quality live voice memos, rather than the core studio album.
- Selecting obscure regional pressings or bootlegs with bonus tracks that fail to match standard release indexers (Soulseek, trackers, Usenet).

#### The Fix
Introduced `AlbumReleasePreferenceComparer`, an intelligent, multi-tier release selection engine applied during metadata import (`SkyHookProxy`) and album refresh (`RefreshAlbumService`):

1. **Existing File Preservation**: Releases that already have downloaded files on disk are never unmonitored.
2. **Medium Format Hierarchy**:
   - **Digital Media** (`Digital Media`, `Digital` — primary modern distribution with clean metadata)
   - **Standard CD** (`CD`, `Enhanced CD`, `CD-R` — standard audio disc)
   - **Vinyl** (`Vinyl`, `12" Vinyl`, `7" Vinyl`, `10" Vinyl`, `LP`)
   - **Cassette** (`Cassette`)
   - **Exotic / Niche Audiophile** (`SACD`, `SHM-CD`, `DualDisc`, `DVD-Audio`, etc., deprioritized to avoid rare collector editions)
3. **Clean Core Edition Filter**:
   - Releases with bloat keywords in their title or disambiguation (`deluxe`, `anniversary`, `expanded`, `collector`, `bonus track`, `special edition`, `box set`) are deprioritized below standard core tracklists.
4. **Remaster Preference**:
   - Among clean standard editions, remastered releases (`remaster`, `remastered`) are prioritized over older, flatter transfers.
5. **Regional Prioritization**:
   - **Worldwide** (`[Worldwide]`, `XW`) $\rightarrow$ **USA** (`US`) $\rightarrow$ **Europe** (`XE`) $\rightarrow$ **UK** (`GB`) $\rightarrow$ **Japan** (`JP`) $\rightarrow$ Others.
6. **Release Status & Stability**:
   - `Official` $\rightarrow$ `Promotion` $\rightarrow$ `Bootleg` / `Pseudo-Release`, with earliest release date as final tie-breaker.

---

## 🌿 Branches & Upstream Tracking

| Branch | Focus | Status |
| :--- | :--- | :--- |
| [`develop`](https://github.com/Krabardaf/Lidarr/tree/develop) | Integration branch for this fork | Active |
| [`perf/optimize-album-artist-stats`](https://github.com/Krabardaf/Lidarr/tree/perf/optimize-album-artist-stats) | AlbumController artist statistics speedup | Submitted: [PR #5838](https://github.com/Lidarr/Lidarr/pull/5838) |
| [`feature/release-selection-preference`](https://github.com/Krabardaf/Lidarr/tree/feature/release-selection-preference) | Smart release & medium preference engine | Tested & ready for PR |

---

## 🔗 Upstream Repository
The official Lidarr project is maintained at [Lidarr/Lidarr](https://github.com/Lidarr/Lidarr).
