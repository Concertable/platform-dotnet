using Concertable.Artist.Domain;
using Concertable.Concert.Domain;
using Concertable.Contract.Domain;
using Concertable.User.Domain;
using Concertable.Conversations.Domain;
using Concertable.Shared;
using Concertable.Venue.Domain;

namespace Concertable.DataAccess;

public interface IReadDbContext
{
    IQueryable<UserEntity> Users { get; }
    IQueryable<ArtistEntity> Artists { get; }
    IQueryable<ArtistGenreEntity> ArtistGenres { get; }
    IQueryable<VenueEntity> Venues { get; }
    IQueryable<VenueImageEntity> VenueImages { get; }
    IQueryable<ConcertEntity> Concerts { get; }
    IQueryable<ConcertGenreEntity> ConcertGenres { get; }
    IQueryable<ConcertImageEntity> ConcertImages { get; }
    IQueryable<OpportunityEntity> Opportunities { get; }
    IQueryable<OpportunityGenreEntity> OpportunityGenres { get; }
    IQueryable<ApplicationEntity> Applications { get; }
    IQueryable<BookingEntity> Bookings { get; }
    IQueryable<MessageEntity> Messages { get; }
    IQueryable<TransactionEntity> Transactions { get; }
    IQueryable<TicketTransactionEntity> TicketTransactions { get; }
    IQueryable<SettlementTransactionEntity> SettlementTransactions { get; }
    IQueryable<StripeEventEntity> StripeEvents { get; }
    IQueryable<PayoutAccountEntity> PayoutAccounts { get; }
    IQueryable<EscrowEntity> Escrows { get; }
    IQueryable<ContractEntity> Contracts { get; }
    IQueryable<FlatFeeContractEntity> FlatFeeContracts { get; }
    IQueryable<DoorSplitContractEntity> DoorSplitContracts { get; }
    IQueryable<VersusContractEntity> VersusContracts { get; }
    IQueryable<VenueHireContractEntity> VenueHireContracts { get; }
    IQueryable<ArtistRatingProjection> ArtistRatingProjections { get; }
    IQueryable<VenueRatingProjection> VenueRatingProjections { get; }
    IQueryable<ConcertRatingProjection> ConcertRatingProjections { get; }
}
