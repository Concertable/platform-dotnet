using Azure.Storage.Blobs;
using Concertable.Seed.Shared;
using Concertable.Shared.Blob.Application;
using Concertable.Shared.Blob.Infrastructure.Resources;
using Microsoft.Extensions.Options;

namespace Concertable.Shared.Blob.Infrastructure;

internal sealed class BlobDevSeeder : IDevSeeder
{
    public int Order => 0;

    private const string Sentinel = ".seeded";

    private static readonly string[] ArtistBanners =
    [
        "rockers.jpg", "indievibes.jpg", "electronicpulse.jpg", "hiphopflow.jpg",
        "jazzmaster.jpg", "alwayspunks.jpg", "hollowfrequencies.jpg", "neonfoxes.jpg",
        "velvetstatic.jpg", "echobloom.jpg", "wildchords.jpg", "glitchandglow.jpg",
        "sonicmirage.jpg", "neonechoes.jpg", "dreamwavecollective.jpg", "synthpulse.jpg",
        "brasspoets.jpg", "groovealchemy.jpg", "velvetrhymes.jpg", "lofisyndicate.jpg",
        "beatsbluenotes.jpg", "basspilots.jpg", "digitalprophets.jpg", "neonbasstheory.jpg",
        "wavelength303.jpg", "gravityloops.jpg", "goldenreverie.jpg", "fablesound.jpg",
        "moonlightstatic.jpg", "thechromatics.jpg", "echoreverberation.jpg", "midnightreverie.jpg",
        "staticwolves.jpg", "echocollapse.jpg", "violetsundown.jpg"
    ];

    private static readonly string[] VenueBanners =
    [
        "grandvenue.jpg", "redhillhall.jpg", "weybridgepavilon.jpg", "cobhamarts.jpg",
        "chertseyarena.jpg", "camdenballroom.jpg", "manchesternightday.jpg", "birminghamo2.jpg",
        "edinburghusher.jpg", "liverpoolphilharmonic.jpg", "leedsbrudenell.jpg",
        "glasgowbarrowland.jpg", "sheffieldleadmill.jpg", "nottinghamrockcity.jpg",
        "bristolthekla.jpg", "brightonconcorde2.jpg", "cardifftramshed.jpg", "newcastleo2.jpg",
        "oxfordo2.jpg", "cambridgecornexchange.jpg", "bathkomedia.jpg", "aberdeenlemontree.jpg",
        "yorkbarbican.jpg", "belfastlimelight.jpg", "dublinvicarstreet.jpg", "norwichwaterfront.jpg",
        "exeterphoenix.jpg", "southamptonengine.jpg", "hullwellyclub.jpg", "plymouthjunction.jpg",
        "swanseasincity.jpg", "invernessironworks.jpg", "stirlingalberthalls.jpg",
        "dundeefatsams.jpg", "coventryempire.jpg"
    ];

    private readonly BlobServiceClient blobServiceClient;
    private readonly IBlobStorageService blobStorage;
    private readonly string containerName;

    public BlobDevSeeder(BlobServiceClient blobServiceClient, IBlobStorageService blobStorage, IOptions<BlobStorageSettings> options)
    {
        this.blobServiceClient = blobServiceClient;
        this.blobStorage = blobStorage;
        containerName = options.Value.ContainerName!;
    }

    public Task MigrateAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

        if (await blobStorage.ExistsAsync(Sentinel))
            return;

        await blobStorage.UploadAsync(SeedImages.Avatar(), "avatar.jpg");

        foreach (var banner in ArtistBanners)
            await blobStorage.UploadAsync(SeedImages.Banner(), banner);

        foreach (var banner in VenueBanners)
            await blobStorage.UploadAsync(SeedImages.Banner(), banner);

        await blobStorage.UploadAsync(Stream.Null, Sentinel);
    }
}
