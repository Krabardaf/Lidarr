namespace NzbDrone.Core.Test.MusicTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using NUnit.Framework;
    using NzbDrone.Core.Music;
    using NzbDrone.Core.Test.Framework;

    [TestFixture]
    public class AlbumReleasePreferenceComparerFixture : CoreTest
    {
        private AlbumRelease CreateRelease(string format = "Digital Media",
                                           string country = "[Worldwide]",
                                           string title = "Test Album",
                                           string disambiguation = "",
                                           string status = "Official",
                                           int trackCount = 10,
                                           DateTime? releaseDate = null,
                                           string id = "rel-1")
        {
            return new AlbumRelease
            {
                ForeignReleaseId = id,
                Title = title,
                Disambiguation = disambiguation,
                Status = status,
                TrackCount = trackCount,
                ReleaseDate = releaseDate ?? new DateTime(2020, 1, 1),
                Country = string.IsNullOrEmpty(country) ? new List<string>() : new List<string> { country },
                Media = string.IsNullOrEmpty(format) ? new List<Medium>() : new List<Medium> { new Medium { Format = format, Number = 1 } }
            };
        }

        [Test]
        public void should_prefer_digital_over_cd_vinyl_and_cassette()
        {
            var digital = CreateRelease(format: "Digital Media", id: "digital");
            var cd = CreateRelease(format: "CD", id: "cd");
            var vinyl = CreateRelease(format: "Vinyl", id: "vinyl");
            var cassette = CreateRelease(format: "Cassette", id: "cassette");

            var list = new List<AlbumRelease> { cassette, vinyl, cd, digital };
            var sorted = list.OrderByPreference().ToList();

            sorted[0].Should().Be(digital);
            sorted[1].Should().Be(cd);
            sorted[2].Should().Be(vinyl);
            sorted[3].Should().Be(cassette);
        }

        [Test]
        public void should_treat_sacd_and_shm_cd_as_exotic_below_standard_cd()
        {
            var cd = CreateRelease(format: "CD", id: "cd");
            var vinyl = CreateRelease(format: "12\" Vinyl", id: "vinyl");
            var sacd = CreateRelease(format: "SACD", id: "sacd");
            var shm = CreateRelease(format: "SHM-CD", id: "shm");

            var list = new List<AlbumRelease> { sacd, shm, vinyl, cd };
            var sorted = list.OrderByPreference().ToList();

            sorted[0].Should().Be(cd);
            sorted[1].Should().Be(vinyl);

            // SACD and SHM-CD should be at the bottom with exotic formats
            sorted.Skip(2).Should().Contain(new[] { sacd, shm });
        }

        [Test]
        public void should_prefer_clean_standard_release_over_deluxe_or_anniversary()
        {
            var standard = CreateRelease(title: "Nevermind", disambiguation: "", trackCount: 12, id: "std");
            var deluxe = CreateRelease(title: "Nevermind (Deluxe Edition)", disambiguation: "deluxe edition", trackCount: 40, id: "deluxe");
            var anniversary = CreateRelease(title: "Nevermind", disambiguation: "30th anniversary edition", trackCount: 35, id: "anniv");

            var list = new List<AlbumRelease> { deluxe, anniversary, standard };
            var sorted = list.OrderByPreference().ToList();

            sorted.First().Should().Be(standard);
        }

        [Test]
        public void should_prefer_remaster_among_clean_standard_releases()
        {
            var original = CreateRelease(title: "The Dark Side of the Moon", disambiguation: "", id: "orig");
            var remaster = CreateRelease(title: "The Dark Side of the Moon", disambiguation: "2011 remastered", id: "remaster");

            var list = new List<AlbumRelease> { original, remaster };
            var sorted = list.OrderByPreference().ToList();

            sorted.First().Should().Be(remaster);
        }

        [Test]
        public void should_prefer_clean_remaster_over_bloated_deluxe_remaster()
        {
            var cleanRemaster = CreateRelease(title: "Paranoid", disambiguation: "2016 remaster", trackCount: 8, id: "clean-remaster");
            var superDeluxe = CreateRelease(title: "Paranoid", disambiguation: "Super Deluxe Edition Remastered", trackCount: 32, id: "super-deluxe");

            var list = new List<AlbumRelease> { superDeluxe, cleanRemaster };
            var sorted = list.OrderByPreference().ToList();

            sorted.First().Should().Be(cleanRemaster);
        }

        [Test]
        public void should_order_countries_by_worldwide_us_europe_uk_japan_others()
        {
            var worldwide = CreateRelease(country: "[Worldwide]", id: "worldwide");
            var us = CreateRelease(country: "United States", id: "us");
            var europe = CreateRelease(country: "Europe", id: "europe");
            var uk = CreateRelease(country: "United Kingdom", id: "uk");
            var japan = CreateRelease(country: "Japan", id: "japan");
            var australia = CreateRelease(country: "Australia", id: "australia");

            var list = new List<AlbumRelease> { australia, japan, uk, europe, us, worldwide };
            var sorted = list.OrderByPreference().ToList();

            sorted[0].Should().Be(worldwide);
            sorted[1].Should().Be(us);
            sorted[2].Should().Be(europe);
            sorted[3].Should().Be(uk);
            sorted[4].Should().Be(japan);
            sorted[5].Should().Be(australia);
        }

        [Test]
        public void should_prefer_official_over_promo_and_bootleg()
        {
            var official = CreateRelease(status: "Official", id: "official");
            var promo = CreateRelease(status: "Promotion", id: "promo");
            var bootleg = CreateRelease(status: "Bootleg", id: "bootleg");

            var list = new List<AlbumRelease> { bootleg, promo, official };
            var sorted = list.OrderByPreference().ToList();

            sorted[0].Should().Be(official);
            sorted[1].Should().Be(promo);
            sorted[2].Should().Be(bootleg);
        }
    }
}
