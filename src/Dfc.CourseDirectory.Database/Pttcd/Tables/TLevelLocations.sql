﻿CREATE TABLE [Pttcd].[TLevelLocations]
(
	[TLevelLocationId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_TLevelLocations] PRIMARY KEY,
	[TLevelLocationStatus] TINYINT NOT NULL,
	[TLevelId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [FK_TLevelLocations_TLevel] FOREIGN KEY REFERENCES [Pttcd].[TLevels] ([TLevelId]),
	[VenueId] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [PK_TLevelLocations_Venue] FOREIGN KEY REFERENCES [Pttcd].[Venues] ([VenueId])
)
