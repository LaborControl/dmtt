using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LaborControl.API.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceColumnToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Vérifier si la table Orders existe, sinon la créer
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'Orders') THEN
                        CREATE TABLE ""Orders"" (
                            ""Id"" uuid NOT NULL,
                            ""CustomerId"" uuid NOT NULL,
                            ""OrderNumber"" character varying(20) NOT NULL,
                            ""ChipsQuantity"" integer NOT NULL,
                            ""TotalAmount"" numeric NOT NULL,
                            ""Status"" character varying(50) NOT NULL,
                            ""DeliveryAddress"" character varying(500) NOT NULL,
                            ""DeliveryCity"" character varying(100),
                            ""DeliveryPostalCode"" character varying(10),
                            ""DeliveryCountry"" character varying(100),
                            ""Service"" character varying(200),
                            ""StripePaymentIntentId"" text,
                            ""StripeCheckoutSessionId"" text,
                            ""TrackingNumber"" text,
                            ""ShippedAt"" timestamp with time zone,
                            ""DeliveredAt"" timestamp with time zone,
                            ""CreatedAt"" timestamp with time zone NOT NULL,
                            ""UpdatedAt"" timestamp with time zone,
                            ""Notes"" text,
                            CONSTRAINT ""PK_Orders"" PRIMARY KEY (""Id""),
                            CONSTRAINT ""FK_Orders_Customers_CustomerId"" FOREIGN KEY (""CustomerId"")
                                REFERENCES ""Customers"" (""Id"") ON DELETE RESTRICT
                        );

                        CREATE INDEX ""IX_Orders_CustomerId"" ON ""Orders"" (""CustomerId"");
                        CREATE UNIQUE INDEX ""IX_Orders_OrderNumber"" ON ""Orders"" (""OrderNumber"");
                    ELSE
                        -- Si la table existe, ajouter juste la colonne Service si elle n'existe pas
                        IF NOT EXISTS (SELECT FROM information_schema.columns
                                      WHERE table_schema = 'public'
                                      AND table_name = 'Orders'
                                      AND column_name = 'Service') THEN
                            ALTER TABLE ""Orders"" ADD COLUMN ""Service"" character varying(200);
                        END IF;
                    END IF;
                END $$;
            ");

            // Ajouter les colonnes manquantes à Customers si elles n'existent pas
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (SELECT FROM information_schema.columns
                                  WHERE table_schema = 'public'
                                  AND table_name = 'Customers'
                                  AND column_name = 'Address') THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""Address"" character varying(500);
                    END IF;

                    IF NOT EXISTS (SELECT FROM information_schema.columns
                                  WHERE table_schema = 'public'
                                  AND table_name = 'Customers'
                                  AND column_name = 'ContactEmail') THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""ContactEmail"" character varying(255);
                    END IF;

                    IF NOT EXISTS (SELECT FROM information_schema.columns
                                  WHERE table_schema = 'public'
                                  AND table_name = 'Customers'
                                  AND column_name = 'ContactName') THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""ContactName"" character varying(100);
                    END IF;

                    IF NOT EXISTS (SELECT FROM information_schema.columns
                                  WHERE table_schema = 'public'
                                  AND table_name = 'Customers'
                                  AND column_name = 'ContactPhone') THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""ContactPhone"" character varying(50);
                    END IF;

                    IF NOT EXISTS (SELECT FROM information_schema.columns
                                  WHERE table_schema = 'public'
                                  AND table_name = 'Customers'
                                  AND column_name = 'Description') THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""Description"" text;
                    END IF;

                    IF NOT EXISTS (SELECT FROM information_schema.columns
                                  WHERE table_schema = 'public'
                                  AND table_name = 'Customers'
                                  AND column_name = 'Siret') THEN
                        ALTER TABLE ""Customers"" ADD COLUMN ""Siret"" character varying(50);
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS ""Orders"";

                ALTER TABLE ""Customers"" DROP COLUMN IF EXISTS ""Address"";
                ALTER TABLE ""Customers"" DROP COLUMN IF EXISTS ""ContactEmail"";
                ALTER TABLE ""Customers"" DROP COLUMN IF EXISTS ""ContactName"";
                ALTER TABLE ""Customers"" DROP COLUMN IF EXISTS ""ContactPhone"";
                ALTER TABLE ""Customers"" DROP COLUMN IF EXISTS ""Description"";
                ALTER TABLE ""Customers"" DROP COLUMN IF EXISTS ""Siret"";
            ");
        }
    }
}
