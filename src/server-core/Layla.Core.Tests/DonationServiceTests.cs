using Layla.Core.Common;
using Layla.Core.Configuration;
using Layla.Core.Constants;
using Layla.Core.Contracts.Donation;
using Layla.Core.Entities;
using Layla.Core.Interfaces.Data;
using Layla.Core.Interfaces.Services;
using Layla.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Layla.Core.Tests;

file static class DonationSutFactory
{
    internal const string DonorId = "donor-user-id";
    internal const string OwnerId = "owner-user-id";
    internal static readonly Guid ProjectId = Guid.NewGuid();

    internal record Components(
        IDonationRepository Donations,
        IProjectRepository Projects,
        IPayPalClient PayPal,
        DonationService Sut);

    internal static Components Create()
    {
        var donations = Substitute.For<IDonationRepository>();
        var projects = Substitute.For<IProjectRepository>();
        var payPal = Substitute.For<IPayPalClient>();
        var options = Options.Create(new PayPalSettings
        {
            ClientId = "sandbox-client-id",
            ClientSecret = "sandbox-client-secret",
            BaseUrl = "https://api-m.sandbox.paypal.com",
            Currency = "MXN"
        });

        return new(donations, projects, payPal,
            new DonationService(donations, projects, payPal, options, NullLogger<DonationService>.Instance));
    }

    internal static Project PublicProject() => new()
    {
        Id = ProjectId,
        Title = "Public Novel",
        IsPublic = true
    };
}

public class DonationService_CreatePayPalOrderAsync_WhenAmountIsInvalid
{
    private readonly Result<PayPalDonationOrderResponseDto> _result;
    private readonly IPayPalClient _payPal;

    public DonationService_CreatePayPalOrderAsync_WhenAmountIsInvalid()
    {
        var c = DonationSutFactory.Create();
        _payPal = c.PayPal;
        _result = c.Sut.CreatePayPalOrderAsync(
                DonationSutFactory.ProjectId,
                new CreatePayPalDonationOrderRequestDto { Amount = 0 },
                DonationSutFactory.DonorId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInvalidInput() => Assert.Equal(ErrorCode.InvalidInput, _result.ErrorCode);

    [Fact]
    public async Task PayPal_IsNotCalled() =>
        await _payPal.DidNotReceive().CreateOrderAsync(Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
}

public class DonationService_CreatePayPalOrderAsync_WhenProjectIsPrivate
{
    private readonly Result<PayPalDonationOrderResponseDto> _result;

    public DonationService_CreatePayPalOrderAsync_WhenProjectIsPrivate()
    {
        var c = DonationSutFactory.Create();
        c.Projects.GetProjectByIdAsync(DonationSutFactory.ProjectId, Arg.Any<CancellationToken>())
            .Returns(new Project { Id = DonationSutFactory.ProjectId, Title = "Private", IsPublic = false });

        _result = c.Sut.CreatePayPalOrderAsync(
                DonationSutFactory.ProjectId,
                new CreatePayPalDonationOrderRequestDto { Amount = 25 },
                DonationSutFactory.DonorId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsInvalidInput() => Assert.Equal(ErrorCode.InvalidInput, _result.ErrorCode);
}

public class DonationService_CreatePayPalOrderAsync_WhenSucceeds
{
    private readonly Result<PayPalDonationOrderResponseDto> _result;
    private readonly IDonationRepository _donations;

    public DonationService_CreatePayPalOrderAsync_WhenSucceeds()
    {
        var c = DonationSutFactory.Create();
        _donations = c.Donations;
        c.Projects.GetProjectByIdAsync(DonationSutFactory.ProjectId, Arg.Any<CancellationToken>())
            .Returns(DonationSutFactory.PublicProject());
        c.PayPal.CreateOrderAsync(Arg.Any<decimal>(), "MXN", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new PayPalOrderResult("PAYPAL-ORDER-1", "CREATED"));

        _result = c.Sut.CreatePayPalOrderAsync(
                DonationSutFactory.ProjectId,
                new CreatePayPalDonationOrderRequestDto { Amount = 125.50m },
                DonationSutFactory.DonorId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void OrderId_IsReturned() => Assert.Equal("PAYPAL-ORDER-1", _result.Data!.OrderId);
    [Fact] public void Currency_IsConfigured() => Assert.Equal("MXN", _result.Data!.Currency);

    [Fact]
    public async Task Donation_IsPersisted() =>
        await _donations.Received(1).AddDonationAsync(
            Arg.Is<Donation>(d => d.Amount == 125.50m && d.Status == DonationStatuses.Created && d.PayPalOrderId == "PAYPAL-ORDER-1"),
            Arg.Any<CancellationToken>());
}

public class DonationService_CapturePayPalOrderAsync_WhenSucceeds
{
    private readonly Result<DonationResponseDto> _result;
    private readonly Donation _donation;

    public DonationService_CapturePayPalOrderAsync_WhenSucceeds()
    {
        var c = DonationSutFactory.Create();
        _donation = new Donation
        {
            Id = Guid.NewGuid(),
            ProjectId = DonationSutFactory.ProjectId,
            DonorUserId = DonationSutFactory.DonorId,
            Amount = 10,
            Currency = "MXN",
            Status = DonationStatuses.Created,
            PayPalOrderId = "ORDER-1"
        };
        c.Donations.GetDonationByPayPalOrderIdAsync("ORDER-1", Arg.Any<CancellationToken>())
            .Returns(_donation);
        c.PayPal.CaptureOrderAsync("ORDER-1", Arg.Any<CancellationToken>())
            .Returns(new PayPalCaptureResult("ORDER-1", "COMPLETED", "CAPTURE-1"));

        _result = c.Sut.CapturePayPalOrderAsync(
                DonationSutFactory.ProjectId,
                new CapturePayPalDonationRequestDto { OrderId = "ORDER-1" },
                DonationSutFactory.DonorId)
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsSuccess() => Assert.True(_result.IsSuccess);
    [Fact] public void Donation_StatusIsCaptured() => Assert.Equal(DonationStatuses.Captured, _donation.Status);
    [Fact] public void Donation_CaptureIdIsSet() => Assert.Equal("CAPTURE-1", _donation.PayPalCaptureId);
}

public class DonationService_GetProjectDonationsAsync_WhenCallerIsNotOwner
{
    private readonly Result<IEnumerable<DonationResponseDto>> _result;

    public DonationService_GetProjectDonationsAsync_WhenCallerIsNotOwner()
    {
        var c = DonationSutFactory.Create();
        c.Projects.UserHasRoleInProjectAsync(DonationSutFactory.ProjectId, "reader-id", ProjectRoles.Owner, Arg.Any<CancellationToken>())
            .Returns(false);

        _result = c.Sut.GetProjectDonationsAsync(DonationSutFactory.ProjectId, "reader-id")
            .GetAwaiter().GetResult();
    }

    [Fact] public void Result_IsNotSuccess() => Assert.False(_result.IsSuccess);
    [Fact] public void ErrorCode_IsForbidden() => Assert.Equal(ErrorCode.Forbidden, _result.ErrorCode);
}
