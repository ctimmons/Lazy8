﻿USE [finance]
go

IF OBJECT_ID('dbo.CK_CONSTRAINT_IS_WEEKEND', 'C') IS NOT NULL
  ALTER TABLE [dbo].[FEDERAL_CALENDAR] DROP CONSTRAINT [CK_CONSTRAINT_IS_WEEKEND]
GO
--~
USE [finance]
--~
IF OBJECT_ID('dbo.CK_CONSTRAINT_IS_WEEKEND', 'C') IS NOT NULL
  ALTER TABLE [dbo].[FEDERAL_CALENDAR] DROP CONSTRAINT [CK_CONSTRAINT_IS_WEEKEND]

