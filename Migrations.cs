using System;
using System.Collections.Generic;
using System.Data;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;

namespace Datwendo.ClientConnector {
    public class Migrations : DataMigrationImpl {

        public int Create() 
        {
            SchemaBuilder.CreateTable("ClientConnectorAdminSettingsPartRecord", table => table
                    .ContentPartRecord()
                    .Column<int>("PublisherId", c => c.WithDefault(0))
                    .Column<string>("ServiceProdUrl", c => c.WithLength(255))
                    .Column<int>("CurrentAPI", c => c.WithDefault(1))
                    .Column<int>("TransactionDelay", c => c.WithDefault(200))
                    );
            SchemaBuilder.CreateTable("ClientConnectorPartRecord", table => table
                .ContentPartRecord()
                .Column<string>("CIndex", c => c.WithDefault(0))
                );

            ContentDefinitionManager.AlterPartDefinition("ClientConnectorPart",
                builder => builder.Attachable());

            return 2;
        }

        public int UpdateFrom1()
        {
            SchemaBuilder.DropTable("ClientConnectorSettingsPartRecord");

            SchemaBuilder.CreateTable("ClientConnectorAdminSettingsPartRecord", table => table
                    .ContentPartRecord()
                    .Column<int>("PublisherId", c => c.WithDefault(0))
                    .Column<string>("ServiceProdUrl", c => c.WithLength(255))
                    .Column<int>("CurrentAPI", c => c.WithDefault(1))
                    .Column<int>("TransactionDelay", c => c.WithDefault(200))
                    );
            return 2;
        }

    }
}