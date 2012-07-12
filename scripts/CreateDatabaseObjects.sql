CREATE SCHEMA [SqlBus] AUTHORIZATION [dbo]
GO
CREATE SCHEMA [MessageLog] AUTHORIZATION [dbo]
GO
CREATE SCHEMA [Events] AUTHORIZATION [dbo]
GO
CREATE SCHEMA [ConferenceRegistrationProcesses] AUTHORIZATION [dbo]
GO
CREATE SCHEMA [ConferenceRegistration] AUTHORIZATION [dbo]
GO
CREATE SCHEMA [ConferencePayments] AUTHORIZATION [dbo]
GO
CREATE SCHEMA [ConferenceManagement] AUTHORIZATION [dbo]
GO
CREATE SCHEMA [BlobStorage] AUTHORIZATION [dbo]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [MessageLog].[Messages](
	[Id] [uniqueidentifier] NOT NULL,
	[Kind] [nvarchar](max) NULL,
	[SourceId] [nvarchar](max) NULL,
	[AssemblyName] [nvarchar](max) NULL,
	[Namespace] [nvarchar](max) NULL,
	[FullName] [nvarchar](max) NULL,
	[TypeName] [nvarchar](max) NULL,
	[SourceType] [nvarchar](max) NULL,
	[CreationDate] [nvarchar](max) NULL,
	[Payload] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SqlBus].[Events](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[DeliveryDate] [datetime] NULL,
	[CorrelationId] [nvarchar](max) NULL,
 CONSTRAINT [PK_SqlBus.Events] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [Events].[Events](
	[AggregateId] [uniqueidentifier] NOT NULL,
	[AggregateType] [nvarchar](128) NOT NULL,
	[Version] [int] NOT NULL,
	[Payload] [nvarchar](max) NULL,
	[CorrelationId] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[AggregateId] ASC,
	[AggregateType] ASC,
	[Version] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistration].[ConferencesView](
	[Id] [uniqueidentifier] NOT NULL,
	[Code] [nvarchar](max) NULL,
	[Name] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[Location] [nvarchar](max) NULL,
	[Tagline] [nvarchar](max) NULL,
	[TwitterSearch] [nvarchar](max) NULL,
	[StartDate] [datetimeoffset](7) NOT NULL,
	[IsPublished] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistration].[ConferenceSeatTypesView](
	[Id] [uniqueidentifier] NOT NULL,
	[ConferenceId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[Price] [decimal](18, 2) NOT NULL,
	[Quantity] [int] NOT NULL,
	[AvailableQuantity] [int] NOT NULL,
	[SeatsAvailabilityVersion] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
CREATE NONCLUSTERED INDEX [IX_SeatTypesView_ConferenceId] ON [ConferenceRegistration].[ConferenceSeatTypesView] 
(
	[ConferenceId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceManagement].[Conferences](
	[Id] [uniqueidentifier] NOT NULL,
	[AccessCode] [nvarchar](6) NULL,
	[OwnerName] [nvarchar](max) NOT NULL,
	[OwnerEmail] [nvarchar](max) NOT NULL,
	[Slug] [nvarchar](max) NOT NULL,
	[WasEverPublished] [bit] NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[Description] [nvarchar](max) NOT NULL,
	[Location] [nvarchar](max) NOT NULL,
	[Tagline] [nvarchar](max) NULL,
	[TwitterSearch] [nvarchar](max) NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NOT NULL,
	[IsPublished] [bit] NOT NULL,
 CONSTRAINT [PK_ConferenceManagement.Conferences] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [SqlBus].[Commands](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Body] [nvarchar](max) NOT NULL,
	[DeliveryDate] [datetime] NULL,
	[CorrelationId] [nvarchar](max) NULL,
 CONSTRAINT [PK_SqlBus.Commands] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [BlobStorage].[Blobs](
	[Id] [nvarchar](128) NOT NULL,
	[ContentType] [nvarchar](max) NULL,
	[Blob] [varbinary](max) NULL,
	[BlobString] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistration].[PricedOrderLineSeatTypeDescriptionsV3](
	[SeatTypeId] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[SeatTypeId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistration].[OrdersViewV3](
	[OrderId] [uniqueidentifier] NOT NULL,
	[ConferenceId] [uniqueidentifier] NOT NULL,
	[ReservationExpirationDate] [datetime] NULL,
	[StateValue] [int] NOT NULL,
	[OrderVersion] [int] NOT NULL,
	[RegistrantEmail] [nvarchar](max) NULL,
	[AccessCode] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistrationProcesses].[RegistrationProcess](
	[Id] [uniqueidentifier] NOT NULL,
	[Completed] [bit] NOT NULL,
	[ConferenceId] [uniqueidentifier] NOT NULL,
	[OrderId] [uniqueidentifier] NOT NULL,
	[ReservationId] [uniqueidentifier] NOT NULL,
	[SeatReservationCommandId] [uniqueidentifier] NOT NULL,
	[ReservationAutoExpiration] [datetime] NULL,
	[ExpirationCommandId] [uniqueidentifier] NOT NULL,
	[StateValue] [int] NOT NULL,
	[TimeStamp] [timestamp] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
CREATE NONCLUSTERED INDEX [IX_RegistrationProcessManager_Completed] ON [ConferenceRegistrationProcesses].[RegistrationProcess] 
(
	[Completed] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO
CREATE NONCLUSTERED INDEX [IX_RegistrationProcessManager_OrderId] ON [ConferenceRegistrationProcesses].[RegistrationProcess] 
(
	[OrderId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistration].[PricedOrdersV3](
	[OrderId] [uniqueidentifier] NOT NULL,
	[AssignmentsId] [uniqueidentifier] NULL,
	[Total] [decimal](18, 2) NOT NULL,
	[OrderVersion] [int] NOT NULL,
	[IsFreeOfCharge] [bit] NOT NULL,
	[ReservationExpirationDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistrationProcesses].[UndispatchedMessages](
	[Id] [uniqueidentifier] NOT NULL,
	[Commands] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferencePayments].[ThirdPartyProcessorPayments](
	[Id] [uniqueidentifier] NOT NULL,
	[StateValue] [int] NOT NULL,
	[PaymentSourceId] [uniqueidentifier] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[TotalAmount] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceManagement].[Orders](
	[Id] [uniqueidentifier] NOT NULL,
	[ConferenceId] [uniqueidentifier] NOT NULL,
	[AssignmentsId] [uniqueidentifier] NULL,
	[AccessCode] [nvarchar](max) NULL,
	[RegistrantName] [nvarchar](max) NULL,
	[RegistrantEmail] [nvarchar](max) NULL,
	[TotalAmount] [decimal](18, 2) NOT NULL,
	[StatusValue] [int] NOT NULL,
 CONSTRAINT [PK_ConferenceManagement.Orders] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistration].[OrderItemsViewV3](
	[OrderId] [uniqueidentifier] NOT NULL,
	[SeatType] [uniqueidentifier] NOT NULL,
	[RequestedSeats] [int] NOT NULL,
	[ReservedSeats] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC,
	[SeatType] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [ConferencePayments].[ThirdPartyProcessorPaymentDetailsView]
AS
SELECT     
    Id AS Id, 
    StateValue as StateValue,
    PaymentSourceId as PaymentSourceId,
    Description as Description,
    TotalAmount as TotalAmount
FROM ConferencePayments.ThirdPartyProcessorPayments
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferencePayments].[ThidPartyProcessorPaymentItems](
	[Id] [uniqueidentifier] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Amount] [decimal](18, 2) NOT NULL,
	[ThirdPartyProcessorPayment_Id] [uniqueidentifier] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceManagement].[SeatTypes](
	[Id] [uniqueidentifier] NOT NULL,
	[Name] [nvarchar](70) NOT NULL,
	[Description] [nvarchar](250) NOT NULL,
	[Quantity] [int] NOT NULL,
	[Price] [decimal](18, 2) NOT NULL,
	[ConferenceInfo_Id] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_ConferenceManagement.SeatTypes] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
CREATE NONCLUSTERED INDEX [IX_ConferenceInfo_Id] ON [ConferenceManagement].[SeatTypes] 
(
	[ConferenceInfo_Id] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceRegistration].[PricedOrderLinesV3](
	[OrderId] [uniqueidentifier] NOT NULL,
	[Position] [int] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[UnitPrice] [decimal](18, 2) NOT NULL,
	[Quantity] [int] NOT NULL,
	[LineTotal] [decimal](18, 2) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC,
	[Position] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [ConferenceManagement].[OrderSeats](
	[OrderId] [uniqueidentifier] NOT NULL,
	[Position] [int] NOT NULL,
	[Attendee_FirstName] [nvarchar](max) NULL,
	[Attendee_LastName] [nvarchar](max) NULL,
	[Attendee_Email] [nvarchar](max) NULL,
	[SeatInfoId] [uniqueidentifier] NOT NULL,
 CONSTRAINT [PK_ConferenceManagement.OrderSeats] PRIMARY KEY CLUSTERED 
(
	[OrderId] ASC,
	[Position] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF)
)
GO
CREATE NONCLUSTERED INDEX [IX_OrderId] ON [ConferenceManagement].[OrderSeats] 
(
	[OrderId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO
CREATE NONCLUSTERED INDEX [IX_SeatInfoId] ON [ConferenceManagement].[OrderSeats] 
(
	[SeatInfoId] ASC
)WITH (STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF)
GO
ALTER TABLE [ConferenceRegistration].[OrderItemsViewV3]  WITH CHECK ADD  CONSTRAINT [DraftOrder_Lines] FOREIGN KEY([OrderId])
REFERENCES [ConferenceRegistration].[OrdersViewV3] ([OrderId])
ON DELETE CASCADE
GO
ALTER TABLE [ConferenceRegistration].[OrderItemsViewV3] CHECK CONSTRAINT [DraftOrder_Lines]
GO
ALTER TABLE [ConferencePayments].[ThidPartyProcessorPaymentItems]  WITH CHECK ADD  CONSTRAINT [ThirdPartyProcessorPayment_Items] FOREIGN KEY([ThirdPartyProcessorPayment_Id])
REFERENCES [ConferencePayments].[ThirdPartyProcessorPayments] ([Id])
GO
ALTER TABLE [ConferencePayments].[ThidPartyProcessorPaymentItems] CHECK CONSTRAINT [ThirdPartyProcessorPayment_Items]
GO
ALTER TABLE [ConferenceManagement].[SeatTypes]  WITH CHECK ADD  CONSTRAINT [FK_ConferenceManagement.SeatTypes_ConferenceManagement.Conferences_ConferenceInfo_Id] FOREIGN KEY([ConferenceInfo_Id])
REFERENCES [ConferenceManagement].[Conferences] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [ConferenceManagement].[SeatTypes] CHECK CONSTRAINT [FK_ConferenceManagement.SeatTypes_ConferenceManagement.Conferences_ConferenceInfo_Id]
GO
ALTER TABLE [ConferenceRegistration].[PricedOrderLinesV3]  WITH CHECK ADD  CONSTRAINT [PricedOrder_Lines] FOREIGN KEY([OrderId])
REFERENCES [ConferenceRegistration].[PricedOrdersV3] ([OrderId])
ON DELETE CASCADE
GO
ALTER TABLE [ConferenceRegistration].[PricedOrderLinesV3] CHECK CONSTRAINT [PricedOrder_Lines]
GO
ALTER TABLE [ConferenceManagement].[OrderSeats]  WITH CHECK ADD  CONSTRAINT [FK_ConferenceManagement.OrderSeats_ConferenceManagement.Orders_OrderId] FOREIGN KEY([OrderId])
REFERENCES [ConferenceManagement].[Orders] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [ConferenceManagement].[OrderSeats] CHECK CONSTRAINT [FK_ConferenceManagement.OrderSeats_ConferenceManagement.Orders_OrderId]
GO
ALTER TABLE [ConferenceManagement].[OrderSeats]  WITH CHECK ADD  CONSTRAINT [FK_ConferenceManagement.OrderSeats_ConferenceManagement.SeatTypes_SeatInfoId] FOREIGN KEY([SeatInfoId])
REFERENCES [ConferenceManagement].[SeatTypes] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [ConferenceManagement].[OrderSeats] CHECK CONSTRAINT [FK_ConferenceManagement.OrderSeats_ConferenceManagement.SeatTypes_SeatInfoId]
GO
