using System.Text.Json.Serialization;

namespace Concertable.Shared;

[JsonDerivedType(typeof(ArtistHeaderDto), "artist")]
[JsonDerivedType(typeof(VenueHeaderDto), "venue")]
[JsonDerivedType(typeof(ConcertHeaderDto), "concert")]
public interface IHeader : IHasRating, IAddress
{
    string Name { get; set; }
    string ImageUrl { get; set; }
}
