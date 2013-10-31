using System;
using System.Collections.Generic;
using System.Data;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Logging;

namespace Datwendo.ClientConnector {
    public class Migrations : DataMigrationImpl {

        public Migrations()
        {
            Logger = NullLogger.Instance;
        }

        public ILogger Logger { get; set; }

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
                .Column<int>("CIndex", c => c.WithDefault(0))
                .Column<string>("DataSent", c => c.Nullable().WithLength(4096))
                );

            ContentDefinitionManager.AlterPartDefinition("ClientConnectorPart",
                builder => builder.Attachable());

            return 3;
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

        /* Unfortunately in v1 the CIndex was declared as a string, it is an int
         * BUT THERE IS A BUG IN ORCHARD 1.7 un til actual 1.7.2 https://orchard.codeplex.com/workitem/19298
         * this bug when droping a clumn does not chack for constraint and sql server needs first droping the constraint
         * YOU MUST DROP THE CONTRAINT by hand either using SQL Server Management Studio, either using a script as expliend down here
         * A way to find the name of a default constraint for a given table and column could be something like this: 
            declare @column nvarchar(128) 
            declare @table nvarchar(128) 

            set @table = 'table1' 
            set @column = 'column_1' 

            select dc."name" as "DF_constraint" 
            from sys.default_constraints dc 
            join sys.columns c on c.default_object_id = dc.object_id 
            where dc.parent_object_id = OBJECT_ID(@table) 
            and c."name" = @column
         * when the constraint is dropped, you can run the update module.
         */
        public int UpdateFrom2()
        {
            try
            {
                SchemaBuilder.AlterTable("ClientConnectorPartRecord", table => table
                    .DropColumn("CIndex"));
                SchemaBuilder.AlterTable("ClientConnectorPartRecord", table => table
                    .AddColumn<int>("CIndex", c => c.WithDefault(0)));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "ClientConnector Migration update from 2: you must drop the constraint by hand before running the module update");
                throw;
            }
            SchemaBuilder.AlterTable("ClientConnectorPartRecord", table => table
                .DropColumn("DataSent"));
            SchemaBuilder.AlterTable("ClientConnectorPartRecord", table => table
                    .AddColumn<string>("DataSent", c => c.Nullable().WithLength(4096)));            
            return 3;
        }
    }
}