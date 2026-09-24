namespace NzbDrone.Core.Music
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NzbDrone.Common.Extensions;

    public class AlbumReleasePreferenceComparer : IComparer<AlbumRelease>
    {
        public static readonly AlbumReleasePreferenceComparer Instance = new AlbumReleasePreferenceComparer();

        private static readonly string[] BloatKeywords = new[]
        {
            "deluxe",
            "anniversary",
            "expanded",
            "collector",
            "bonus track",
            "bonus tracks",
            "special edition",
            "box set",
            "outtakes",
            "demos",
            "tour edition"
        };

        private static readonly string[] RemasterKeywords = new[]
        {
            "remaster",
            "remastered",
            "remastering"
        };

        public int Compare(AlbumRelease x, AlbumRelease y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null)
            {
                return 1;
            }

            if (y == null)
            {
                return -1;
            }

            // 1. Medium Format Priority (Digital > CD > Vinyl > Cassette > Others/Exotic)
            var mediumX = GetMediumScore(x);
            var mediumY = GetMediumScore(y);
            var cmp = mediumX.CompareTo(mediumY);
            if (cmp != 0)
            {
                return cmp;
            }

            // 2. Edition bloat penalty (Non-deluxe / standard editions preferred)
            var bloatX = IsBloatOrDeluxe(x);
            var bloatY = IsBloatOrDeluxe(y);
            cmp = bloatX.CompareTo(bloatY);
            if (cmp != 0)
            {
                return cmp;
            }

            // 3. Remaster preference (Remastered preferred among clean editions)
            var remasterX = IsRemaster(x);
            var remasterY = IsRemaster(y);
            cmp = remasterY.CompareTo(remasterX); // true (remaster) comes first
            if (cmp != 0)
            {
                return cmp;
            }

            // 4. Country Priority (Worldwide > USA > Europe > UK > Japan > Others)
            var countryX = GetCountryScore(x);
            var countryY = GetCountryScore(y);
            cmp = countryX.CompareTo(countryY);
            if (cmp != 0)
            {
                return cmp;
            }

            // 5. Release Status (Official > Promotion > Bootleg)
            var statusX = GetStatusScore(x);
            var statusY = GetStatusScore(y);
            cmp = statusX.CompareTo(statusY);
            if (cmp != 0)
            {
                return cmp;
            }

            // 6. Release Date (Earliest / original release date if neither is remaster, or for stability)
            var dateX = x.ReleaseDate ?? DateTime.MaxValue;
            var dateY = y.ReleaseDate ?? DateTime.MaxValue;
            cmp = dateX.CompareTo(dateY);
            if (cmp != 0)
            {
                return cmp;
            }

            // 7. Track Count tie-breaker (more complete standard release)
            cmp = y.TrackCount.CompareTo(x.TrackCount);
            if (cmp != 0)
            {
                return cmp;
            }

            // Final deterministic tie-breaker by ID
            return string.Compare(x.ForeignReleaseId, y.ForeignReleaseId, StringComparison.Ordinal);
        }

        public static int GetMediumScore(AlbumRelease release)
        {
            if (release?.Media == null || release.Media.Count == 0)
            {
                return 50; // Unknown
            }

            var formats = release.Media
                .Select(m => m.Format?.Trim())
                .Where(f => f.IsNotNullOrWhiteSpace())
                .ToList();

            if (!formats.Any())
            {
                return 50;
            }

            // 1. Digital Media
            if (formats.Any(f => f.IndexOf("digital", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return 1;
            }

            // Check if exotic audiophile CD format (SACD, SHM-CD, DualDisc, DVD-Audio)
            var isExoticCd = formats.Any(f =>
                f.IndexOf("sacd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.IndexOf("shm-cd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.IndexOf("shm cd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.IndexOf("dvd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                f.IndexOf("dualdisc", StringComparison.OrdinalIgnoreCase) >= 0);

            // 2. Standard Compact Disc
            if (!isExoticCd && formats.Any(f => f.IndexOf("cd", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                f.IndexOf("compact disc", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return 2;
            }

            // 3. Vinyl
            if (formats.Any(f => f.IndexOf("vinyl", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                f.Equals("LP", StringComparison.OrdinalIgnoreCase)))
            {
                return 3;
            }

            // 4. Cassette
            if (formats.Any(f => f.IndexOf("cassette", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return 4;
            }

            // 5. Others / Exotic
            return 5;
        }

        public static int GetCountryScore(AlbumRelease release)
        {
            if (release?.Country == null || release.Country.Count == 0)
            {
                return 60; // Unspecified / Missing
            }

            // 1. Worldwide
            if (release.Country.Any(c => c.Equals("[Worldwide]", StringComparison.OrdinalIgnoreCase) ||
                                         c.Equals("Worldwide", StringComparison.OrdinalIgnoreCase) ||
                                         c.Equals("XW", StringComparison.OrdinalIgnoreCase)))
            {
                return 1;
            }

            // 2. USA
            if (release.Country.Any(c => c.Equals("United States", StringComparison.OrdinalIgnoreCase) ||
                                         c.Equals("US", StringComparison.OrdinalIgnoreCase) ||
                                         c.Equals("USA", StringComparison.OrdinalIgnoreCase)))
            {
                return 2;
            }

            // 3. Europe
            if (release.Country.Any(c => c.Equals("Europe", StringComparison.OrdinalIgnoreCase) ||
                                         c.Equals("XE", StringComparison.OrdinalIgnoreCase)))
            {
                return 3;
            }

            // 4. UK
            if (release.Country.Any(c => c.Equals("United Kingdom", StringComparison.OrdinalIgnoreCase) ||
                                         c.Equals("GB", StringComparison.OrdinalIgnoreCase) ||
                                         c.Equals("UK", StringComparison.OrdinalIgnoreCase)))
            {
                return 4;
            }

            // 5. Japan
            if (release.Country.Any(c => c.Equals("Japan", StringComparison.OrdinalIgnoreCase) ||
                                         c.Equals("JP", StringComparison.OrdinalIgnoreCase)))
            {
                return 5;
            }

            return 6; // Other countries
        }

        public static bool IsBloatOrDeluxe(AlbumRelease release)
        {
            if (release == null)
            {
                return false;
            }

            var text = $"{release.Title} {release.Disambiguation}";
            return BloatKeywords.Any(k => text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool IsRemaster(AlbumRelease release)
        {
            if (release == null)
            {
                return false;
            }

            var text = $"{release.Title} {release.Disambiguation}";
            return RemasterKeywords.Any(k => text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static int GetStatusScore(AlbumRelease release)
        {
            if (string.IsNullOrWhiteSpace(release?.Status))
            {
                return 4;
            }

            if (release.Status.Equals("Official", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            if (release.Status.Equals("Promotion", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return 3; // Bootleg, Pseudo-Release, etc.
        }
    }

    public static class AlbumReleasePreferenceExtensions
    {
        public static IOrderedEnumerable<AlbumRelease> OrderByPreference(this IEnumerable<AlbumRelease> releases)
        {
            return releases.OrderBy(x => x, AlbumReleasePreferenceComparer.Instance);
        }
    }
}
