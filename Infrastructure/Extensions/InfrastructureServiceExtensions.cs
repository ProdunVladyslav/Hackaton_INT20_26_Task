using Domain.Services;
using Infrastructure.Contracts.AIGeneration.Internal;
using Infrastructure.UseCases.AIGeneration;
using Infrastructure.UseCases.Analytics;
using Infrastructure.UseCases.Auth;
using Infrastructure.UseCases.Content;
using Infrastructure.UseCases.Edges;
using Infrastructure.UseCases.Flows;
using Infrastructure.UseCases.Leads;
using Infrastructure.UseCases.LeadChannels;
using Infrastructure.UseCases.NodeLeadCaptureFields;
using Infrastructure.UseCases.NodeOffers;
using Infrastructure.UseCases.NodeRedirectLinks;
using Infrastructure.UseCases.Nodes;
using Infrastructure.UseCases.Offers;
using Infrastructure.UseCases.Options;
using Infrastructure.UseCases.Quiz;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

/// <summary>
/// Registers every Infrastructure-layer use case with the DI container.
/// All registrations are Scoped — one instance per HTTP request — because use
/// cases depend on AppDbContext (also Scoped). Singleton would cause a
/// "captured dependency" bug where a long-lived object holds a short-lived DbContext.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<FlowGenerationJobStore>();
        services.AddScoped<StartGenerateFlowUseCase>();
        services.AddScoped<GetGenerateFlowStatusUseCase>();
        services.AddScoped<GenerateFlowUseCase>();

        // ── Auth ──────────────────────────────────────────────────────────────
        services.AddScoped<LoginUseCase>();
        services.AddScoped<SignUpUseCase>();
        services.AddScoped<MeUseCase>();

        // ── Admin: Flows ──────────────────────────────────────────────────────
        services.AddScoped<ListFlowsUseCase>();
        services.AddScoped<GetFlowUseCase>();
        services.AddScoped<GetFlowStatsUseCase>();
        services.AddScoped<CreateFlowUseCase>();
        services.AddScoped<UpdateFlowUseCase>();
        services.AddScoped<SetEntryNodeUseCase>();
        services.AddScoped<PublishFlowUseCase>();
        services.AddScoped<UnpublishFlowUseCase>();
        services.AddScoped<DeleteFlowUseCase>();

        // ── Admin: Nodes ──────────────────────────────────────────────────────
        services.AddScoped<CreateNodeUseCase>();
        services.AddScoped<UpdateNodeUseCase>();
        services.AddScoped<MoveNodeUseCase>();
        services.AddScoped<DeleteNodeUseCase>();

        services.AddScoped<FlowStructureService>();
        services.AddScoped<NodeFactory>();

        // ── Admin: Options ────────────────────────────────────────────────────
        services.AddScoped<CreateOptionUseCase>();
        services.AddScoped<UpdateOptionUseCase>();
        services.AddScoped<DeleteOptionUseCase>();
        services.AddScoped<ReorderOptionsUseCase>();

        // ── Admin: Edges ──────────────────────────────────────────────────────
        services.AddScoped<CreateEdgeUseCase>();
        services.AddScoped<UpdateEdgeUseCase>();
        services.AddScoped<DeleteEdgeUseCase>();

        // ── Admin: Offers ─────────────────────────────────────────────────────
        services.AddScoped<ListOffersUseCase>();
        services.AddScoped<GetOfferUseCase>();
        services.AddScoped<CreateOfferUseCase>();
        services.AddScoped<UpdateOfferUseCase>();
        services.AddScoped<DeleteOfferUseCase>();

        // Leads
        services.AddScoped<GetLeadUseCase>();
        services.AddScoped<UpdateLeadUseCase>();
        services.AddScoped<ListLeadsUseCase>();

        // Lead channels (trackable share links)
        services.AddScoped<CreateLeadChannelUseCase>();
        services.AddScoped<ListLeadChannelsUseCase>();
        services.AddScoped<UpdateLeadChannelUseCase>();
        services.AddScoped<DeleteLeadChannelUseCase>();
        services.AddScoped<ResolveLeadChannelUseCase>();

        // NodeRedirectLinks
        services.AddScoped<CreateNodeRedirectLinkUseCase>();
        services.AddScoped<DeleteNodeRedirectLinkUseCase>();
        services.AddScoped<ReorderNodeRedirectLinksUseCase>();
        services.AddScoped<UpdateNodeRedirectLinkUseCase>();

        // NodeLeadCaptureFields
        services.AddScoped<CreateNodeLeadCaptureFieldUseCase>();
        services.AddScoped<UpdateNodeLeadCaptureFieldUseCase>();
        services.AddScoped<DeleteNodeLeadCaptureFieldUseCase>();
        services.AddScoped<ReorderNodeLeadCaptureFieldsUseCase>();

        // ── Admin: Node↔Offer links ───────────────────────────────────────────
        services.AddScoped<ListNodeOffersUseCase>();
        services.AddScoped<LinkOfferUseCase>();
        services.AddScoped<UpdateNodeOfferUseCase>();
        services.AddScoped<UnlinkOfferUseCase>();

        // ── Content delivery (public) ─────────────────────────────────────────
        services.AddScoped<GetPublishedFlowUseCase>();
        services.AddScoped<GetPublishedFlowByIdUseCase>();

        // ── Quiz engine (public) ──────────────────────────────────────────────
        services.AddScoped<StartSessionUseCase>();
        services.AddScoped<GetSessionUseCase>();
        services.AddScoped<SubmitAnswerUseCase>();
        services.AddScoped<GoBackUseCase>();
        services.AddScoped<ConvertUseCase>();

        // ── Admin: Analytics ──────────────────────────────────────────────────
        services.AddScoped<SessionStatsUseCase>();
        services.AddScoped<OfferStatsUseCase>();
        services.AddScoped<DropOffUseCase>();
        services.AddScoped<LeadQualityUseCase>();
        services.AddScoped<ChannelStatsUseCase>();

        return services;
    }
}
