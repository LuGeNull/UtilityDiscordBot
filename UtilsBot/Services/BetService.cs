using UtilsBot.Domain;
using UtilsBot.Domain.BetCancel;
using UtilsBot.Domain.BetClose;
using UtilsBot.Domain.BetPayout;
using UtilsBot.Domain.BetRequest;
using UtilsBot.Domain.BetStart;
using UtilsBot.Domain.ValueObjects;
using UtilsBot.Repository;

namespace UtilsBot.Services;

public class BetService
{
    public async Task<BetStartResponse> HandleMessageAsync(BetStartRequest request, DatabaseRepository db)
    {
        var bet = new Bet
        {
            Id = Guid.NewGuid(),
            Title = request.title,
            Ereignis1Name = request.ereignis1Name,
            Ereignis2Name = request.ereignis2Name,
            UserIdStartedBet = request.userIdStartedBet,
            MaxPayoutMultiplikator = request.maxPayoutMultiplikator,
            StartedAt = DateTime.Now,
            EndedAt = DateTime.Now.AddHours(request.annahmeschlussAbJetztInStunden),
            MessageId = request.messageId,
            ChannelId = request.channelId,
            Placements = new List<BetPlacements>()
        };
        await db.AddBetAsync(bet);
        return new BetStartResponse(bet.ReferenzId);
    }

    public async Task<BetResponse> HandleMessageAsync(BetRequest request, DatabaseRepository db)
    {
        var aktiveBet = await db.GetBetAndPlacementsByMessageId(request.messageId);

        if (aktiveBet == null)
        {
            return new BetResponse(false, requestWasSuccesful : false);
        }
        
        if (aktiveBet.EndedAt < DateTime.Now)
        {
            return new BetResponse(true, true, true, requestWasSuccesful : false);
        }

        if (!db.HatDerUserGenugXpFuerAnfrage(request.userId, request.einsatz))
        {
            return new BetResponse(true, false, requestWasSuccesful : false);
        }

        var erfolgreich = await db.AddUserToBet(request.userId, request.einsatz, request.messageId, request.option);
        if (!erfolgreich)
        {
            return new BetResponse(true, true, false, true , requestWasSuccesful : false);
        }

        return new BetResponse(requestWasSuccesful : true);
    }
    public bool ContainsBetsOnBothSides(Bet? bet)
    {
        var seiteAVorhanden = false;
        var seiteBVorhanden = false;
        foreach (var placement in bet.Placements)
        {
            if (placement.Site)
            {
                seiteAVorhanden = true;
            }

            if (!placement.Site)
            {
                seiteBVorhanden = true;
            }

            if (seiteAVorhanden && seiteBVorhanden)
            {
                return true;
            }
        }

        return false;
    }
    public async Task<bool> IsBetClosed(ulong? messageId, DatabaseRepository db)
    {
        var wette = await db.GetBetAndPlacementsByMessageId(messageId);
        if (wette == null) return true;
        if (wette.EndedAt < DateTime.Now)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task<bool> IsThisUserCreatorOfBet(ulong userId, ulong nachrichtId, DatabaseRepository db)
    {
        return await db.IstDieserUserErstellerDerWette(userId, nachrichtId);
    }

    public async Task WettannahmenSchliessen(ulong messageId, DatabaseRepository db)
    {
        await db.WettannahmenSchliessen(messageId);
    }

    public async Task<Bet?> GetBetByMessageId(ulong messageId, DatabaseRepository db)
    {
        return await db.GetBetAndPlacementsByMessageId(messageId);
    }

    public async Task<BetCancelResponse> HandleMessageAsync(BetCancelRequest request, DatabaseRepository db)
    {
        var bet = await db.GetBetAndPlacementsByMessageId(request.messageId);
        if (bet == null)
        {
            return new BetCancelResponse(false, true, anfrageWarErfolgreich: false);
        }

        if (bet.EndedAt > DateTime.Now)
        {
            return new BetCancelResponse(true, anfrageWarErfolgreich: false);
        }

        bet.WetteWurdeAbgebrochen = true;
        foreach (var placement in bet.Placements)
        {
            var person = await db.GetUserById(placement.UserId);
            if (person == null)
            {
                continue;
            }

            person.Xp += placement.Einsatz;
            placement.Einsatz = 0;
        }

        await db.SaveChangesAsync();
        return new BetCancelResponse(false);
    }

    public async Task<BetPayoutResponse> HandleMessageAsync(BetPayoutRequest request, DatabaseRepository db)
    {
        var bet = await db.GetBetAndPlacementsByMessageId(request.messageId);
        if (bet == null)
        {
            return new BetPayoutResponse(false, true);
        }

        if (bet.EndedAt > DateTime.Now)
        {
            return new BetPayoutResponse(true);
        }

        if (bet.WetteWurdeBeendet)
        {
            return new BetPayoutResponse(false, false, true);
        }

        bet.WetteWurdeBeendet = true;


        foreach (var userBets in bet.Placements.Where(p => p.Site == ConvertBetSideToBool(request.betSide)))
        {
            
        }
        await db.SaveChangesAsync();
        return new BetPayoutResponse(false);
    }

    private bool ConvertBetSideToBool(BetSide requestBetSide)
    {
        if (requestBetSide == BetSide.Yes)
        {
            return true;
        }

        return false;
    }

    public async Task HandleMessageAsync(BetCloseRequest request)
    {
        await WettannahmenSchliessen(request.messageId, request.db);
    }
}